using Tatakae.Application.Contracts.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Wishlist;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface IWishlistApiClient
{
    Task<ResultDto<WishlistDto>> GetAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<WishlistToggleResultDto>> ToggleAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> IsWishlistedAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto> RemoveAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> RecommendationsAsync(string? contextSlug = null, int take = 8, CancellationToken cancellationToken = default);
}
