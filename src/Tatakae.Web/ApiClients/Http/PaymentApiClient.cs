using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;

namespace Tatakae.Web.ApiClients.Http;

public sealed class PaymentApiClient(IApiClientTransport transport) : IPaymentApiClient
{
    public Task<ResultDto<PaymentInitDto>> StartAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<PaymentInitDto>(HttpMethod.Post, "api/payments/start", request, "شروع پرداخت ناموفق بود.", cancellationToken);

    public Task<ResultDto<PaymentDto>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<PaymentDto>($"api/payments/order/{orderId}", "دریافت پرداخت سفارش ناموفق بود.", cancellationToken);

}
