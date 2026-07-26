using Tatakae.Application.Contracts.Payments;

namespace Tatakae.Application.Interfaces.Gateways;

/// <summary>
/// Atomically persists a successful payment/order transition and consumes the
/// associated inventory reservation. The infrastructure implementation uses the
/// same database transaction for both operations.
/// </summary>
public interface IPaidOrderInventoryFinalizer
{
    Task<PaymentDto> PersistPaidOutcomeAsync(
        PersistPaymentOutcome command,
        CancellationToken cancellationToken = default);
}
