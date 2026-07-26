using System.Net.Http.Json;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Lookups;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Contracts.Wishlist;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface IStoreApiClient
{
    Task<ResultDto<PagedResult<ProductCardDto>>> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductFilterDto>> GetProductFiltersAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductDetailDto>> GetProductAsync(string slug, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> GetProductReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductRatingSummaryDto>> GetProductRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductReviewDto>> SubmitProductReviewAsync(CreateProductReviewRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> GetProductQuestionsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductQuestionDto>> SubmitProductQuestionAsync(SubmitProductQuestionRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> GetSimilarProductsAsync(string slug, int take = 6, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<StorePolicyPageDto>> GetPolicyPageAsync(string slug, CancellationToken cancellationToken = default);
    Task<ResultDto> SubmitContactMessageAsync(SubmitContactMessageRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProvinceLocationDto>>> GetLocationsAsync(CancellationToken cancellationToken = default);
}
