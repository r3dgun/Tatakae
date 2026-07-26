using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Interfaces;

public sealed record WishlistEntry(Guid Id, Guid CustomerId, Guid ProductId, DateTimeOffset CreatedAt);

public interface IWishlistRepository
{
    Task<ResultDto<IReadOnlyCollection<WishlistEntry>>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> ExistsAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<WishlistEntry>> AddAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto> RemoveAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
}
