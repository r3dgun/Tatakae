using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence;

public sealed class TatakaeDbContext(DbContextOptions<TatakaeDbContext> options)
    : IdentityDbContext<ApplicationUserIdentity, ApplicationRoleIdentity, Guid>(options)
{
    public DbSet<CategoryDbRecord> Categories => Set<CategoryDbRecord>();
    public DbSet<ProductDbRecord> Products => Set<ProductDbRecord>();
    public DbSet<ProductImageDbRecord> ProductImages => Set<ProductImageDbRecord>();
    public DbSet<ProductVariantDbRecord> ProductVariants => Set<ProductVariantDbRecord>();
    public DbSet<ProductSpecificationDbRecord> ProductSpecifications => Set<ProductSpecificationDbRecord>();
    public DbSet<ProductTagDbRecord> ProductTags => Set<ProductTagDbRecord>();
    public DbSet<ProductEmbroideryPolicyDbRecord> ProductEmbroideryPolicies => Set<ProductEmbroideryPolicyDbRecord>();
    public DbSet<ProductAllowedPlacementDbRecord> ProductAllowedPlacements => Set<ProductAllowedPlacementDbRecord>();
    public DbSet<ProductAllowedThreadColorDbRecord> ProductAllowedThreadColors => Set<ProductAllowedThreadColorDbRecord>();
    public DbSet<CustomerDbRecord> Customers => Set<CustomerDbRecord>();
    public DbSet<CustomerAddressDbRecord> CustomerAddresses => Set<CustomerAddressDbRecord>();
    public DbSet<OrderDbRecord> Orders => Set<OrderDbRecord>();
    public DbSet<OrderLineDbRecord> OrderLines => Set<OrderLineDbRecord>();
    public DbSet<OrderStatusHistoryDbRecord> OrderStatusHistory => Set<OrderStatusHistoryDbRecord>();
    public DbSet<CouponDbRecord> Coupons => Set<CouponDbRecord>();
    public DbSet<IranianProvinceDbRecord> IranianProvinces => Set<IranianProvinceDbRecord>();
    public DbSet<IranianCityDbRecord> IranianCities => Set<IranianCityDbRecord>();
    public DbSet<BrandDbRecord> Brands => Set<BrandDbRecord>();
    public DbSet<SellerDbRecord> Sellers => Set<SellerDbRecord>();
    public DbSet<WarrantyDbRecord> Warranties => Set<WarrantyDbRecord>();
    public DbSet<ProductOfferDbRecord> ProductOffers => Set<ProductOfferDbRecord>();
    public DbSet<CustomerBankCardDbRecord> CustomerBankCards => Set<CustomerBankCardDbRecord>();
    public DbSet<ApplicationUserDbRecord> ApplicationUsers => Set<ApplicationUserDbRecord>();
    public DbSet<ApplicationUserRoleDbRecord> ApplicationUserRoles => Set<ApplicationUserRoleDbRecord>();
    public DbSet<OtpCodeDbRecord> OtpCodes => Set<OtpCodeDbRecord>();
    public DbSet<WalletDbRecord> Wallets => Set<WalletDbRecord>();
    public DbSet<WalletTransactionDbRecord> WalletTransactions => Set<WalletTransactionDbRecord>();
    public DbSet<PaymentDbRecord> Payments => Set<PaymentDbRecord>();
    public DbSet<PaymentTransactionDbRecord> PaymentTransactions => Set<PaymentTransactionDbRecord>();
    public DbSet<RefundDbRecord> Refunds => Set<RefundDbRecord>();
    public DbSet<ShippingMethodDbRecord> ShippingMethods => Set<ShippingMethodDbRecord>();
    public DbSet<ShippingZoneDbRecord> ShippingZones => Set<ShippingZoneDbRecord>();
    public DbSet<ShipmentDbRecord> Shipments => Set<ShipmentDbRecord>();
    public DbSet<ShipmentEventDbRecord> ShipmentEvents => Set<ShipmentEventDbRecord>();
    public DbSet<InvoiceDbRecord> Invoices => Set<InvoiceDbRecord>();
    public DbSet<InvoiceLineDbRecord> InvoiceLines => Set<InvoiceLineDbRecord>();
    public DbSet<ReturnRequestDbRecord> ReturnRequests => Set<ReturnRequestDbRecord>();
    public DbSet<ReturnRequestLineDbRecord> ReturnRequestLines => Set<ReturnRequestLineDbRecord>();
    public DbSet<WarehouseDbRecord> Warehouses => Set<WarehouseDbRecord>();
    public DbSet<StockItemDbRecord> StockItems => Set<StockItemDbRecord>();
    public DbSet<InventoryTransactionDbRecord> InventoryTransactions => Set<InventoryTransactionDbRecord>();
    public DbSet<InventoryReservationDbRecord> InventoryReservations => Set<InventoryReservationDbRecord>();
    public DbSet<MediaAssetDbRecord> MediaAssets => Set<MediaAssetDbRecord>();
    public DbSet<EmbroideryArtworkDbRecord> EmbroideryArtworks => Set<EmbroideryArtworkDbRecord>();
    public DbSet<ProductReviewDbRecord> ProductReviews => Set<ProductReviewDbRecord>();
    public DbSet<ProductQuestionDbRecord> ProductQuestions => Set<ProductQuestionDbRecord>();
    public DbSet<CartDbRecord> Carts => Set<CartDbRecord>();
    public DbSet<CartItemDbRecord> CartItems => Set<CartItemDbRecord>();
    public DbSet<WishlistDbRecord> Wishlists => Set<WishlistDbRecord>();
    public DbSet<DiscountCampaignDbRecord> DiscountCampaigns => Set<DiscountCampaignDbRecord>();
    public DbSet<SeoRedirectDbRecord> SeoRedirects => Set<SeoRedirectDbRecord>();
    public DbSet<UrlSlugHistoryDbRecord> UrlSlugHistories => Set<UrlSlugHistoryDbRecord>();
    public DbSet<AuditLogDbRecord> AuditLogs => Set<AuditLogDbRecord>();
    public DbSet<StorePolicyPageDbRecord> StorePolicyPages => Set<StorePolicyPageDbRecord>();
    public DbSet<ContactMessageDbRecord> ContactMessages => Set<ContactMessageDbRecord>();
    public DbSet<AppPermissionDbRecord> Permissions => Set<AppPermissionDbRecord>();
    public DbSet<AppRolePermissionDbRecord> RolePermissions => Set<AppRolePermissionDbRecord>();
    public DbSet<AdminPageAccessDbRecord> AdminPageAccesses => Set<AdminPageAccessDbRecord>();
    public DbSet<LoginAuditDbRecord> LoginAudits => Set<LoginAuditDbRecord>();
    public DbSet<NotificationDbRecord> Notifications => Set<NotificationDbRecord>();
    public DbSet<Tatakae.Infrastructure.Persistence.Models.User> PermissionUsers => Set<Tatakae.Infrastructure.Persistence.Models.User>();
    public DbSet<Tatakae.Infrastructure.Persistence.Models.Role> PermissionRoles => Set<Tatakae.Infrastructure.Persistence.Models.Role>();
    public DbSet<Tatakae.Infrastructure.Persistence.Models.Permission> PermissionDefinitions => Set<Tatakae.Infrastructure.Persistence.Models.Permission>();
    public DbSet<UserRole> PermissionUserRoles => Set<UserRole>();
    public DbSet<PermissionsRole> PermissionsRoles => Set<PermissionsRole>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public void SoftDelete(IBaseEntity entity, DateTime? removedAt = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.MarkAsRemoved(removedAt);
        Entry(entity).State = EntityState.Modified;
    }

    public void SoftDeleteRange(IEnumerable<IBaseEntity> entities, DateTime? removedAt = null)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var timestamp = removedAt ?? DateTime.Now;

        foreach (var entity in entities)
        {
            entity.MarkAsRemoved(timestamp);
            Entry(entity).State = EntityState.Modified;
        }
    }

    public void Restore(IBaseEntity entity, DateTime? restoredAt = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.Restore(restoredAt);
        Entry(entity).State = EntityState.Modified;
    }

    private void ApplyAuditInformation()
    {
        var now = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries().Where(x => x.Entity is IBaseEntity))
        {
            var entity = (IBaseEntity)entry.Entity;

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entity.MarkAsRemoved(now);
                continue;
            }

            if (entry.State == EntityState.Added && entity.InsertTime == default)
            {
                entity.InsertTime = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entity.UpdateTime = now;
            }

            if (entity.IsRemoved && entity.RemoveTime is null)
            {
                entity.RemoveTime = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUserIdentity>().ToTable("IdentityUsers");
        modelBuilder.Entity<LoginAuditDbRecord>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<NotificationDbRecord>()
            .Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<NotificationDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<NotificationDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<NotificationDbRecord>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<NotificationDbRecord>()
            .HasOne(x => x.RelatedOrder)
            .WithMany()
            .HasForeignKey(x => x.RelatedOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<NotificationDbRecord>()
            .HasOne(x => x.RelatedProduct)
            .WithMany()
            .HasForeignKey(x => x.RelatedProductId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationRoleIdentity>().ToTable("IdentityRoles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("IdentityUserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("IdentityUserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("IdentityUserLogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("IdentityRoleClaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("IdentityUserTokens");

        ConfigurePermissionCheckerEntities(modelBuilder);

        // The schema is intentionally defined by the Code First models in Persistence/Models.
        // Fluent API is kept only for rules that DataAnnotations do not express cleanly:
        // enum-as-string persistence and a few delete behaviors.
        modelBuilder.Entity<ProductDbRecord>()
            .Property(x => x.ApparelCategory)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<ProductAllowedPlacementDbRecord>()
            .Property(x => x.Placement)
            .HasConversion<string>()
            .HasMaxLength(80);

        modelBuilder.Entity<OrderDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<OrderDbRecord>()
            .Property(x => x.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<OrderLineDbRecord>()
            .Property(x => x.EmbroideryPlacement)
            .HasConversion<string>()
            .HasMaxLength(80);

        modelBuilder.Entity<OrderStatusHistoryDbRecord>()
            .Property(x => x.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<OrderStatusHistoryDbRecord>()
            .Property(x => x.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<CouponDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<CategoryDbRecord>()
            .HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductDbRecord>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductDbRecord>()
            .HasOne(x => x.Brand)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductDbRecord>()
            .HasOne(x => x.EmbroideryPolicy)
            .WithOne(x => x.Product)
            .HasForeignKey<ProductEmbroideryPolicyDbRecord>(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductDbRecord>()
            .HasMany(x => x.Images)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductDbRecord>()
            .HasMany(x => x.Variants)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductDbRecord>()
            .HasMany(x => x.Specifications)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductDbRecord>()
            .HasMany(x => x.Tags)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductEmbroideryPolicyDbRecord>()
            .HasMany(x => x.AllowedPlacements)
            .WithOne(x => x.Policy)
            .HasForeignKey(x => x.ProductEmbroideryPolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductEmbroideryPolicyDbRecord>()
            .HasMany(x => x.AllowedThreadColors)
            .WithOne(x => x.Policy)
            .HasForeignKey(x => x.ProductEmbroideryPolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerDbRecord>()
            .HasMany(x => x.Addresses)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDbRecord>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderDbRecord>()
            .HasMany(x => x.Lines)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDbRecord>()
            .HasMany(x => x.StatusHistory)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SellerDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<SellerDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<WarrantyDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ApplicationUserRoleDbRecord>()
            .Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<OtpCodeDbRecord>()
            .Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<WalletTransactionDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<PaymentDbRecord>()
            .Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<PaymentDbRecord>()
            .Property(x => x.Gateway)
            .HasConversion<string>()
            .HasMaxLength(60);

        modelBuilder.Entity<PaymentDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<PaymentTransactionDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<RefundDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ShippingMethodDbRecord>()
            .Property(x => x.Carrier)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ShipmentDbRecord>()
            .Property(x => x.Carrier)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ShipmentDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ShipmentEventDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<InvoiceDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<InvoiceDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ReturnRequestDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ReturnRequestDbRecord>()
            .Property(x => x.Reason)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<InventoryTransactionDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<InventoryReservationDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<MediaAssetDbRecord>()
            .Property(x => x.UsageType)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<EmbroideryArtworkDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<EmbroideryArtworkDbRecord>()
            .HasOne(x => x.MediaAsset)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmbroideryArtworkDbRecord>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EmbroideryArtworkDbRecord>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EmbroideryArtworkDbRecord>()
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EmbroideryArtworkDbRecord>()
            .HasOne(x => x.OrderLine)
            .WithMany()
            .HasForeignKey(x => x.OrderLineId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductReviewDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ProductQuestionDbRecord>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<SeoRedirectDbRecord>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<IranianProvinceDbRecord>()
            .HasMany(x => x.Cities)
            .WithOne(x => x.Province)
            .HasForeignKey(x => x.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SellerDbRecord>()
            .HasMany(x => x.Offers)
            .WithOne(x => x.Seller)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductOfferDbRecord>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductOfferDbRecord>()
            .HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductOfferDbRecord>()
            .HasOne(x => x.Warranty)
            .WithMany()
            .HasForeignKey(x => x.WarrantyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUserDbRecord>()
            .HasMany(x => x.Roles)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WalletDbRecord>()
            .HasMany(x => x.Transactions)
            .WithOne(x => x.Wallet)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentDbRecord>()
            .HasMany(x => x.Transactions)
            .WithOne(x => x.Payment)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShipmentDbRecord>()
            .HasMany(x => x.Events)
            .WithOne(x => x.Shipment)
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceDbRecord>()
            .HasMany(x => x.Lines)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReturnRequestDbRecord>()
            .HasMany(x => x.Lines)
            .WithOne(x => x.ReturnRequest)
            .HasForeignKey(x => x.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartDbRecord>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Cart)
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<AppRolePermissionDbRecord>()
            .HasOne(x => x.Role)
            .WithMany(x => x.Permissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppRolePermissionDbRecord>()
            .HasOne(x => x.Permission)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PermissionsRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.PermissionsRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PermissionsRole>()
            .HasOne(x => x.Permission)
            .WithMany(x => x.PermissionsRoles)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Every persistence model that implements IBaseEntity is hidden automatically after soft delete.
        // Administrative restore/audit code can opt in explicitly through IgnoreQueryFilters().
        ApplySoftDeleteQueryFilters(modelBuilder);

        // SQL Server does not allow multiple cascade paths.
        // This project uses soft delete through BaseEntity.IsRemoved, so physical cascade delete is intentionally disabled.
        DisableCascadeDeleteForSqlServer(modelBuilder);

    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType)
                || entityType.IsOwned()
                || entityType.BaseType is not null)
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isRemoved = Expression.Property(parameter, nameof(IBaseEntity.IsRemoved));
            var activeOnly = Expression.Equal(isRemoved, Expression.Constant(false));
            entityType.SetQueryFilter(Expression.Lambda(activeOnly, parameter));

            // A removed row must not reserve business keys such as slug, SKU, code or mobile.
            // SQL Server filtered unique indexes allow a new active row to reuse those values.
            foreach (var index in entityType.GetIndexes().Where(x => x.IsUnique).ToArray())
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex(index.Properties.Select(x => x.Name).ToArray())
                    .IsUnique()
                    .HasFilter("[IsRemoved] = 0");
            }
        }
    }

    private static void ConfigurePermissionCheckerEntities(ModelBuilder modelBuilder)
    {
        // These entities intentionally use legacy key names sent by the user
        // (UserId, RoleId, PermissionId, UR_Id, RP_Id).
        // BaseEntity.Id is ignored here so SQL Server does not create a second unused Id column.
        modelBuilder.Entity<Tatakae.Infrastructure.Persistence.Models.User>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Ignore(x => x.Id);
            entity.Property(x => x.UserId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Tatakae.Infrastructure.Persistence.Models.Role>(entity =>
        {
            entity.HasKey(x => x.RoleId);
            entity.Ignore(x => x.Id);
            entity.Property(x => x.RoleId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Tatakae.Infrastructure.Persistence.Models.Permission>(entity =>
        {
            entity.HasKey(x => x.PermissionId);
            entity.Ignore(x => x.Id);
            // Permission IDs are stable numeric constants used by [PermissionChecker(...)]
            // and must be inserted explicitly, e.g. 1100 = admin.products.view.
            entity.Property(x => x.PermissionId).ValueGeneratedNever();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => x.UR_Id);
            entity.Ignore(x => x.Id);
            entity.Property(x => x.UR_Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<PermissionsRole>(entity =>
        {
            entity.HasKey(x => x.RP_Id);
            entity.Ignore(x => x.Id);
            entity.Property(x => x.RP_Id).ValueGeneratedOnAdd();
        });
    }

    private static void DisableCascadeDeleteForSqlServer(ModelBuilder modelBuilder)
    {
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetForeignKeys()))
        {
            // ClientNoAction is important for the soft-delete model: marking a required
            // principal as Deleted must not sever required one-to-one/one-to-many
            // associations before SaveChanges can convert Deleted to Modified.
            // The database also receives NO ACTION, so physical deletes remain blocked
            // by foreign-key constraints.
            foreignKey.DeleteBehavior = DeleteBehavior.ClientNoAction;
        }
    }
}
