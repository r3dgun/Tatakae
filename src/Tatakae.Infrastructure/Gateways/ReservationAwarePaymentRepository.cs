using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Gateways;

/// <summary>
/// Production-only payment persistence decorator. The baseline
/// <see cref="EfPaymentRepository"/> remains unaware of inventory reservations, while
/// this adapter validates an active reservation before creating a payment and routes
/// successful paid transitions through the atomic inventory finalizer.
/// </summary>
public sealed class ReservationAwarePaymentRepository(
    EfPaymentRepository inner,
    IInventoryReservationGateway inventoryReservations,
    IPaidOrderInventoryFinalizer paidOrderFinalizer) : IPaymentRepository
{
    public Task<PaymentDto?> GetActiveForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => inner.GetActiveForOrderAsync(orderId, cancellationToken);

    public Task<PaymentDto?> GetForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => inner.GetForOrderAsync(orderId, cancellationToken);

    public Task<PaymentDto?> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
        => inner.GetByIdAsync(paymentId, cancellationToken);

    public async Task<CreatePaymentResult> CreateAsync(
        CreatePaymentRecord command,
        CancellationToken cancellationToken = default)
    {
        var current = await inventoryReservations.GetForOrderAsync(
            command.OrderId,
            cancellationToken);

        // Legacy orders created before inventory reservations remain payable.
        if (current is not null)
        {
            if (!string.Equals(
                    current.Status,
                    InventoryReservationStatus.Reserved.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "رزرو موجودی این سفارش دیگر فعال نیست.");
            }

            var payable = await inventoryReservations.EnsurePayableAndExtendAsync(
                command.OrderId,
                cancellationToken);

            if (payable is null)
            {
                throw new InvalidOperationException(
                    "مهلت نگهداری موجودی این سفارش تمام شده است. لطفاً سفارش جدیدی ثبت کنید.");
            }
        }

        return await inner.CreateAsync(command, cancellationToken);
    }

    public Task<PaymentDto> PersistOutcomeAsync(
        PersistPaymentOutcome command,
        CancellationToken cancellationToken = default)
    {
        var isPaidTransition = command.OrderState is
        {
            Status: OrderStatus.Paid,
            PaymentStatus: PaymentStatus.Paid
        } && command.Status is
            PaymentTransactionStatus.Succeeded or
            PaymentTransactionStatus.Verified;

        return isPaidTransition
            ? paidOrderFinalizer.PersistPaidOutcomeAsync(command, cancellationToken)
            : inner.PersistOutcomeAsync(command, cancellationToken);
    }

    public Task<CreatePaymentRefundResult> CreateRefundAsync(
        CreatePaymentRefundRecord command,
        CancellationToken cancellationToken = default)
        => inner.CreateRefundAsync(command, cancellationToken);

    public Task<PaymentRefundDto> PersistRefundOutcomeAsync(
        PersistPaymentRefundOutcome command,
        CancellationToken cancellationToken = default)
        => inner.PersistRefundOutcomeAsync(command, cancellationToken);

    public Task<AdminPaymentsDto> AdminListAsync(
        CancellationToken cancellationToken = default)
        => inner.AdminListAsync(cancellationToken);
}
