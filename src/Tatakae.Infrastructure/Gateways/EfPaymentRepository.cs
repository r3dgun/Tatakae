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
/// EF payment persistence adapter. All workflow decisions are supplied by
/// PaymentService/Order; this class only stores them atomically in one SaveChanges.
/// </summary>
public sealed class EfPaymentRepository(TatakaeDbContext db) : IPaymentRepository
{
    public async Task<PaymentDto?> GetActiveForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var paymentId = await db.Payments.AsNoTracking()
            .Where(x => x.OrderId == orderId &&
                        x.Status != PaymentTransactionStatus.Failed &&
                        x.Status != PaymentTransactionStatus.CancelledByUser &&
                        x.Status != PaymentTransactionStatus.Reversed)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return paymentId is { } id
            ? await LoadPaymentAsync(id, cancellationToken)
            : null;
    }

    public async Task<PaymentDto?> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var paymentId = await db.Payments.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return paymentId is { } id
            ? await LoadPaymentAsync(id, cancellationToken)
            : null;
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        => await db.Payments.AsNoTracking().AnyAsync(x => x.Id == paymentId, cancellationToken)
            ? await LoadPaymentAsync(paymentId, cancellationToken)
            : null;

    public async Task<CreatePaymentResult> CreateAsync(
        CreatePaymentRecord command,
        CancellationToken cancellationToken = default)
    {
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

            var existingId = await db.Payments
                .Where(x => x.OrderId == command.OrderId &&
                            x.Status != PaymentTransactionStatus.Failed &&
                            x.Status != PaymentTransactionStatus.CancelledByUser &&
                            x.Status != PaymentTransactionStatus.Reversed)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingId is { } activePaymentId)
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    transactionCompleted = true;
                }

                return new CreatePaymentResult(
                    await LoadPaymentAsync(activePaymentId, cancellationToken),
                    false);
            }

            var orderExists = await db.Orders.AsNoTracking().AnyAsync(
                x => x.Id == command.OrderId,
                cancellationToken);
            if (!orderExists)
                throw new KeyNotFoundException("سفارش پیدا نشد.");

            var payment = new PaymentDbRecord
            {
                Id = command.PaymentId,
                OrderId = command.OrderId,
                Method = command.Method,
                Gateway = command.Gateway,
                Status = PaymentTransactionStatus.Pending,
                Amount = command.Amount,
                GatewayMessage = Truncate(command.Message, 1000),
                CreatedAt = command.CreatedAt
            };

            db.Payments.Add(payment);
            db.PaymentTransactions.Add(new PaymentTransactionDbRecord
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Status = PaymentTransactionStatus.Pending,
                Amount = payment.Amount,
                RawGatewayResponse = Truncate(command.Message, 2000),
                CreatedAt = command.CreatedAt
            });

            await db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                transactionCompleted = true;
            }

            return new CreatePaymentResult(
                await LoadPaymentAsync(payment.Id, cancellationToken),
                true);
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

    public async Task<PaymentDto> PersistOutcomeAsync(
        PersistPaymentOutcome command,
        CancellationToken cancellationToken = default)
    {
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

            // Update the payment and order as flat tracked rows. Loading the whole
            // Payment -> Order -> Transactions graph in a DbContext that has already
            // created the payment can produce duplicate/partial tracking graphs in
            // EF InMemory and in long-lived request scopes.
            var payment = await db.Payments.SingleOrDefaultAsync(
                              x => x.Id == command.PaymentId,
                              cancellationToken)
                          ?? throw new KeyNotFoundException("پرداخت پیدا نشد.");
            var orderRecord = await db.Orders.SingleOrDefaultAsync(
                                  x => x.Id == payment.OrderId,
                                  cancellationToken)
                              ?? throw new KeyNotFoundException("سفارش پرداخت پیدا نشد.");

            if (IsVerified(payment.Status) && IsVerified(command.Status))
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    transactionCompleted = true;
                }

                return await LoadPaymentAsync(payment.Id, cancellationToken);
            }

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

            if (command.OrderState is not null)
            {
                PersistOrderState(orderRecord, command.OrderState);

                if (!string.IsNullOrWhiteSpace(command.OrderHistoryTitle) &&
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

    public async Task<CreatePaymentRefundResult> CreateRefundAsync(
        CreatePaymentRefundRecord command,
        CancellationToken cancellationToken = default)
    {
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

            var completedAmount = await db.Refunds
                .Where(x => x.PaymentId == command.PaymentId &&
                            (x.Status == RefundStatus.PaidToBankCard ||
                             x.Status == RefundStatus.PaidToWallet))
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

            var normalizedReason = Truncate(command.Reason, 700)
                ?? "درخواست Refund زرین‌پال";

            var existing = await db.Refunds
                .AsNoTracking()
                .Where(x => x.PaymentId == command.PaymentId &&
                            x.Amount == command.Amount &&
                            (x.Reason == normalizedReason ||
                             x.Reason.StartsWith(normalizedReason + " | ")) &&
                            x.Status != RefundStatus.Rejected &&
                            x.Status != RefundStatus.Cancelled)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    transactionCompleted = true;
                }

                return new CreatePaymentRefundResult(MapRefund(existing), false, completedAmount);
            }

            var payment = await db.Payments
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == command.PaymentId, cancellationToken)
                ?? throw new KeyNotFoundException("پرداخت پیدا نشد.");

            if (payment.OrderId != command.OrderId)
                throw new InvalidOperationException("پرداخت با سفارش Refund مطابقت ندارد.");

            var reservedAmount = await db.Refunds
                .Where(x => x.PaymentId == command.PaymentId &&
                            x.Status != RefundStatus.Rejected &&
                            x.Status != RefundStatus.Cancelled)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

            if (reservedAmount + command.Amount > command.MaximumTotalRefundAmount)
            {
                throw new InvalidOperationException(
                    "مجموع Refundهای ثبت‌شده از مبلغ پرداخت بیشتر می‌شود.");
            }

            var refund = new RefundDbRecord
            {
                Id = command.RefundId,
                PaymentId = command.PaymentId,
                OrderId = command.OrderId,
                Status = RefundStatus.Requested,
                Amount = command.Amount,
                Reason = normalizedReason,
                CreatedAt = command.CreatedAt
            };

            db.Refunds.Add(refund);
            await db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                transactionCompleted = true;
            }

            return new CreatePaymentRefundResult(MapRefund(refund), true, completedAmount);
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

    public async Task<PaymentRefundDto> PersistRefundOutcomeAsync(
        PersistPaymentRefundOutcome command,
        CancellationToken cancellationToken = default)
    {
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

            var refund = await db.Refunds
                .SingleOrDefaultAsync(x => x.Id == command.RefundId, cancellationToken)
                ?? throw new KeyNotFoundException("درخواست Refund پیدا نشد.");

            if (refund.Status == RefundStatus.PaidToBankCard &&
                command.Status == RefundStatus.PaidToBankCard)
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    transactionCompleted = true;
                }

                return MapRefund(refund);
            }

            var payment = await db.Payments
                .SingleOrDefaultAsync(x => x.Id == refund.PaymentId, cancellationToken)
                ?? throw new KeyNotFoundException("پرداخت Refund پیدا نشد.");
            var orderRecord = await db.Orders
                .SingleOrDefaultAsync(x => x.Id == payment.OrderId, cancellationToken)
                ?? throw new KeyNotFoundException("سفارش Refund پیدا نشد.");

            refund.Status = command.Status;
            refund.ReferenceNumber = Normalize(command.ReferenceNumber) ?? refund.ReferenceNumber;
            refund.PaidAt = command.PaidAt ?? refund.PaidAt;
            // Reason is the client supplied refund request description and is also
            // part of the idempotency fingerprint. Keep it immutable. Provider outcome
            // details belong to transactions/logs and must not change future matching.

            if (command.NewPaymentStatus is { } newPaymentStatus)
                payment.Status = newPaymentStatus;

            if (command.TransactionStatus is { } transactionStatus)
            {
                var gatewayReference = Normalize(command.ReferenceNumber);
                var duplicateTransaction = await db.PaymentTransactions.AnyAsync(x =>
                    x.PaymentId == payment.Id &&
                    x.Status == transactionStatus &&
                    x.Amount == refund.Amount &&
                    x.GatewayReference == gatewayReference,
                    cancellationToken);

                if (!duplicateTransaction)
                {
                    db.PaymentTransactions.Add(new PaymentTransactionDbRecord
                    {
                        Id = Guid.NewGuid(),
                        PaymentId = payment.Id,
                        Status = transactionStatus,
                        Amount = refund.Amount,
                        GatewayReference = gatewayReference,
                        RawGatewayResponse = Truncate(
                            command.RawGatewayResponse ?? command.Message,
                            2000),
                        CreatedAt = command.OccurredAt
                    });
                }
            }

            if (command.OrderState is not null)
            {
                PersistOrderState(orderRecord, command.OrderState);

                if (command.PreviousOrderStatus is { } previous &&
                    previous != command.OrderState.Status)
                {
                    db.OrderStatusHistory.Add(new OrderStatusHistoryDbRecord
                    {
                        Id = Guid.NewGuid(),
                        OrderId = command.OrderState.OrderId,
                        FromStatus = previous,
                        ToStatus = command.OrderState.Status,
                        Title = "Refund کامل پرداخت در زرین‌پال",
                        Note = Truncate(command.Message, 1200),
                        TrackingCode = Truncate(command.OrderState.TrackingCode, 120),
                        ChangedBy = Truncate(command.ChangedBy, 180) ?? "zarinpal-refund",
                        HappenedAt = command.OccurredAt
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                transactionCompleted = true;
            }

            return MapRefund(refund);
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

    public async Task<AdminPaymentsDto> AdminListAsync(CancellationToken cancellationToken = default)
    {
        var paymentRows = await db.Payments.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var paymentIds = paymentRows.Select(x => x.Id).ToArray();
        var orderIds = paymentRows.Select(x => x.OrderId).Distinct().ToArray();

        var orderMap = await db.Orders.AsNoTracking()
            .Where(x => orderIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var transactionMap = (await db.PaymentTransactions.AsNoTracking()
                .Where(x => paymentIds.Contains(x.PaymentId))
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.PaymentId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var refundMap = (await db.Refunds.AsNoTracking()
                .Where(x => x.PaymentId != null && paymentIds.Contains(x.PaymentId.Value))
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.PaymentId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        var rows = paymentRows
            .Where(x => orderMap.ContainsKey(x.OrderId))
            .Select(payment =>
            {
                payment.Transactions = transactionMap.GetValueOrDefault(payment.Id) ?? [];
                payment.Refunds = refundMap.GetValueOrDefault(payment.Id) ?? [];
                return Map(payment, orderMap[payment.OrderId]);
            })
            .ToArray();

        var summary = new AdminPaymentSummaryDto(
            rows.Count(x => x.Status is nameof(PaymentTransactionStatus.Pending) or nameof(PaymentTransactionStatus.RedirectedToGateway)),
            rows.Count(x => x.Status is nameof(PaymentTransactionStatus.Succeeded) or nameof(PaymentTransactionStatus.Verified)),
            rows.Count(x => x.Status is nameof(PaymentTransactionStatus.Failed) or nameof(PaymentTransactionStatus.CancelledByUser)),
            rows.Where(x => x.Status is nameof(PaymentTransactionStatus.Succeeded) or nameof(PaymentTransactionStatus.Verified)).Sum(x => x.Amount),
            rows.Where(x => x.Status is nameof(PaymentTransactionStatus.Pending) or nameof(PaymentTransactionStatus.RedirectedToGateway)).Sum(x => x.Amount),
            rows.SelectMany(x => x.Refunds)
                .Where(x => x.Status is nameof(RefundStatus.PaidToBankCard) or nameof(RefundStatus.PaidToWallet))
                .Sum(x => x.Amount));

        return new AdminPaymentsDto(summary, rows);
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

    private static void PersistOrderState(
        OrderDbRecord record,
        OrderPaymentState state)
    {
        if (record.Id != state.OrderId)
            throw new InvalidOperationException("شناسه وضعیت سفارش با پرداخت مطابقت ندارد.");

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
