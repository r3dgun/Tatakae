using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Lookups;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;

namespace Tatakae.Web.ApiClients.Http;

public sealed class StoreApiClient(IApiClientTransport transport) : IStoreApiClient
{
    public Task<ResultDto<PagedResult<ProductCardDto>>> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<PagedResult<ProductCardDto>>(
            BuildProductQueryUrl("api/products", query),
            "دریافت محصولات ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<ProductFilterDto>> GetProductFiltersAsync(ProductListQuery query, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<ProductFilterDto>(
            BuildProductQueryUrl("api/products/filters", query),
            "دریافت فیلترهای محصول ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<ProductDetailDto>> GetProductAsync(string slug, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<ProductDetailDto>(
            $"api/products/by-slug/{Uri.EscapeDataString(slug)}",
            "دریافت محصول ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> GetProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<IReadOnlyCollection<ProductReviewDto>>(
            $"api/products/{productId}/reviews",
            "دریافت نظرهای محصول ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<ProductRatingSummaryDto>> GetProductRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<ProductRatingSummaryDto>(
            $"api/products/{productId}/reviews/summary",
            "دریافت خلاصه امتیاز محصول ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<ProductReviewDto>> SubmitProductReviewAsync(
        CreateProductReviewRequest request,
        CancellationToken cancellationToken = default)
        => transport.SendResultAsync<ProductReviewDto>(
            HttpMethod.Post,
            $"api/products/{request.ProductId}/reviews",
            request,
            "ثبت نظر انجام نشد.",
            cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> GetProductQuestionsAsync(Guid productId, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<IReadOnlyCollection<ProductQuestionDto>>(
            $"api/products/{productId}/questions",
            "دریافت پرسش‌های محصول ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<ProductQuestionDto>> SubmitProductQuestionAsync(
        SubmitProductQuestionRequest request,
        CancellationToken cancellationToken = default)
        => transport.SendResultAsync<ProductQuestionDto>(
            HttpMethod.Post,
            $"api/products/{request.ProductId}/questions",
            request,
            "ثبت پرسش ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> GetSimilarProductsAsync(string slug, int take = 6, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<IReadOnlyCollection<ProductRecommendationDto>>(
            $"api/recommendations/similar/{Uri.EscapeDataString(slug)}?take={take}",
            "دریافت محصولات مشابه ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => transport.GetResultAsync<IReadOnlyCollection<CategoryDto>>(
            "api/categories",
            "دریافت دسته‌بندی‌ها ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<StorePolicyPageDto>> GetPolicyPageAsync(string slug, CancellationToken cancellationToken = default)
        => transport.GetResultAsync<StorePolicyPageDto>(
            $"api/store-pages/{Uri.EscapeDataString(slug)}",
            "دریافت صفحه اطلاعاتی ناموفق بود.",
            cancellationToken);

    public Task<ResultDto> SubmitContactMessageAsync(SubmitContactMessageRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync(
            HttpMethod.Post,
            "api/store-pages/contact",
            request,
            "ارسال پیام تماس ناموفق بود.",
            cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<ProvinceLocationDto>>> GetLocationsAsync(CancellationToken cancellationToken = default)
        => transport.GetResultAsync<IReadOnlyCollection<ProvinceLocationDto>>(
            "api/locations",
            "دریافت استان‌ها و شهرها ناموفق بود.",
            cancellationToken);

    private static string BuildProductQueryUrl(string endpoint, ProductListQuery query)
    {
        var values = new Dictionary<string, string?>
        {
            ["search"] = query.Search,
            ["category"] = query.Category,
            ["size"] = query.Size,
            ["color"] = query.Color,
            ["minPrice"] = query.MinPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["maxPrice"] = query.MaxPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["inStockOnly"] = query.InStockOnly.ToString().ToLowerInvariant(),
            ["featuredOnly"] = query.FeaturedOnly.ToString().ToLowerInvariant(),
            ["saleOnly"] = query.SaleOnly.ToString().ToLowerInvariant(),
            ["readyMadeOnly"] = query.ReadyMadeOnly.ToString().ToLowerInvariant(),
            ["customizableOnly"] = query.CustomizableOnly.ToString().ToLowerInvariant(),
            ["page"] = query.Page.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["pageSize"] = query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["sort"] = query.Sort
        };

        var queryString = string.Join("&", values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        return string.IsNullOrWhiteSpace(queryString) ? endpoint : $"{endpoint}?{queryString}";
    }
}
