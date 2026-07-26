using Tatakae.Application.Contracts.Inventory;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Interfaces.Gateways;

/// <summary>
/// Persists inventory reservations and the pending order in one database transaction.
/// ExpiresAt is the source of truth; Hangfire only performs eventual cleanup.
/// </summary>
public interface IInventoryReservationGateway
{
    Task<InventoryReservationSnapshot> CreateReservedOrderAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<InventoryReservationSnapshot> ReserveExistingOrderAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<InventoryReservationSnapshot?> GetForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, InventoryReservationSnapshot>> GetForOrdersAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns null when the reservation no longer exists or has expired. An active
    /// reservation is extended to cover the gateway round-trip.
    /// </summary>
    Task<InventoryReservationSnapshot?> EnsurePayableAndExtendAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<bool> ConsumePendingAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> ReleasePendingAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default);
}
