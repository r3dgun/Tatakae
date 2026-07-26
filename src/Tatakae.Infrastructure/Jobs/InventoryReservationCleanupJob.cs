using System.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Inventory;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Jobs;

public sealed class InventoryReservationCleanupJob(
    TatakaeDbContext db,
    IOptions<InventoryReservationOptions> options,
    ILogger<InventoryReservationCleanupJob> logger)
{
    private readonly InventoryReservationOptions _options = options.Value;

    [Queue("inventory")]
    [DisableConcurrentExecution(timeoutInSeconds: 55)]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var totalConsumed = 0;
        var totalReleased = 0;
        var now = DateTimeOffset.UtcNow;

        for (var batchNumber = 0; batchNumber < _options.SafeMaxBatchesPerRun; batchNumber++)
        {
            var consumed = await ConsumePaidBatchAsync(now, cancellationToken);
            totalConsumed += consumed;
            if (consumed > 0)
                db.ChangeTracker.Clear();

            var released = await ReleaseBatchAsync(now, cancellationToken);
            totalReleased += released;
            if (consumed == 0 && released == 0)
                break;

            db.ChangeTracker.Clear();
        }

        if (totalConsumed > 0)
        {
            logger.LogInformation(
                "Paid inventory reservations reconciled. Count={Count} OccurredAt={OccurredAt}",
                totalConsumed,
                now);
        }

        if (totalReleased > 0)
        {
            logger.LogInformation(
                "Expired inventory reservations released. Count={Count} OccurredAt={OccurredAt}",
                totalReleased,
                now);
        }

    }

    /// <summary>
    /// Reconciles the narrow post-commit window where the payment/order was persisted
    /// as paid but the immediate idempotent reservation consumption did not complete.
    /// Paid reservations are consumed regardless of ExpiresAt and are never released.
    /// </summary>
    private async Task<int> ConsumePaidBatchAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var candidateOrderIds = await (
                    from reservation in db.InventoryReservations
                    join order in db.Orders on reservation.OrderId equals order.Id
                    where reservation.Status == InventoryReservationStatus.Reserved &&
                          order.PaymentStatus == PaymentStatus.Paid
                    group reservation by reservation.OrderId
                    into reservationGroup
                    orderby reservationGroup.Min(x => x.CreatedAt)
                    select reservationGroup.Key)
                .Take(_options.SafeCleanupBatchSize)
                .ToArrayAsync(cancellationToken);

            if (candidateOrderIds.Length == 0)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            var rows = await db.InventoryReservations
                .Where(x => candidateOrderIds.Contains(x.OrderId) &&
                            x.Status == InventoryReservationStatus.Reserved)
                .OrderBy(x => x.OrderId)
                .ThenBy(x => x.ProductVariantId)
                .ToListAsync(cancellationToken);

            var variantIds = rows
                .Select(x => x.ProductVariantId)
                .Distinct()
                .ToArray();
            var variants = await db.ProductVariants
                .Where(x => variantIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            var consumedCount = 0;
            foreach (var orderGroup in rows.GroupBy(x => x.OrderId))
            {
                foreach (var variantGroup in orderGroup.GroupBy(x => x.ProductVariantId))
                {
                    if (!variants.TryGetValue(variantGroup.Key, out var variant))
                        throw new KeyNotFoundException($"Reserved product variant was not found: {variantGroup.Key}");

                    var quantity = variantGroup.Sum(x => x.Quantity);
                    if (variant.ReservedQuantity < quantity || variant.StockQuantity < quantity)
                    {
                        throw new InvalidOperationException(
                            $"Inventory is inconsistent for paid reservation variant {variant.Id}. " +
                            $"Required={quantity}, Reserved={variant.ReservedQuantity}, Stock={variant.StockQuantity}.");
                    }

                    variant.ReservedQuantity -= quantity;
                    variant.StockQuantity -= quantity;

                    foreach (var reservation in variantGroup)
                    {
                        reservation.Status = InventoryReservationStatus.Consumed;
                        consumedCount++;
                    }

                    db.InventoryTransactions.Add(new InventoryTransactionDbRecord
                    {
                        Id = Guid.NewGuid(),
                        ProductVariantId = variant.Id,
                        OrderId = orderGroup.Key,
                        Type = StockTransactionType.ReservationConsumed,
                        QuantityDelta = -quantity,
                        Note = "تطبیق خودکار رزرو موجودی سفارش پرداخت‌شده.",
                        CreatedAt = now
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return consumedCount;
        }
        catch
        {
            if (transaction is not null)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Preserve the original exception.
                }
            }

            throw;
        }
    }

    private async Task<int> ReleaseBatchAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var candidateOrderIds = await db.InventoryReservations
                .Where(x => x.Status == InventoryReservationStatus.Reserved)
                .GroupBy(x => x.OrderId)
                .Where(group => group.Max(x => x.ExpiresAt) <= now)
                .OrderBy(group => group.Min(x => x.ExpiresAt))
                .Select(group => group.Key)
                .Take(_options.SafeCleanupBatchSize)
                .ToArrayAsync(cancellationToken);

            if (candidateOrderIds.Length == 0)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            var rows = await db.InventoryReservations
                .Where(x => candidateOrderIds.Contains(x.OrderId) &&
                            x.Status == InventoryReservationStatus.Reserved)
                .OrderBy(x => x.OrderId)
                .ThenBy(x => x.ProductVariantId)
                .ToListAsync(cancellationToken);

            var expiredGroups = rows
                .GroupBy(x => x.OrderId)
                .Where(group => group.All(x => x.ExpiresAt <= now))
                .ToArray();

            if (expiredGroups.Length == 0)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            var orderIds = expiredGroups.Select(x => x.Key).ToArray();
            var orders = await db.Orders
                .Where(x => orderIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            var variantIds = expiredGroups
                .SelectMany(group => group.Select(x => x.ProductVariantId))
                .Distinct()
                .ToArray();
            var variants = await db.ProductVariants
                .Where(x => variantIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            var releasedCount = 0;
            foreach (var orderGroup in expiredGroups)
            {
                orders.TryGetValue(orderGroup.Key, out var order);
                if (order?.PaymentStatus == PaymentStatus.Paid)
                {
                    logger.LogWarning(
                        "Paid order still has reserved inventory and was skipped by cleanup. OrderId={OrderId}",
                        orderGroup.Key);
                    continue;
                }

                foreach (var variantGroup in orderGroup.GroupBy(x => x.ProductVariantId))
                {
                    if (!variants.TryGetValue(variantGroup.Key, out var variant))
                        throw new KeyNotFoundException($"Reserved product variant was not found: {variantGroup.Key}");

                    var quantity = variantGroup.Sum(x => x.Quantity);
                    if (variant.ReservedQuantity < quantity)
                    {
                        throw new InvalidOperationException(
                            $"ReservedQuantity is inconsistent for variant {variant.Id}. Expected at least {quantity}, actual {variant.ReservedQuantity}.");
                    }

                    variant.ReservedQuantity -= quantity;
                    foreach (var reservation in variantGroup)
                    {
                        reservation.Status = InventoryReservationStatus.Expired;
                        releasedCount++;
                    }

                    db.InventoryTransactions.Add(new InventoryTransactionDbRecord
                    {
                        Id = Guid.NewGuid(),
                        ProductVariantId = variant.Id,
                        OrderId = orderGroup.Key,
                        Type = StockTransactionType.ReservationRelease,
                        QuantityDelta = quantity,
                        Note = "آزادسازی خودکار موجودی پس از پایان مهلت پرداخت.",
                        CreatedAt = now
                    });
                }

                if (order is not null && order.Status == OrderStatus.PendingPayment)
                {
                    var previousStatus = order.Status;
                    order.Status = OrderStatus.Cancelled;
                    order.PaymentStatus = PaymentStatus.Failed;
                    order.AdminNote = "مهلت پرداخت سفارش تمام شد و موجودی رزروشده آزاد شد.";

                    db.OrderStatusHistory.Add(new OrderStatusHistoryDbRecord
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        FromStatus = previousStatus,
                        ToStatus = OrderStatus.Cancelled,
                        Title = "انقضای مهلت پرداخت",
                        Note = "پرداخت در بازه رزرو تکمیل نشد؛ سفارش لغو و موجودی آزاد شد.",
                        TrackingCode = order.TrackingCode,
                        ChangedBy = "hangfire-inventory-cleanup",
                        HappenedAt = now
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return releasedCount;
        }
        catch
        {
            if (transaction is not null)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Preserve the original exception.
                }
            }

            throw;
        }
    }
}
