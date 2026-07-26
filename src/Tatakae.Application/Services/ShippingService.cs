using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class ShippingService(
    IShippingMethodRepository shippingMethods,
    ILogger<ShippingService>? logger = null) : IShippingService
{
    private readonly ILogger<ShippingService> _logger = logger ?? NullLogger<ShippingService>.Instance;
    public async Task<IReadOnlyCollection<ShippingMethodDto>> GetAdminMethodsAsync(CancellationToken cancellationToken = default)
        => (await shippingMethods.GetAllAsync(cancellationToken)).RequireData();

    public async Task<IReadOnlyCollection<ShippingMethodDto>> GetCheckoutMethodsAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var methods = (await shippingMethods.GetActiveAsync(cancellationToken)).RequireData();
        return methods
            .Select(method => method with
            {
                Price = CalculatePrice(method, request.CartSubtotal),
                IsAvailable = true
            })
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Price)
            .ThenBy(x => x.EstimatedMinDays)
            .ToArray();
    }

    public async Task<ShippingMethodDto> ResolveCheckoutMethodAsync(string code, decimal cartSubtotal, CancellationToken cancellationToken = default)
    {
        var method = (await shippingMethods.GetByCodeAsync(code, cancellationToken)).RequireData();
        if (!method.IsActive) throw new ArgumentException("روش ارسال انتخاب‌شده فعال نیست.");
        return method with { Price = CalculatePrice(method, cartSubtotal), IsAvailable = true };
    }

    public async Task<ShippingMethodDto> UpsertAsync(Guid? id, UpsertManualShippingMethodRequest request, CancellationToken cancellationToken = default)
        => (await shippingMethods.UpsertAsync(id, request, cancellationToken)).RequireData();

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => (await shippingMethods.DeleteAsync(id, cancellationToken)).EnsureSuccess();

    private static decimal CalculatePrice(ShippingMethodDto method, decimal cartSubtotal)
        => method.FreeShippingThreshold.HasValue && cartSubtotal >= method.FreeShippingThreshold.Value ? 0m : method.BasePrice;
}
