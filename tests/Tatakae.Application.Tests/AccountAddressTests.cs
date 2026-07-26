using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class AccountAddressTests
{
    [Fact]
    public async Task UpsertAddressAsync_AddsFirstAddressAsDefault()
    {
        var customer = Customer.Create(Guid.NewGuid(), "علی رضایی", "09123456789", "ali@example.com", DateTimeOffset.UnixEpoch);
        var service = new AccountService(new FakeCustomerRepository(customer), new EmptyOrderRepository());

        var saved = await service.UpsertAddressAsync(customer.Mobile, null, Request(isDefault: false));

        Assert.NotNull(saved);
        Assert.True(saved!.IsDefault);
        Assert.Equal("تهران", saved.City);
    }

    [Fact]
    public async Task UpsertAddressAsync_WhenNewDefault_UnsetsPreviousDefault()
    {
        var customer = Customer.Create(Guid.NewGuid(), "علی رضایی", "09123456789", null, DateTimeOffset.UnixEpoch);
        var repository = new FakeCustomerRepository(customer);
        var service = new AccountService(repository, new EmptyOrderRepository());

        var first = await service.UpsertAddressAsync(customer.Mobile, null, Request(recipient: "خانه", isDefault: true));
        var second = await service.UpsertAddressAsync(customer.Mobile, null, Request(recipient: "محل کار", isDefault: true));

        var addresses = await service.AddressesAsync(customer.Mobile);

        Assert.Equal(2, addresses.Count);
        Assert.True(addresses.Single(x => x.Id == second!.Id).IsDefault);
        Assert.False(addresses.Single(x => x.Id == first!.Id).IsDefault);
    }

    [Fact]
    public async Task DeleteAddressAsync_RemovesAddressAndKeepsOneDefault()
    {
        var customer = Customer.Create(Guid.NewGuid(), "علی رضایی", "09123456789", null, DateTimeOffset.UnixEpoch);
        var repository = new FakeCustomerRepository(customer);
        var service = new AccountService(repository, new EmptyOrderRepository());

        var first = await service.UpsertAddressAsync(customer.Mobile, null, Request(recipient: "خانه", isDefault: true));
        await service.UpsertAddressAsync(customer.Mobile, null, Request(recipient: "محل کار", isDefault: false));

        await service.DeleteAddressAsync(customer.Mobile, first!.Id);
        var addresses = await service.AddressesAsync(customer.Mobile);

        var remaining = Assert.Single(addresses);
        Assert.True(remaining.IsDefault);
        Assert.Equal("محل کار", remaining.RecipientName);
    }

    private static CustomerAddressRequest Request(string recipient = "علی رضایی", bool isDefault = false) => new()
    {
        RecipientName = recipient,
        Mobile = "09123456789",
        Province = "تهران",
        City = "تهران",
        PostalCode = "1234567890",
        AddressLine = "خیابان ولیعصر، پلاک ۱۰، طبقه اول",
        Plaque = "۱۰",
        Unit = "۱",
        IsDefault = isDefault
    };

    private sealed class FakeCustomerRepository(Customer initial) : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _customers = new() { [initial.Id] = initial };
        private readonly Dictionary<Guid, List<Address>> _addresses = new() { [initial.Id] = [] };

        public Task<ResultDto<Customer>> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default)
        {
            var customer = _customers.Values.SingleOrDefault(x => x.Mobile == mobile);
            var result = new ResultDto<Customer>();
            return Task.FromResult(customer is null ? result.NotFound("مشتری پیدا نشد.") : result.Success("مشتری دریافت شد.", customer));
        }

        public Task<ResultDto<Customer>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = new ResultDto<Customer>();
            return Task.FromResult(_customers.TryGetValue(id, out var customer) ? result.Success("مشتری دریافت شد.", customer) : result.NotFound("مشتری پیدا نشد."));
        }

        public Task<ResultDto<IReadOnlyCollection<Customer>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Customer>>().Success("مشتریان دریافت شدند.", _customers.Values.ToArray()));

        public Task<ResultDto<Customer>> UpsertAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            _customers[customer.Id] = customer;
            if (!_addresses.ContainsKey(customer.Id)) _addresses[customer.Id] = customer.Addresses.ToList();
            return Task.FromResult(new ResultDto<Customer>().Success("مشتری ذخیره شد.", customer));
        }

        public Task<ResultDto<IReadOnlyCollection<Address>>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Address> data = _addresses.TryGetValue(customerId, out var list) ? list.OrderByDescending(x => x.IsDefault).ToArray() : Array.Empty<Address>();
            return Task.FromResult(new ResultDto<IReadOnlyCollection<Address>>().Success("آدرس‌ها دریافت شدند.", data));
        }

        public Task<ResultDto<Address>> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
        {
            var address = _addresses.TryGetValue(customerId, out var list) ? list.FirstOrDefault(x => x.Id == addressId) : null;
            var result = new ResultDto<Address>();
            return Task.FromResult(address is null ? result.NotFound("آدرس پیدا نشد.") : result.Success("آدرس دریافت شد.", address));
        }

        public Task<ResultDto<Address>> UpsertAddressAsync(Guid customerId, Address address, CancellationToken cancellationToken = default)
        {
            if (!_addresses.TryGetValue(customerId, out var list)) _addresses[customerId] = list = [];
            var saved = address.Id == Guid.Empty
                ? new Address(
                    Guid.NewGuid(),
                    address.RecipientName,
                    address.Mobile,
                    address.Province,
                    address.City,
                    address.PostalCode,
                    address.AddressLine,
                    address.Plaque,
                    address.Unit,
                    address.IsDefault)
                : address;
            var makeDefault = saved.IsDefault || list.Count == 0;
            list.RemoveAll(x => x.Id == saved.Id);
            if (makeDefault) list = list.Select(x => x with { IsDefault = false }).ToList();
            saved = saved with { IsDefault = makeDefault };
            list.Add(saved);
            _addresses[customerId] = list;
            return Task.FromResult(new ResultDto<Address>().Success("آدرس ذخیره شد.", saved));
        }

        public Task<ResultDto> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
        {
            var result = new ResultDto();
            if (!_addresses.TryGetValue(customerId, out var list)) return Task.FromResult(result.NotFound("مشتری پیدا نشد."));
            var wasDefault = list.FirstOrDefault(x => x.Id == addressId)?.IsDefault == true;
            if (!list.RemoveAll(x => x.Id == addressId).Equals(1)) return Task.FromResult(result.NotFound("آدرس پیدا نشد."));
            if (wasDefault && list.Count > 0 && !list.Any(x => x.IsDefault)) list[0] = list[0] with { IsDefault = true };
            return Task.FromResult(result.Success("آدرس حذف شد."));
        }
    }

    private sealed class EmptyOrderRepository : IOrderRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Order>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Order>>().Success("سفارش‌ها دریافت شدند.", Array.Empty<Order>()));
        public Task<ResultDto<Order>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().NotFound("سفارش پیدا نشد."));
        public Task<ResultDto<Order>> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().NotFound("سفارش پیدا نشد."));
        public Task<ResultDto<Order>> AddAsync(Order order, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().Success("سفارش ثبت شد.", order));
        public Task<ResultDto<Order>> UpdateAsync(Order order, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().Success("سفارش به‌روزرسانی شد.", order));
        public Task<ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>().Success("تاریخچه دریافت شد.", Array.Empty<OrderStatusHistoryDto>()));
        public Task<ResultDto<OrderStatusHistoryDto>> AddStatusHistoryAsync(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string title, string? note, string? trackingCode, string changedBy, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<OrderStatusHistoryDto>().Success("تاریخچه ثبت شد.", new OrderStatusHistoryDto(Guid.NewGuid(), orderId, fromStatus?.ToString(), null, toStatus.ToString(), toStatus.ToString(), title, note, trackingCode, changedBy, DateTimeOffset.UtcNow)));
    }
}
