using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Infrastructure.Repositories;

public sealed class InMemoryCustomerRepository(ILogger<InMemoryCustomerRepository>? logger = null) : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _data = new(StoreSeed.CreateCustomers().ToDictionary(x => x.Id));
    private readonly ILogger<InMemoryCustomerRepository> _logger = logger ?? NullLogger<InMemoryCustomerRepository>.Instance;

    public Task<ResultDto<Customer>> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Customer>();
        if (string.IsNullOrWhiteSpace(mobile)) return Task.FromResult(result.ValidationFailed("شماره موبایل معتبر نیست."));
        try
        {
            var customer = _data.Values.SingleOrDefault(x => x.Mobile == mobile.Trim());
            return Task.FromResult(customer is null ? result.NotFound("مشتری پیدا نشد.") : result.Success("مشتری دریافت شد.", customer));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت مشتری با موبایل {Mobile}", mobile); return Task.FromResult(result.Failed("خطایی در دریافت مشتری رخ داده است.")); }
    }

    public Task<ResultDto<Customer>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Customer>();
        if (id == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه مشتری معتبر نیست."));
        try { return Task.FromResult(_data.TryGetValue(id, out var customer) ? result.Success("مشتری دریافت شد.", customer) : result.NotFound("مشتری پیدا نشد.")); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت مشتری {CustomerId}", id); return Task.FromResult(result.Failed("خطایی در دریافت مشتری رخ داده است.")); }
    }

    public Task<ResultDto<IReadOnlyCollection<Customer>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<Customer>>();
        try { return Task.FromResult(result.Success("مشتریان دریافت شدند.", _data.Values.ToArray())); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت مشتریان"); return Task.FromResult(result.Failed("خطایی در دریافت مشتریان رخ داده است.")); }
    }

    public Task<ResultDto<Customer>> UpsertAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Customer>();
        if (customer is null) return Task.FromResult(result.ValidationFailed("اطلاعات مشتری ارسال نشده است."));
        try { _data[customer.Id] = customer; return Task.FromResult(result.Success("مشتری ذخیره شد.", customer)); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ذخیره مشتری {CustomerId}", customer.Id); return Task.FromResult(result.Failed("خطایی در ذخیره مشتری رخ داده است.")); }
    }

    public Task<ResultDto<IReadOnlyCollection<Address>>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<Address>>();
        if (customerId == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه مشتری معتبر نیست."));
        try
        {
            if (!_data.TryGetValue(customerId, out var customer)) return Task.FromResult(result.NotFound("مشتری پیدا نشد."));
            return Task.FromResult(result.Success("آدرس‌ها دریافت شدند.", customer.Addresses.OrderByDescending(x => x.IsDefault).ToArray()));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت آدرس‌های مشتری {CustomerId}", customerId); return Task.FromResult(result.Failed("خطایی در دریافت آدرس‌ها رخ داده است.")); }
    }

    public Task<ResultDto<Address>> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Address>();
        if (customerId == Guid.Empty || addressId == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه مشتری یا آدرس معتبر نیست."));
        try
        {
            if (!_data.TryGetValue(customerId, out var customer)) return Task.FromResult(result.NotFound("مشتری پیدا نشد."));
            var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
            return Task.FromResult(address is null ? result.NotFound("آدرس پیدا نشد.") : result.Success("آدرس دریافت شد.", address));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت آدرس {AddressId}", addressId); return Task.FromResult(result.Failed("خطایی در دریافت آدرس رخ داده است.")); }
    }

    public Task<ResultDto<Address>> UpsertAddressAsync(Guid customerId, Address address, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Address>();
        if (customerId == Guid.Empty || address is null) return Task.FromResult(result.ValidationFailed("اطلاعات آدرس کامل نیست."));
        try
        {
            if (!_data.TryGetValue(customerId, out var customer)) return Task.FromResult(result.NotFound("مشتری پیدا نشد."));
            var addresses = customer.Addresses.Where(x => x.Id != address.Id).ToList();
            if (address.IsDefault) addresses = addresses.Select(x => x with { IsDefault = false }).ToList();
            if (!addresses.Any() && !address.IsDefault) address = address with { IsDefault = true };
            addresses.Add(address);
            _data[customerId] = Customer.Rehydrate(customer.Id, customer.FullName, customer.Mobile, customer.Email, customer.CreatedAt, addresses.OrderByDescending(x => x.IsDefault).ToArray());
            return Task.FromResult(result.Success("آدرس ذخیره شد.", address));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ذخیره آدرس مشتری {CustomerId}", customerId); return Task.FromResult(result.Failed("خطایی در ذخیره آدرس رخ داده است.")); }
    }

    public Task<ResultDto> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto();
        if (customerId == Guid.Empty || addressId == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه مشتری یا آدرس معتبر نیست."));
        try
        {
            if (!_data.TryGetValue(customerId, out var customer)) return Task.FromResult(result.NotFound("مشتری پیدا نشد."));
            if (!customer.Addresses.Any(x => x.Id == addressId)) return Task.FromResult(result.NotFound("آدرس پیدا نشد."));
            var addresses = customer.Addresses.Where(x => x.Id != addressId).ToList();
            if (addresses.Count > 0 && !addresses.Any(x => x.IsDefault)) addresses[0] = addresses[0] with { IsDefault = true };
            _data[customerId] = Customer.Rehydrate(customer.Id, customer.FullName, customer.Mobile, customer.Email, customer.CreatedAt, addresses.ToArray());
            return Task.FromResult(result.Success("آدرس حذف شد."));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در حذف آدرس {AddressId}", addressId); return Task.FromResult(result.Failed("خطایی در حذف آدرس رخ داده است.")); }
    }
}
