using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Admin;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Contracts.Customers;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Contracts.Security;
using Tatakae.Application.Contracts.Seo;
using Tatakae.Application.Contracts.Shipping;

namespace Tatakae.Web.ApiClients.Http;

public sealed class AdminApiClient(IApiClientTransport transport) : IAdminApiClient
{
    public Task<ResultDto<AdminDashboardDto>> DashboardAsync(CancellationToken cancellationToken = default) => GetAsync<AdminDashboardDto>("api/admin/dashboard", cancellationToken);
    public Task<ResultDto<SeoAuditSummaryDto>> SeoAuditAsync(CancellationToken cancellationToken = default) => GetAsync<SeoAuditSummaryDto>("api/admin/seo/audit", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<NotificationDto>>> NotificationsAsync(AdminNotificationFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new AdminNotificationFilter();
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Status)) qs.Add($"status={Uri.EscapeDataString(filter.Status)}");
        if (!string.IsNullOrWhiteSpace(filter.Channel)) qs.Add($"channel={Uri.EscapeDataString(filter.Channel)}");
        if (!string.IsNullOrWhiteSpace(filter.Type)) qs.Add($"type={Uri.EscapeDataString(filter.Type)}");
        if (!string.IsNullOrWhiteSpace(filter.Search)) qs.Add($"search={Uri.EscapeDataString(filter.Search)}");
        qs.Add($"take={filter.Take}");
        var url = "api/admin/notifications" + (qs.Count > 0 ? "?" + string.Join("&", qs) : string.Empty);
        return GetAsync<IReadOnlyCollection<NotificationDto>>(url, cancellationToken);
    }
    public Task<ResultDto<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default) => SendAsync<NotificationDto>(HttpMethod.Post, "api/admin/notifications", request, cancellationToken);
    public Task<ResultDto<NotificationDto>> UpdateNotificationStatusAsync(Guid id, UpdateNotificationStatusRequest request, CancellationToken cancellationToken = default) => SendAsync<NotificationDto>(HttpMethod.Patch, $"api/admin/notifications/{id}/status", request, cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<SeoRoutePolicyDto>>> SeoRoutesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<SeoRoutePolicyDto>>("api/admin/seo/routes", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<ProductDetailDto>>> ProductsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<ProductDetailDto>>("api/admin/products", cancellationToken);
    public Task<ResultDto<ProductDetailDto>> ProductAsync(Guid id, CancellationToken cancellationToken = default) => GetAsync<ProductDetailDto>($"api/admin/products/{id}", cancellationToken);
    public Task<ResultDto<ProductDetailDto>> CreateProductAsync(AdminProductRequest request, CancellationToken cancellationToken = default) => SendAsync<ProductDetailDto>(HttpMethod.Post, "api/admin/products", request, cancellationToken);
    public Task<ResultDto<ProductDetailDto>> UpdateProductAsync(Guid id, AdminProductRequest request, CancellationToken cancellationToken = default) => SendAsync<ProductDetailDto>(HttpMethod.Put, $"api/admin/products/{id}", request, cancellationToken);
    public Task<ResultDto> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default) => DeleteAsync($"api/admin/products/{id}", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<InventoryVariantDto>>> InventoryAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<InventoryVariantDto>>("api/admin/inventory", cancellationToken);
    public Task<ResultDto<InventoryVariantDto>> AdjustInventoryAsync(Guid variantId, InventoryAdjustmentRequest request, CancellationToken cancellationToken = default) => SendAsync<InventoryVariantDto>(HttpMethod.Patch, $"api/admin/inventory/{variantId}/adjust", request, cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<CategoryDto>>> CategoriesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<CategoryDto>>("api/admin/categories", cancellationToken);
    public Task<ResultDto<CategoryDto>> CreateCategoryAsync(AdminCategoryRequest request, CancellationToken cancellationToken = default) => SendAsync<CategoryDto>(HttpMethod.Post, "api/admin/categories", request, cancellationToken);
    public Task<ResultDto<CategoryDto>> UpdateCategoryAsync(Guid id, AdminCategoryRequest request, CancellationToken cancellationToken = default) => SendAsync<CategoryDto>(HttpMethod.Put, $"api/admin/categories/{id}", request, cancellationToken);
    public Task<ResultDto> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default) => DeleteAsync($"api/admin/categories/{id}", cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<OrderDto>>> OrdersAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<OrderDto>>("api/admin/orders", cancellationToken);
    public Task<ResultDto<OrderDto>> OrderAsync(Guid id, CancellationToken cancellationToken = default) => GetAsync<OrderDto>($"api/admin/orders/{id}", cancellationToken);
    public Task<ResultDto<AdminOrderWorkflowDto>> OrderWorkflowAsync(Guid id, CancellationToken cancellationToken = default) => GetAsync<AdminOrderWorkflowDto>($"api/admin/orders/{id}/workflow", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<OrderStatusOptionDto>>> OrderStatusOptionsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<OrderStatusOptionDto>>("api/admin/orders/status-options", cancellationToken);
    public Task<ResultDto<OrderDto>> UpdateOrderStatusAsync(Guid id, AdminOrderStatusRequest request, CancellationToken cancellationToken = default) => SendAsync<OrderDto>(HttpMethod.Patch, $"api/admin/orders/{id}/status", request, cancellationToken);
    public Task<ResultDto<AdminPaymentsDto>> AdminPaymentsAsync(CancellationToken cancellationToken = default) => GetAsync<AdminPaymentsDto>("api/admin/payments", cancellationToken);
    public Task<ResultDto<PaymentDto>> UpdatePaymentStatusAsync(Guid id, UpdatePaymentStatusRequest request, CancellationToken cancellationToken = default) => SendAsync<PaymentDto>(HttpMethod.Patch, $"api/admin/payments/{id}/status", request, cancellationToken);
    public Task<ResultDto<PaymentRefundDto>> RefundZarinpalAsync(Guid id, CreateZarinpalRefundRequest request, CancellationToken cancellationToken = default) => SendAsync<PaymentRefundDto>(HttpMethod.Post, $"api/admin/payments/{id}/refund", request, cancellationToken);


    public Task<ResultDto<IReadOnlyCollection<AdminProductReviewDto>>> ReviewsAsync(string? status = null, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyCollection<AdminProductReviewDto>>(string.IsNullOrWhiteSpace(status) ? "api/admin/reviews" : $"api/admin/reviews?status={Uri.EscapeDataString(status)}", cancellationToken);
    public Task<ResultDto<AdminProductReviewDto>> ModerateReviewAsync(Guid id, AdminReviewModerationRequest request, CancellationToken cancellationToken = default)
        => SendAsync<AdminProductReviewDto>(HttpMethod.Patch, $"api/admin/reviews/{id}/moderate", request, cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>> QuestionsAsync(string? status = null, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyCollection<AdminProductQuestionDto>>(string.IsNullOrWhiteSpace(status) ? "api/admin/questions" : $"api/admin/questions?status={Uri.EscapeDataString(status)}", cancellationToken);
    public Task<ResultDto<AdminProductQuestionDto>> ModerateQuestionAsync(Guid id, AdminQuestionModerationRequest request, CancellationToken cancellationToken = default)
        => SendAsync<AdminProductQuestionDto>(HttpMethod.Patch, $"api/admin/questions/{id}/moderate", request, cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<CustomerDto>>> CustomersAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<CustomerDto>>("api/admin/customers", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<CouponDto>>> CouponsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyCollection<CouponDto>>("api/admin/coupons", cancellationToken);

    public Task<ResultDto<CouponDto>> CreateCouponAsync(AdminCouponRequest request, CancellationToken cancellationToken = default)
        => SendAsync<CouponDto>(HttpMethod.Post, "api/admin/coupons", request, cancellationToken);

    public Task<ResultDto<CouponDto>> UpdateCouponAsync(Guid id, AdminCouponRequest request, CancellationToken cancellationToken = default)
        => SendAsync<CouponDto>(HttpMethod.Put, $"api/admin/coupons/{id}", request, cancellationToken);

    public Task<ResultDto> DeleteCouponAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/admin/coupons/{id}", cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<ShippingMethodDto>>> ShippingMethodsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<ShippingMethodDto>>("api/admin/shipping-methods", cancellationToken);
    public Task<ResultDto<ShippingMethodDto>> CreateShippingMethodAsync(UpsertManualShippingMethodRequest request, CancellationToken cancellationToken = default) => SendAsync<ShippingMethodDto>(HttpMethod.Post, "api/admin/shipping-methods", request, cancellationToken);
    public Task<ResultDto<ShippingMethodDto>> UpdateShippingMethodAsync(Guid id, UpsertManualShippingMethodRequest request, CancellationToken cancellationToken = default) => SendAsync<ShippingMethodDto>(HttpMethod.Put, $"api/admin/shipping-methods/{id}", request, cancellationToken);
    public Task<ResultDto> DeleteShippingMethodAsync(Guid id, CancellationToken cancellationToken = default) => DeleteAsync($"api/admin/shipping-methods/{id}", cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<FileUploadDto>>> MediaAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<FileUploadDto>>("api/admin/media", cancellationToken);
    public Task<ResultDto> DeleteMediaAsync(Guid id, CancellationToken cancellationToken = default) => DeleteAsync($"api/admin/media/{id}", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> AdminArtworksAsync(string? status = null, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyCollection<EmbroideryArtworkDto>>(string.IsNullOrWhiteSpace(status) ? "api/admin/artworks" : $"api/admin/artworks?status={Uri.EscapeDataString(status)}", cancellationToken);
    public Task<ResultDto<EmbroideryArtworkDto>> ModerateArtworkAsync(Guid id, AdminArtworkModerationRequest request, CancellationToken cancellationToken = default)
        => SendAsync<EmbroideryArtworkDto>(HttpMethod.Patch, $"api/admin/artworks/{id}/moderate", request, cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<StorePolicyPageDto>>> LegalPagesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<StorePolicyPageDto>>("api/admin/legal/pages", cancellationToken);
    public Task<ResultDto<StorePolicyPageDto>> UpdateLegalPageAsync(string slug, UpsertStorePolicyPageRequest request, CancellationToken cancellationToken = default) => SendAsync<StorePolicyPageDto>(HttpMethod.Put, $"api/admin/legal/pages/{Uri.EscapeDataString(slug)}", request, cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<ContactMessageDto>>> ContactMessagesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<ContactMessageDto>>("api/admin/legal/contact-messages", cancellationToken);
    public Task<ResultDto<ContactMessageDto>> UpdateContactMessageAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default) => SendAsync<ContactMessageDto>(HttpMethod.Patch, $"api/admin/legal/contact-messages/{id}", request, cancellationToken);

    public Task<ResultDto<IReadOnlyCollection<PermissionDto>>> PermissionsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<PermissionDto>>("api/admin/security/permissions", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<AdminPageAccessDto>>> AdminPagesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<AdminPageAccessDto>>("api/admin/security/admin-pages", cancellationToken);
    public Task<ResultDto<AdminPageAccessDto>> UpdateAdminPageAsync(Guid id, UpsertAdminPageAccessRequest request, CancellationToken cancellationToken = default) => SendAsync<AdminPageAccessDto>(HttpMethod.Put, $"api/admin/security/admin-pages/{id}", request, cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<RoleSecurityDto>>> RolesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<RoleSecurityDto>>("api/admin/security/roles", cancellationToken);
    public Task<ResultDto<RoleSecurityDto>> CreateRoleAsync(UpsertRoleRequest request, CancellationToken cancellationToken = default) => SendAsync<RoleSecurityDto>(HttpMethod.Post, "api/admin/security/roles", request, cancellationToken);
    public Task<ResultDto<RoleSecurityDto>> UpdateRolePermissionsAsync(Guid roleId, AssignRolePermissionsRequest request, CancellationToken cancellationToken = default) => SendAsync<RoleSecurityDto>(HttpMethod.Put, $"api/admin/security/roles/{roleId}/permissions", request, cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<AdminUserDto>>> SecurityUsersAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<AdminUserDto>>("api/admin/security/users", cancellationToken);
    public Task<ResultDto<IReadOnlyCollection<LoginAuditDto>>> LoginAuditsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyCollection<LoginAuditDto>>("api/admin/security/login-audits", cancellationToken);
    public Task<ResultDto<AdminUserDto>> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default) => SendAsync<AdminUserDto>(HttpMethod.Post, "api/admin/security/users", request, cancellationToken);
    public Task<ResultDto<AdminUserDto>> UpdateUserRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken = default) => SendAsync<AdminUserDto>(HttpMethod.Put, $"api/admin/security/users/{userId}/roles", request, cancellationToken);


    private Task<ResultDto<T>> GetAsync<T>(string url, CancellationToken cancellationToken)
        => transport.GetResultAsync<T>(url, "دریافت اطلاعات مدیریت ناموفق بود.", cancellationToken);

    private Task<ResultDto<T>> SendAsync<T>(HttpMethod method, string url, object data, CancellationToken cancellationToken)
        => transport.SendResultAsync<T>(method, url, data, "ثبت اطلاعات مدیریت ناموفق بود.", cancellationToken);

    private Task<ResultDto> DeleteAsync(string url, CancellationToken cancellationToken)
        => transport.SendResultAsync(HttpMethod.Delete, url, null, "حذف اطلاعات مدیریت ناموفق بود.", cancellationToken);
}
