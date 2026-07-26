using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;

namespace Tatakae.Application.Interfaces.Services;

public interface ICouponService
{
    Task<ResultDto<CouponQuoteDto>> QuoteAsync(CouponQuoteRequest request, CancellationToken cancellationToken = default);
}
