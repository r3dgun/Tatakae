using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Services;

public sealed partial class AccountService(
    ICustomerRepository customers, IOrderRepository orders,
    ILogger<AccountService>? logger = null,
    IInventoryReservationGateway? inventoryReservations = null) : IAccountService
{
    private readonly ILogger<AccountService> _logger = logger ?? NullLogger<AccountService>.Instance;
    public async Task<AccountSessionDto> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var existing = (await customers.GetByMobileAsync(request.Mobile, cancellationToken)).DataOrDefault();
        var customer = existing ?? Customer.Create(Guid.NewGuid(), request.FullName, request.Mobile, request.Email, DateTimeOffset.UtcNow);
        (await customers.UpsertAsync(customer, cancellationToken)).EnsureSuccess();
        return CreateSession(customer);
    }

    public async Task<AccountSessionDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(request.Mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return null;
        return CreateSession(customer);
    }

    public async Task<AccountProfileDto?> ProfileAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return null;
        var orderData = (await orders.GetAllAsync(cancellationToken)).RequireData();
        var customerOrders = orderData.Where(order => order.CustomerId == customer.Id).ToArray();
        return new AccountProfileDto(customer.Id, customer.FullName, customer.Mobile, customer.Email, customer.CreatedAt, customerOrders.Length, customerOrders.Sum(x => x.Total));
    }

    public async Task<IReadOnlyCollection<Tatakae.Application.Contracts.Orders.OrderDto>> OrdersAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return Array.Empty<Tatakae.Application.Contracts.Orders.OrderDto>();

        var orderData = (await orders.GetAllAsync(cancellationToken)).RequireData()
            .Where(order => order.CustomerId == customer.Id)
            .OrderByDescending(order => order.CreatedAt)
            .ToArray();
        var reservationMap = inventoryReservations is null
            ? new Dictionary<Guid, Tatakae.Application.Contracts.Inventory.InventoryReservationSnapshot>()
            : await inventoryReservations.GetForOrdersAsync(
                orderData.Select(x => x.Id).ToArray(),
                cancellationToken);

        return orderData
            .Select(order => OrderService.Map(order, reservationMap.GetValueOrDefault(order.Id)))
            .ToArray();
    }

    public async Task<Tatakae.Application.Contracts.Orders.OrderTrackingDto?> OrderTrackingAsync(string mobile, Guid orderId, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return null;

        var order = (await orders.GetByIdAsync(orderId, cancellationToken)).DataOrDefault();
        if (order is null || order.CustomerId != customer.Id) return null;

        var history = (await orders.GetStatusHistoryAsync(order.Id, cancellationToken)).RequireData();
        return OrderService.MapTracking(order, history);
    }


    public async Task<IReadOnlyCollection<CustomerAddressDto>> AddressesAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return Array.Empty<CustomerAddressDto>();
        var addresses = (await customers.GetAddressesAsync(customer.Id, cancellationToken)).RequireData();
        return addresses.Select(MapAddress).ToArray();
    }

    public async Task<CustomerAddressDto?> UpsertAddressAsync(string mobile, Guid? addressId, CustomerAddressRequest request, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return null;

        var address = new Address(
            addressId.GetValueOrDefault(Guid.NewGuid()),
            request.RecipientName,
            request.Mobile,
            request.Province,
            request.City,
            request.PostalCode,
            request.AddressLine,
            request.Plaque,
            request.Unit,
            request.IsDefault);

        var saved = (await customers.UpsertAddressAsync(customer.Id, address, cancellationToken)).RequireData();
        return MapAddress(saved);
    }

    public async Task<bool> DeleteAddressAsync(string mobile, Guid addressId, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return false;
        var deleteResult = await customers.DeleteAddressAsync(customer.Id, addressId, cancellationToken);
        if (!deleteResult.IsSuccess && deleteResult.Status == ResultStatus.NotFound) return false;
        deleteResult.EnsureSuccess();
        return true;
    }

    private static CustomerAddressDto MapAddress(Address address) => new(
        address.Id,
        address.RecipientName,
        address.Mobile,
        address.Province,
        address.City,
        address.PostalCode,
        address.AddressLine,
        address.Plaque,
        address.Unit,
        address.IsDefault);

    private static AccountSessionDto CreateSession(Customer customer)
        => new(customer.Id, customer.FullName, customer.Mobile, customer.Email, Convert.ToBase64String(Guid.NewGuid().ToByteArray()), DateTimeOffset.UtcNow.AddDays(7));
}
