using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Shipping;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface ICheckoutApiClient
{
    Task<ResultDto<EmbroideryQuoteDto>> QuoteAsync(EmbroideryCustomizationRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> ShippingMethodsAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<CouponQuoteDto>> QuoteCouponAsync(CouponQuoteRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<OrderDto>> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
}
