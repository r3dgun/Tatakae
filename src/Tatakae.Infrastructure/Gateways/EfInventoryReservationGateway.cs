using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Inventory;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Mappers;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Gateways;

public sealed class EfInventoryReservationGateway(
    TatakaeDbContext db,
    IOptions<InventoryReservationOptions> options) : IInventoryReservationGateway
{
    private readonly InventoryReservationOptions _options = options.Value;

    public Task<InventoryReservationSnapshot> CreateReservedOrderAsync(
        Order order,
        CancellationToken cancellationToken = default)
        => ReserveAsync(order, createOrder: true, cancellationToken);

    public Task<InventoryReservationSnapshot> ReserveExistingOrderAsync(
        Order order,
        CancellationToken cancellationToken = default)
        => ReserveAsync(order, createOrder: false, cancellationToken);

    public async Task<InventoryReservationSnapshot?> GetForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.InventoryReservations
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Count == 0 ? null : MapSnapshot(orderId, rows);
    }

    public async Task<IReadOnlyDictionary<Guid, InventoryReservationSnapshot>> GetForOrdersAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken = default)
    {
        if (orderIds.Count == 0)
            return new Dictionary<Guid, InventoryReservationSnapshot>();

        var ids = orderIds.Distinct().ToArray();
        var rows = await db.InventoryReservations
            .AsNoTracking()
            .Where(x => ids.Contains(x.OrderId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.OrderId)
            .ToDictionary(x => x.Key, x => MapSnapshot(x.Key, x.ToArray()));
    }

    public async Task<InventoryReservationSnapshot?> EnsurePayableAndExtendAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginSerializableAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var rows = await db.InventoryReservations
                .Where(x => x.OrderId == orderId && x.Status == InventoryReservationStatus.Reserved)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                await CommitAsync(transaction, cancellationToken);
                return null;
            }

            if (rows.Any(x => x.ExpiresAt <= now))
            {
                await ExpireRowsAsync(orderId, rows, now, "مهلت پرداخت پیش از شروع مجدد پرداخت تمام شده بود.", cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await CommitAsync(transaction, cancellationToken);
                return null;
            }

            var maximumExpiry = rows.Min(x => x.CreatedAt).Add(_options.MaximumLifetime);
            if (maximumExpiry <= now)
            {
                await ExpireRowsAsync(orderId, rows, now, "حداکثر زمان مجاز نگهداری موجودی تمام شده بود.", cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await CommitAsync(transaction, cancellationToken);
                return null;
            }

            var gatewayExpiry = now.Add(_options.PaymentGraceDuration);
            if (gatewayExpiry > maximumExpiry)
                gatewayExpiry = maximumExpiry;

            foreach (var row in rows)
            {
                if (row.ExpiresAt < gatewayExpiry)
                    row.ExpiresAt = gatewayExpiry;
            }

            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return MapSnapshot(orderId, rows);
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<bool> ConsumePendingAsync(
           Guid orderId,
           string reason,
           CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginSerializableAsync(cancellationToken);
        try
        {
            var allRows = await db.InventoryReservations
                .Where(x => x.OrderId == orderId)
                .ToListAsync(cancellationToken);
            var rows = allRows
                .Where(x => x.Status == InventoryReservationStatus.Reserved)
                .ToArray();

            if (rows.Length == 0)
            {
                var alreadyConsumed = allRows.Count > 0 &&
                                      allRows.All(x => x.Status == InventoryReservationStatus.Consumed);
                await CommitAsync(transaction, cancellationToken);
                return alreadyConsumed;
            }

            foreach (var group in rows.GroupBy(x => x.ProductVariantId))
            {
                var quantity = group.Sum(x => x.Quantity);
                var variant = await db.ProductVariants
                    .SingleOrDefaultAsync(x => x.Id == group.Key, cancellationToken)
                    ?? throw new KeyNotFoundException("SKU رزروشده پیدا نشد.");

                if (variant.ReservedQuantity < quantity || variant.StockQuantity < quantity)
                    throw new InvalidOperationException("موجودی رزروشده برای مصرف با اطلاعات انبار سازگار نیست.");

                variant.ReservedQuantity -= quantity;
                variant.StockQuantity -= quantity;
                foreach (var row in group)
                    row.Status = InventoryReservationStatus.Consumed;

                db.InventoryTransactions.Add(new InventoryTransactionDbRecord
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variant.Id,
                    OrderId = orderId,
                    Type = StockTransactionType.ReservationConsumed,
                    QuantityDelta = -quantity,
                    Note = reason,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction, cancellationToken);
            throw new InvalidOperationException("به دلیل ترافیک بالا، موجودی این کالا در همین لحظه به پایان رسید. لطفاً سبد خرید خود را بررسی کنید.");
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<bool> ReleasePendingAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginSerializableAsync(cancellationToken);
        try
        {
            var rows = await db.InventoryReservations
                .Where(x => x.OrderId == orderId && x.Status == InventoryReservationStatus.Reserved)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                await CommitAsync(transaction, cancellationToken);
                return false;
            }

            await ReleaseRowsAsync(rows, InventoryReservationStatus.Released, reason, DateTimeOffset.UtcNow, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return true;
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    private async Task<InventoryReservationSnapshot> ReserveAsync(
           Order order,
           bool createOrder,
           CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Lines.Count == 0)
            throw new InvalidOperationException("سفارش بدون آیتم قابل رزرو نیست.");

        await using var transaction = await BeginSerializableAsync(cancellationToken);
        try
        {
            var orderExists = await db.Orders.AnyAsync(x => x.Id == order.Id, cancellationToken);
            if (createOrder && orderExists)
                throw new InvalidOperationException("سفارش قبلاً ثبت شده است.");
            if (!createOrder && !orderExists)
                throw new KeyNotFoundException("سفارش برای رزرو مجدد پیدا نشد.");
            if (!createOrder && await db.InventoryReservations.AnyAsync(
                    x => x.OrderId == order.Id && x.Status == InventoryReservationStatus.Reserved,
                    cancellationToken))
            {
                throw new InvalidOperationException("برای این سفارش از قبل رزرو فعال وجود دارد.");
            }

            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.Add(_options.HoldDuration);
            var groupedLines = order.Lines
                .GroupBy(x => x.VariantId)
                .Select(x => new
                {
                    VariantId = x.Key,
                    Quantity = x.Sum(line => line.Quantity),
                    Sku = x.First().Sku
                })
                .ToArray();

            foreach (var line in groupedLines)
            {
                var variant = await db.ProductVariants
                    .SingleOrDefaultAsync(x => x.Id == line.VariantId, cancellationToken)
                    ?? throw new KeyNotFoundException($"SKU سفارش پیدا نشد: {line.Sku}");

                if (!variant.IsActive)
                    throw new InvalidOperationException($"SKU {line.Sku} غیرفعال است.");
                if (variant.StockQuantity - variant.ReservedQuantity < line.Quantity)
                    throw new InvalidOperationException($"موجودی SKU {line.Sku} برای رزرو کافی نیست.");

                variant.ReservedQuantity += line.Quantity;

                db.InventoryReservations.Add(new InventoryReservationDbRecord
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = line.VariantId,
                    OrderId = order.Id,
                    Quantity = line.Quantity,
                    Status = InventoryReservationStatus.Reserved,
                    CreatedAt = now,
                    ExpiresAt = expiresAt
                });

                db.InventoryTransactions.Add(new InventoryTransactionDbRecord
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = line.VariantId,
                    OrderId = order.Id,
                    Type = StockTransactionType.OrderReservation,
                    QuantityDelta = -line.Quantity,
                    Note = $"رزرو موجودی سفارش {order.OrderNumber} تا {expiresAt:O}",
                    CreatedAt = now
                });
            }

            if (createOrder)
            {
                db.Orders.Add(order.ToRecord());
                db.OrderStatusHistory.Add(new OrderStatusHistoryDbRecord
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    FromStatus = null,
                    ToStatus = order.Status,
                    Title = "سفارش ثبت شد و در انتظار پرداخت است",
                    Note = $"موجودی سفارش تا {expiresAt:O} رزرو شده است.",
                    TrackingCode = order.TrackingCode,
                    ChangedBy = "inventory-reservation",
                    HappenedAt = now
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);

            return new InventoryReservationSnapshot(
                order.Id,
                InventoryReservationStatus.Reserved.ToString(),
                now,
                expiresAt,
                groupedLines.Sum(x => x.Quantity));
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction, cancellationToken);
            throw new InvalidOperationException("به دلیل ترافیک بالا، موجودی این کالا در همین لحظه به پایان رسید. لطفاً سبد خرید خود را بررسی کنید.");
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }
    private async Task ExpireRowsAsync(
        Guid orderId,
        IReadOnlyCollection<InventoryReservationDbRecord> rows,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
    {
        await ReleaseRowsAsync(rows, InventoryReservationStatus.Expired, reason, now, cancellationToken);

        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.PendingPayment || order.PaymentStatus == PaymentStatus.Paid)
            return;

        var previous = order.Status;
        order.Status = OrderStatus.Cancelled;
        if (order.PaymentStatus != PaymentStatus.Paid)
            order.PaymentStatus = PaymentStatus.Failed;
        order.AdminNote = "مهلت پرداخت سفارش تمام شد و رزرو موجودی به‌صورت خودکار آزاد شد.";

        db.OrderStatusHistory.Add(new OrderStatusHistoryDbRecord
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = previous,
            ToStatus = OrderStatus.Cancelled,
            Title = "انقضای مهلت پرداخت",
            Note = reason,
            TrackingCode = order.TrackingCode,
            ChangedBy = "inventory-reservation",
            HappenedAt = now
        });
    }

    private async Task ReleaseRowsAsync(
        IReadOnlyCollection<InventoryReservationDbRecord> rows,
        InventoryReservationStatus targetStatus,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var grouped = rows.GroupBy(x => x.ProductVariantId);
        foreach (var group in grouped)
        {
            var quantity = group.Sum(x => x.Quantity);
            var variant = await db.ProductVariants
                .SingleOrDefaultAsync(x => x.Id == group.Key, cancellationToken)
                ?? throw new KeyNotFoundException("SKU رزروشده پیدا نشد.");

            if (variant.ReservedQuantity < quantity)
                throw new InvalidOperationException("موجودی رزروشده دیتابیس با رکوردهای رزرو سازگار نیست.");

            variant.ReservedQuantity -= quantity;
            foreach (var row in group)
                row.Status = targetStatus;

            db.InventoryTransactions.Add(new InventoryTransactionDbRecord
            {
                Id = Guid.NewGuid(),
                ProductVariantId = group.Key,
                OrderId = group.First().OrderId,
                Type = StockTransactionType.ReservationRelease,
                QuantityDelta = quantity,
                Note = reason,
                CreatedAt = now
            });
        }
    }

    private static InventoryReservationSnapshot MapSnapshot(
        Guid orderId,
        IReadOnlyCollection<InventoryReservationDbRecord> rows)
    {
        var latestCreatedAt = rows.Max(x => x.CreatedAt);
        var latestRows = rows.Where(x => x.CreatedAt == latestCreatedAt).ToArray();
        var status = latestRows.Any(x => x.Status == InventoryReservationStatus.Reserved)
            ? InventoryReservationStatus.Reserved
            : latestRows.All(x => x.Status == InventoryReservationStatus.Consumed)
                ? InventoryReservationStatus.Consumed
                : latestRows.Any(x => x.Status == InventoryReservationStatus.Expired)
                    ? InventoryReservationStatus.Expired
                    : InventoryReservationStatus.Released;

        return new InventoryReservationSnapshot(
            orderId,
            status.ToString(),
            latestCreatedAt,
            latestRows.Max(x => x.ExpiresAt),
            latestRows.Sum(x => x.Quantity));
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginSerializableAsync(
        CancellationToken cancellationToken)
        => db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

    private static Task CommitAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
        => transaction is null ? Task.CompletedTask : transaction.CommitAsync(cancellationToken);

    private static async Task RollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is null)
            return;

        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // Preserve the original exception.
        }
    }
}
