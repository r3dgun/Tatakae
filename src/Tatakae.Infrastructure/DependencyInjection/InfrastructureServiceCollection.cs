using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Security;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Infrastructure.Persistence.Repositories;
using Tatakae.Infrastructure.Gateways;
using Tatakae.Infrastructure.Payments.Zarinpal;
using Tatakae.Infrastructure.Inventory;
using Tatakae.Infrastructure.Jobs;

namespace Tatakae.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddTatakaeSqlInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TatakaeSqlServer")
            ?? throw new InvalidOperationException("Connection string 'TatakaeSqlServer' is missing.");

        services.AddDbContext<TatakaeDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(TatakaeDbContext).Assembly.FullName));
        });

        services
            .AddIdentityCore<ApplicationUserIdentity>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRoleIdentity>()
            .AddEntityFrameworkStores<TatakaeDbContext>();

        services.AddScoped<IProductRepository, SqlProductRepository>();
        services.AddScoped<ICategoryRepository, SqlCategoryRepository>();
        services.AddScoped<IOrderRepository, SqlOrderRepository>();
        services.AddScoped<ICustomerRepository, SqlCustomerRepository>();
        services.AddScoped<ICouponRepository, SqlCouponRepository>();
        services.AddScoped<IShippingMethodRepository, SqlShippingMethodRepository>();
        services.AddScoped<IMediaAssetRepository, SqlMediaAssetRepository>();
        services.AddScoped<IWishlistRepository, SqlWishlistRepository>();
        services.AddScoped<IProductEngagementRepository, SqlProductEngagementRepository>();
        services.AddScoped<IEmbroideryArtworkRepository, SqlEmbroideryArtworkRepository>();
        services.AddScoped<INotificationRepository, SqlNotificationRepository>();
        services.AddScoped<IStorePolicyPageReader, SqlStorePolicyPageReader>();

        services.AddScoped<IIdentityAuthGateway, AspNetIdentityAuthGateway>();
        services.AddScoped<ILegalContentGateway, EfLegalContentGateway>();
        services.AddScoped<EfPaymentRepository>();
        services.AddScoped<IPaidOrderInventoryFinalizer, EfPaidOrderInventoryFinalizer>();
        services.AddScoped<IPaymentRepository, ReservationAwarePaymentRepository>();
        services.AddScoped<IInventoryReservationGateway, EfInventoryReservationGateway>();
        services.Configure<InventoryReservationOptions>(configuration.GetSection(InventoryReservationOptions.SectionName));
        services.AddTransient<InventoryReservationCleanupJob>();
        services.Configure<ZarinpalOptions>(configuration.GetSection(ZarinpalOptions.SectionName));
        services.AddHttpClient<IZarinpalPaymentGateway, ZarinpalPaymentGateway>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<ZarinpalOptions>>()
                .Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
        });
        services.AddScoped<ISecurityAdminGateway, EfSecurityAdminGateway>();
        services.AddScoped<IPermissionGateway, EfPermissionGateway>();
        services.AddScoped<ICartPersistenceGateway, EfCartPersistenceGateway>();
        services.AddScoped<ILocationGateway, EfLocationGateway>();

        return services;
    }
}
