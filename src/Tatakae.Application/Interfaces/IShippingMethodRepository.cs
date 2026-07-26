using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Shipping;

namespace Tatakae.Application.Interfaces;

public interface IShippingMethodRepository
{
    Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<ShippingMethodDto>> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<ResultDto<ShippingMethodDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<ShippingMethodDto>> UpsertAsync(Guid? id, UpsertManualShippingMethodRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
