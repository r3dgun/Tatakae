using Tatakae.Application.Contracts.Common;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Interfaces;

public interface ICategoryRepository
{
    Task<ResultDto<IReadOnlyCollection<Category>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<Category>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<Category>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ResultDto<Category>> UpsertAsync(Category category, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
