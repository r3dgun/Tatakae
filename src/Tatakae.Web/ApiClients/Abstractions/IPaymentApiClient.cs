using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Payments;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface IPaymentApiClient
{
    Task<ResultDto<PaymentInitDto>> StartAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<PaymentDto>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
