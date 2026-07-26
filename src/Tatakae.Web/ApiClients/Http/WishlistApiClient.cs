using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;
using Tatakae.Web.State;

namespace Tatakae.Web.ApiClients.Http;

public sealed class WishlistApiClient(IApiClientTransport transport, IAuthSessionStore auth) : IWishlistApiClient
{
    public async Task<ResultDto<WishlistDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (authResult is not null)
            return CopyFailure<WishlistDto>(authResult);

        return await transport.GetResultAsync<WishlistDto>(
            "api/account/wishlist",
            "دریافت علاقه‌مندی‌ها ناموفق بود.",
            cancellationToken);
    }

    public async Task<ResultDto<WishlistToggleResultDto>> ToggleAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            return new ResultDto<WishlistToggleResultDto>().ValidationFailed("شناسه محصول معتبر نیست.", "invalid_product_id");

        var authResult = await EnsureAuthenticatedAsync();
        if (authResult is not null)
            return CopyFailure<WishlistToggleResultDto>(authResult);

        return await transport.SendResultAsync<WishlistToggleResultDto>(
            HttpMethod.Post,
            $"api/account/wishlist/{productId}/toggle",
            null,
            "به‌روزرسانی علاقه‌مندی ناموفق بود.",
            cancellationToken);
    }

    public async Task<ResultDto<bool>> IsWishlistedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            return new ResultDto<bool>().ValidationFailed("شناسه محصول معتبر نیست.", "invalid_product_id");

        var authResult = await EnsureAuthenticatedAsync();
        if (authResult is not null)
            return CopyFailure<bool>(authResult);

        var result = await transport.GetResultAsync<WishlistStatusResponse>(
            $"api/account/wishlist/{productId}/status",
            "دریافت وضعیت علاقه‌مندی ناموفق بود.",
            cancellationToken);

        if (!result.IsSuccess)
            return CopyFailure<bool>(result);

        return new ResultDto<bool>().Success(
            result.Message,
            result.Data?.IsWishlisted == true);
    }

    public async Task<ResultDto> RemoveAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            return new ResultDto().ValidationFailed("شناسه محصول معتبر نیست.", "invalid_product_id");

        var authResult = await EnsureAuthenticatedAsync();
        if (authResult is not null)
            return authResult;

        return await transport.SendResultAsync(
            HttpMethod.Delete,
            $"api/account/wishlist/{productId}",
            null,
            "حذف علاقه‌مندی ناموفق بود.",
            cancellationToken);
    }

    public async Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> RecommendationsAsync(
        string? contextSlug = null,
        int take = 8,
        CancellationToken cancellationToken = default)
    {
        var authResult = await EnsureAuthenticatedAsync();
        if (authResult is not null)
            return CopyFailure<IReadOnlyCollection<ProductRecommendationDto>>(authResult);

        var url = $"api/account/wishlist/recommendations?take={take}" +
                  (string.IsNullOrWhiteSpace(contextSlug)
                      ? string.Empty
                      : $"&contextSlug={Uri.EscapeDataString(contextSlug)}");

        return await transport.GetResultAsync<IReadOnlyCollection<ProductRecommendationDto>>(
            url,
            "دریافت پیشنهادها ناموفق بود.",
            cancellationToken);
    }

    private async Task<ResultDto?> EnsureAuthenticatedAsync()
    {
        await auth.EnsureLoadedAsync();
        return auth.IsSignedIn
            ? null
            : new ResultDto().Unauthorized("برای استفاده از علاقه‌مندی‌ها ابتدا وارد حساب شوید.", "authentication_required");
    }

    private static ResultDto<T> CopyFailure<T>(ResultDto result)
        => new()
        {
            IsSuccess = false,
            Status = result.Status,
            Message = result.Message,
            ErrorCode = result.ErrorCode,
            Errors = result.Errors,
            Data = default
        };

    private sealed class WishlistStatusResponse
    {
        public Guid ProductId { get; set; }
        public bool IsWishlisted { get; set; }
    }
}
