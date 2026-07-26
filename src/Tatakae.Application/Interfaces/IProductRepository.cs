using Tatakae.Application.Contracts.Common;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Interfaces;

public interface IProductRepository
{
    Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<Product>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ResultDto<Product>> UpsertAsync(Product product, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
