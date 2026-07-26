using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Contracts.Admin;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Contracts.Customers;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Contracts.Seo;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Interfaces.Services;

public interface IAccountService
{
    Task<ResultDto<AccountSessionDto>> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<AccountSessionDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<AccountProfileDto>> ProfileAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<OrderDto>>> OrdersAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<OrderTrackingDto>> OrderTrackingAsync(string mobile, Guid orderId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<CustomerAddressDto>>> AddressesAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<CustomerAddressDto>> UpsertAddressAsync(string mobile, Guid? addressId, CustomerAddressRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAddressAsync(string mobile, Guid addressId, CancellationToken cancellationToken = default);
}

public interface IAdminCatalogService
{
    Task<ResultDto<IReadOnlyCollection<ProductDetailDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<ProductDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductDetailDto>> CreateAsync(AdminProductRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductDetailDto>> UpdateAsync(Guid id, AdminProductRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAdminCategoryService
{
    Task<ResultDto<IReadOnlyCollection<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<CategoryDto>> CreateAsync(AdminCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<CategoryDto>> UpdateAsync(Guid id, AdminCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAdminDashboardService
{
    Task<ResultDto<AdminDashboardDto>> GetAsync(CancellationToken cancellationToken = default);
}

public interface ICatalogService
{
    Task<ResultDto<PagedResult<ProductCardDto>>> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductFilterDto>> GetFiltersAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductDetailDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<CategoryDto>>> GetNavigationCategoriesAsync(CancellationToken cancellationToken = default);
}

public interface ICustomerService
{
    Task<ResultDto<IReadOnlyCollection<CustomerDto>>> GetAllAsync(CancellationToken cancellationToken = default);
}

public interface IEmbroideryArtworkService
{
    EmbroideryArtworkPolicyDto Policy { get; }
    Task<ResultDto<EmbroideryArtworkDto>> SubmitAsync(string? mobile, SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> GetMineAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> AdminListAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ResultDto<EmbroideryArtworkDto>> AdminModerateAsync(Guid id, AdminArtworkModerationRequest request, CancellationToken cancellationToken = default);
}

public interface IEmbroideryPricingService
{
    ResultDto<EmbroideryQuoteDto> Quote(Product product, EmbroideryCustomizationRequest request);
    ResultDto<EmbroideryConfiguration> CreateConfiguration(Product product, EmbroideryCustomizationRequest request);
}

public interface IInventoryService
{
    Task<ResultDto<IReadOnlyCollection<InventoryVariantDto>>> GetInventoryAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<InventoryVariantDto>> AdjustAsync(InventoryAdjustmentRequest request, CancellationToken cancellationToken = default);
}

public interface IMediaAssetService
{
    UploadPolicyDto Policy { get; }
    Task<ResultDto<IReadOnlyCollection<FileUploadDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<FileUploadDto>> AddStoredFileAsync(CreateStoredFileRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<ResultDto<NotificationSummaryDto>> GetMineAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CountUnreadAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> MarkReadAsync(string mobile, Guid notificationId, CancellationToken cancellationToken = default);
    Task<ResultDto> MarkAllReadAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<NotificationDto>>> AdminListAsync(AdminNotificationFilter filter, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> AdminCreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> AdminUpdateStatusAsync(Guid id, UpdateNotificationStatusRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> QueueOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> QueueOrderStatusChangedAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> QueuePaymentResultAsync(PaymentReceiptDto receipt, string customerMobile, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<ResultDto<EmbroideryQuoteDto>> QuoteEmbroideryAsync(EmbroideryCustomizationRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<OrderDto>> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<OrderDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<OrderDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<OrderDto>> UpdateStatusAsync(Guid id, OrderStatus status, string? trackingCode, string? adminNote, CancellationToken cancellationToken = default, bool force = false, string changedBy = "admin");
    Task<ResultDto<AdminOrderWorkflowDto>> GetWorkflowAsync(Guid id, CancellationToken cancellationToken = default);
    ResultDto<IReadOnlyCollection<OrderStatusOptionDto>> GetStatusOptions();
}

public interface IProductEngagementService
{
    Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> GetApprovedReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductRatingSummaryDto>> GetRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductReviewDto>> CreateReviewAsync(string mobile, CreateProductReviewRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<AdminProductReviewDto>>> GetReviewsForAdminAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ResultDto<AdminProductReviewDto>> ModerateReviewAsync(Guid reviewId, AdminReviewModerationRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> GetPublicQuestionsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductQuestionDto>> SubmitQuestionAsync(SubmitProductQuestionRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>> GetQuestionsForAdminAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ResultDto<AdminProductQuestionDto>> ModerateQuestionAsync(Guid questionId, AdminQuestionModerationRequest request, Guid? answeredByUserId = null, CancellationToken cancellationToken = default);
}

public interface ISeoService
{
    ResultDto<IReadOnlyCollection<SeoRoutePolicyDto>> GetRoutePolicies();
    Task<ResultDto<SeoSitemapDocumentDto>> GetSitemapAsync(string? publicBaseUrl, CancellationToken cancellationToken = default);
    Task<ResultDto<SeoAuditSummaryDto>> AuditAsync(string? publicBaseUrl, CancellationToken cancellationToken = default);
    Task<ResultDto<AiSeoDocumentDto>> GetLlmsDocumentAsync(string? publicBaseUrl, AiSeoSiteProfileDto profile, bool includeFullCatalog = false, CancellationToken cancellationToken = default);
    Task<ResultDto<AiCatalogDocumentDto>> GetAiCatalogAsync(string? publicBaseUrl, AiSeoSiteProfileDto profile, CancellationToken cancellationToken = default);
}

public interface IShippingService
{
    Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> GetAdminMethodsAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> GetCheckoutMethodsAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<ShippingMethodDto>> ResolveCheckoutMethodAsync(string code, decimal cartSubtotal, CancellationToken cancellationToken = default);
    Task<ResultDto<ShippingMethodDto>> UpsertAsync(Guid? id, UpsertManualShippingMethodRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWishlistService
{
    Task<ResultDto<WishlistDto>> GetAsync(string mobile, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> IsWishlistedAsync(string mobile, Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<WishlistToggleResultDto>> ToggleAsync(string mobile, Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto> RemoveAsync(string mobile, Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> RecommendationsAsync(string mobile, RecommendationQuery query, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<ProductRecommendationDto>>> SimilarAsync(string slug, int take = 6, CancellationToken cancellationToken = default);
}
