using Microsoft.Extensions.DependencyInjection;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;
using Tatakae.Application.Services;

namespace Tatakae.Application.DependencyInjection;

public static class ApplicationServiceCollection
{
    /// <summary>
    /// Registers Application use cases through their public contracts only.
    /// API and Infrastructure consumers should depend on I...Service interfaces.
    /// </summary>
    public static IServiceCollection AddTatakaeApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IAdminCategoryService, AdminCategoryService>();
        services.AddScoped<IAdminCouponService, AdminCouponService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IEmbroideryArtworkService, EmbroideryArtworkService>();
        services.AddScoped<IEmbroideryPricingService, EmbroideryPricingService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IMediaAssetService, MediaAssetService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductEngagementService, ProductEngagementService>();
        services.AddScoped<ISeoService, SeoService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IWishlistService, WishlistService>();

        services.AddScoped<IIdentityAuthService, IdentityAuthService>();
        services.AddScoped<ILegalContentService, LegalContentService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISecurityAdminService, SecurityAdminService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ICartPersistenceService, CartPersistenceService>();
        services.AddScoped<ILocationService, LocationService>();

        return services;
    }
}
