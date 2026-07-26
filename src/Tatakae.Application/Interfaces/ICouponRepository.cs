using Tatakae.Application.Contracts.Common;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Interfaces;

/// <summary>
/// Result-based persistence contract for coupons. Repository failures and
/// not-found outcomes are returned as <see cref="ResultDto"/> instead of
/// leaking infrastructure exceptions into the application layer.
/// </summary>
public interface ICouponRepository
{
    Task<ResultDto<IReadOnlyCollection<Coupon>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<Coupon>> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<ResultDto<Coupon>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<Coupon>> UpsertAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
