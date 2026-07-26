using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;

namespace Tatakae.Web.ApiClients.Http;

public sealed class CheckoutApiClient(IApiClientTransport transport) : ICheckoutApiClient
{
    public Task<ResultDto<EmbroideryQuoteDto>> QuoteAsync(EmbroideryCustomizationRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<EmbroideryQuoteDto>(HttpMethod.Post, "api/checkout/quote-embroidery", request, "قیمت‌گذاری گلدوزی دریافت نشد.", cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> ShippingMethodsAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<IReadOnlyCollection<ShippingMethodDto>>(HttpMethod.Post, "api/shipping/quote", request, "روش‌های ارسال دریافت نشد.", cancellationToken);

    public Task<ResultDto<CouponQuoteDto>> QuoteCouponAsync(CouponQuoteRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<CouponQuoteDto>(HttpMethod.Post, "api/coupons/quote", request, "پاسخ کد تخفیف دریافت نشد.", cancellationToken);

    public Task<ResultDto<OrderDto>> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<OrderDto>(HttpMethod.Post, "api/checkout", request, "ثبت سفارش ناموفق بود.", cancellationToken);
}
