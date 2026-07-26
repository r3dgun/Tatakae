using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;

namespace Tatakae.Application.Interfaces.Services;

public interface IAdminCouponService
{
    Task<ResultDto<IReadOnlyCollection<CouponDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<CouponDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<CouponDto>> CreateAsync(AdminCouponRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<CouponDto>> UpdateAsync(Guid id, AdminCouponRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
