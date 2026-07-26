using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Customers;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class CustomerService(
    ICustomerRepository customers, IOrderRepository orders,
    ILogger<CustomerService>? logger = null) : ICustomerService
{
    private readonly ILogger<CustomerService> _logger = logger ?? NullLogger<CustomerService>.Instance;
    public async Task<IReadOnlyCollection<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customerData = (await customers.GetAllAsync(cancellationToken)).RequireData();
        var orderData = (await orders.GetAllAsync(cancellationToken)).RequireData();
        return customerData.Select(customer =>
        {
            var customerOrders = orderData.Where(order => order.CustomerId == customer.Id).ToArray();
            return new CustomerDto(customer.Id, customer.FullName, customer.Mobile, customer.Email, customer.CreatedAt, customerOrders.Length, customerOrders.Sum(x => x.Total));
        }).OrderByDescending(x => x.LifetimeValue).ToArray();
    }
}
