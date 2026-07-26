using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Gateways;

/// <summary>
/// Finalizes a paid order and consumes its inventory reservation in one serializable
/// database transaction. This adapter is intentionally separate from the baseline
/// payment repository so non-reservation and isolated test compositions remain valid.
/// </summary>
public sealed class EfPaidOrderInventoryFinalizer(TatakaeDbContext db)
    : IPaidOrderInventoryFinalizer
{
    public async Task<PaymentDto> PersistPaidOutcomeAsync(
        PersistPaymentOutcome command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrderState is null ||
            command.OrderState.Status != OrderStatus.Paid ||
            command.OrderState.PaymentStatus != PaymentStatus.Paid)
        {
            throw new ArgumentException(
                "Paid inventory finalization requires a Paid order state.",
                nameof(command));
        }

        IDbContextTransaction? transaction = null;
        var transactionCompleted = false;
        try
        {
            if (db.Database.IsRelational())
            {
                transaction = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            }

            var payment = await db.Payments
                .SingleOrDefaultAsync(x => x.Id == command.PaymentId, cancellationToken)
                ?? throw new KeyNotFoundException("پرداخت پیدا نشد.");
            var order = await db.Orders
                .SingleOrDefaultAsync(x => x.Id == payment.OrderId, cancellationToken)
                ?? throw new KeyNotFoundException("سفارش پرداخت پیدا نشد.");

            if (order.Id != command.OrderState.OrderId)
                throw new InvalidOperationException("شناسه وضعیت سفارش با پرداخت مطابقت ندارد.");

            var alreadyVerified = IsVerified(payment.Status) && IsVerified(command.Status);
            if (!alreadyVerified)
            {
                payment.Status = command.Status;
                payment.GatewayAuthority = Normalize(command.Authority) ?? payment.GatewayAuthority;
                payment.ReferenceId = Normalize(command.ReferenceId) ?? payment.ReferenceId;
                payment.TraceNumber = Normalize(command.TraceNumber) ?? payment.TraceNumber;
                payment.MaskedCardNumber = Normalize(command.MaskedCardNumber) ?? payment.MaskedCardNumber;
                payment.GatewayMessage = Truncate(command.Message, 1000);
                payment.PaidAt = command.PaidAt ?? payment.PaidAt;

                db.PaymentTransactions.Add(new PaymentTransactionDbRecord
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    Status = command.Status,
                    Amount = payment.Amount,
                    GatewayReference = Normalize(command.ReferenceId) ??
                                       Normalize(command.Authority) ??
                                       Normalize(command.TraceNumber),
                    RawGatewayResponse = Truncate(
                        command.RawGatewayResponse ?? command.Message,
                        2000),
                    CreatedAt = command.OccurredAt
                });
            }

            await ConsumeInventoryReservationAsync(
                order,
                command.OccurredAt,
                cancellationToken);

            PersistOrderState(order, command.OrderState);

            if (!alreadyVerified &&
                !string.IsNullOrWhiteSpace(command.OrderHistoryTitle) &&
                command.PreviousOrderStatus is { } previous &&
                previous != command.OrderState.Status)
            {
                db.OrderStatusHistory.Add(new OrderStatusHistoryDbRecord
                {
                    Id = Guid.NewGuid(),
                    OrderId = command.OrderState.OrderId,
                    FromStatus = previous,
                    ToStatus = command.OrderState.Status,
                    Title = Truncate(command.OrderHistoryTitle, 180) ?? "تغییر وضعیت سفارش",
                    Note = Truncate(command.OrderHistoryNote, 1200),
                    TrackingCode = Truncate(command.OrderState.TrackingCode, 120),
                    ChangedBy = Truncate(command.ChangedBy, 180) ?? "payment-system",
                    HappenedAt = command.OccurredAt
                });
            }

            await db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                transactionCompleted = true;
            }

            return await LoadPaymentAsync(payment.Id, cancellationToken);
        }
        catch
        {
            if (transaction is not null && !transactionCompleted)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Preserve the original persistence exception.
                }
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task<PaymentDto> LoadPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
            ?? throw new KeyNotFoundException("پرداخت پیدا نشد.");
        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == payment.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException("سفارش پرداخت پیدا نشد.");

        payment.Transactions = await db.PaymentTransactions.AsNoTracking()
            .Where(x => x.PaymentId == payment.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        payment.Refunds = await db.Refunds.AsNoTracking()
            .Where(x => x.PaymentId == payment.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Map(payment, order);
    }

    private async Task ConsumeInventoryReservationAsync(
        OrderDbRecord order,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var reservations = await db.InventoryReservations
            .Where(x => x.OrderId == order.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var active = reservations
            .Where(x => x.Status == InventoryReservationStatus.Reserved)
            .ToArray();

        if (active.Length == 0)
        {
            // Orders created before the reservation feature remain payable.
            if (reservations.Count == 0 ||
                reservations.All(x => x.Status == InventoryReservationStatus.Consumed))
            {
                return;
            }

            throw new InvalidOperationException(
                "رزرو فعال موجودی برای نهایی‌کردن این پرداخت وجود ندارد.");
        }

        if (active.Any(x => x.ExpiresAt <= occurredAt))
        {
            throw new InvalidOperationException(
                "مهلت رزرو موجودی پیش از نهایی‌شدن پرداخت تمام شده است.");
        }

        foreach (var group in active.GroupBy(x => x.ProductVariantId))
        {
            var quantity = group.Sum(x => x.Quantity);
            var variant = await db.ProductVariants
                .SingleOrDefaultAsync(x => x.Id == group.Key, cancellationToken)
                ?? throw new KeyNotFoundException("SKU رزروشده برای پرداخت پیدا نشد.");

            if (variant.ReservedQuantity < quantity || variant.StockQuantity < quantity)
            {
                throw new InvalidOperationException(
                    "موجودی رزروشده برای نهایی‌کردن پرداخت با اطلاعات انبار سازگار نیست.");
            }

            variant.ReservedQuantity -= quantity;
            variant.StockQuantity -= quantity;
            foreach (var reservation in group)
                reservation.Status = InventoryReservationStatus.Consumed;

            db.InventoryTransactions.Add(new InventoryTransactionDbRecord
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                OrderId = order.Id,
                Type = StockTransactionType.ReservationConsumed,
                QuantityDelta = -quantity,
                Note = $"مصرف رزرو پس از پرداخت موفق سفارش {order.OrderNumber}",
                CreatedAt = occurredAt
            });
        }
    }

    private static void PersistOrderState(
        OrderDbRecord record,
        OrderPaymentState state)
    {
        record.Status = state.Status;
        record.PaymentStatus = state.PaymentStatus;
        record.TrackingCode = state.TrackingCode;
        record.AdminNote = state.AdminNote;
    }

    private static PaymentDto Map(PaymentDbRecord payment, OrderDbRecord order) => new(
        payment.Id,
        order.Id,
        order.OrderNumber,
        order.CustomerName,
        order.CustomerMobile,
        payment.Method.ToString(),
        MethodLabel(payment.Method),
        payment.Gateway.ToString(),
        payment.Status.ToString(),
        StatusLabel(payment.Status),
        payment.Amount,
        payment.GatewayAuthority,
        payment.ReferenceId,
        payment.TraceNumber,
        payment.MaskedCardNumber,
        payment.GatewayMessage,
        payment.CreatedAt,
        payment.PaidAt,
        payment.Transactions
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PaymentTransactionDto(
                x.Id,
                x.Status.ToString(),
                StatusLabel(x.Status),
                x.Amount,
                x.GatewayReference,
                x.RawGatewayResponse,
                x.CreatedAt))
            .ToArray(),
        payment.Refunds
            .OrderBy(x => x.CreatedAt)
            .Select(MapRefund)
            .ToArray());

    private static PaymentRefundDto MapRefund(RefundDbRecord refund) => new(
        refund.Id,
        refund.PaymentId ?? Guid.Empty,
        refund.OrderId,
        refund.Amount,
        refund.Status.ToString(),
        RefundStatusLabel(refund.Status),
        refund.Reason,
        refund.ReferenceNumber,
        refund.CreatedAt,
        refund.PaidAt);

    private static bool IsVerified(PaymentTransactionStatus status)
        => status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Verified;

    private static string StatusLabel(PaymentTransactionStatus status) => status switch
    {
        PaymentTransactionStatus.Pending => "در انتظار پرداخت",
        PaymentTransactionStatus.RedirectedToGateway => "ارسال به درگاه",
        PaymentTransactionStatus.Succeeded => "موفق",
        PaymentTransactionStatus.Verified => "تأیید شده",
        PaymentTransactionStatus.Failed => "ناموفق",
        PaymentTransactionStatus.CancelledByUser => "لغو توسط مشتری",
        PaymentTransactionStatus.Reversed => "برگشت خورده",
        PaymentTransactionStatus.Refunded => "Refund شده",
        _ => status.ToString()
    };

    private static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.OnlineGateway => "پرداخت آنلاین",
        PaymentMethod.CardToCard => "کارت به کارت",
        PaymentMethod.CashOnDelivery => "پرداخت هنگام تحویل",
        PaymentMethod.Wallet => "کیف پول",
        PaymentMethod.BankTransfer => "حواله بانکی",
        _ => method.ToString()
    };

    private static string RefundStatusLabel(RefundStatus status) => status switch
    {
        RefundStatus.Requested => "درخواست‌شده",
        RefundStatus.Approved => "پذیرفته‌شده توسط زرین‌پال",
        RefundStatus.Rejected => "ردشده",
        RefundStatus.PaidToWallet => "واریز به کیف پول",
        RefundStatus.PaidToBankCard => "واریز به حساب بانکی",
        RefundStatus.Cancelled => "لغوشده",
        _ => status.ToString()
    };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncate(string? value, int maxLength)
    {
        var normalized = Normalize(value);
        return normalized is null || normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
