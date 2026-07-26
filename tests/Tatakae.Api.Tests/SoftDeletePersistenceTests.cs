using Tatakae.Application.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tatakae.Application.Interfaces;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Infrastructure.Persistence.Repositories;
using Tatakae.Domain.Enums;

namespace Tatakae.Api.Tests;

public sealed class SoftDeletePersistenceTests
{
    [Fact]
    public void Model_AppliesGlobalQueryFilterToEverySoftDeletableEntity()
    {
        using var db = CreateDb();

        var softDeletableEntities = db.Model.GetEntityTypes()
            .Where(x => typeof(IBaseEntity).IsAssignableFrom(x.ClrType)
                        && !x.IsOwned()
                        && x.BaseType is null)
            .ToArray();

        Assert.NotEmpty(softDeletableEntities);
        Assert.All(softDeletableEntities, entityType =>
            Assert.NotNull(entityType.GetQueryFilter()));
    }

    [Fact]
    public void Model_UsesClientNoActionForRequiredSoftDeleteRelationships()
    {
        using var db = CreateDb();

        var policyType = db.Model.FindEntityType(typeof(ProductEmbroideryPolicyDbRecord));
        Assert.NotNull(policyType);

        var productForeignKey = policyType!.GetForeignKeys()
            .Single(x => x.PrincipalEntityType.ClrType == typeof(ProductDbRecord));

        Assert.Equal(DeleteBehavior.ClientNoAction, productForeignKey.DeleteBehavior);
    }

    [Fact]
    public void Model_UsesFilteredUniqueIndexesForSoftDeletableBusinessKeys()
    {
        using var db = CreateDb();

        var couponType = db.Model.FindEntityType(typeof(CouponDbRecord));
        Assert.NotNull(couponType);

        var codeIndex = couponType!.GetIndexes()
            .Single(x => x.IsUnique && x.Properties.Any(p => p.Name == nameof(CouponDbRecord.Code)));

        Assert.Equal("[IsRemoved] = 0", codeIndex.GetFilter());
    }


    [Fact]
    public async Task SaveChanges_ConvertsAccidentalEntityRemovalToSoftDelete()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Categories.Add(new CategoryDbRecord
        {
            Id = id,
            Name = "دسته آزمایشی",
            Slug = $"category-{id:N}",
            SeoMetaTitle = "دسته آزمایشی",
            SeoMetaDescription = "توضیح دسته آزمایشی"
        });
        await db.SaveChangesAsync();

        var category = await db.Categories.SingleAsync(x => x.Id == id);
        db.Categories.Remove(category);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        Assert.False(await db.Categories.AnyAsync(x => x.Id == id));
        var removed = await db.Categories.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
        Assert.True(removed.IsRemoved);
        Assert.NotNull(removed.RemoveTime);
    }

    [Fact]
    public async Task CouponDelete_MarksRowRemovedAndDefaultQueriesHideIt()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Coupons.Add(new CouponDbRecord
        {
            Id = id,
            Code = "SOFT-DELETE",
            Type = DiscountType.FixedAmount,
            Value = 100_000,
            StartsAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsActive = true
        });
        await db.SaveChangesAsync();

        ICouponRepository repository = new SqlCouponRepository(db);
        var result = await repository.DeleteAsync(id);

        Assert.True(result.IsSuccess, result.Message);
        db.ChangeTracker.Clear();
        Assert.Null(await db.Coupons.SingleOrDefaultAsync(x => x.Id == id));

        var removed = await db.Coupons.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
        Assert.True(removed.IsRemoved);
        Assert.NotNull(removed.RemoveTime);
        Assert.NotNull(removed.UpdateTime);
    }


    [Fact]
    public async Task CouponDelete_MissingRow_ReturnsNotFoundResultWithPersianMessage()
    {
        await using var db = CreateDb();
        ICouponRepository repository = new SqlCouponRepository(db);

        var result = await repository.DeleteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("کد تخفیف پیدا نشد.", result.Message);
        Assert.Equal("not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ProductDelete_SoftDeletesProductAndDependentRows()
    {
        await using var db = CreateDb();
        var productId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Products.Add(new ProductDbRecord
        {
            Id = productId,
            Name = "محصول حذف نرم",
            Slug = $"soft-delete-{productId:N}",
            SeoMetaTitle = "محصول حذف نرم",
            SeoMetaDescription = "توضیح محصول حذف نرم",
            Images =
            [
                new ProductImageDbRecord
                {
                    Id = imageId,
                    ProductId = productId,
                    Url = "/images/test.webp",
                    AltText = "تصویر تست"
                }
            ],
            Variants =
            [
                new ProductVariantDbRecord
                {
                    Id = variantId,
                    ProductId = productId,
                    Sku = $"SKU-{variantId:N}",
                    Size = "M",
                    ColorName = "مشکی",
                    ColorHex = "#000000",
                    RegularPrice = 1_000_000,
                    StockQuantity = 2,
                    IsActive = true
                }
            ]
        });
        await db.SaveChangesAsync();

        IProductRepository repository = new SqlProductRepository(db);
        var deleteResult = await repository.DeleteAsync(productId);
        Assert.True(deleteResult.IsSuccess, deleteResult.Message);

        db.ChangeTracker.Clear();
        Assert.False(await db.Products.AnyAsync(x => x.Id == productId));
        Assert.False(await db.ProductImages.AnyAsync(x => x.Id == imageId));
        Assert.False(await db.ProductVariants.AnyAsync(x => x.Id == variantId));

        var removedProduct = await db.Products.IgnoreQueryFilters().SingleAsync(x => x.Id == productId);
        var removedImage = await db.ProductImages.IgnoreQueryFilters().SingleAsync(x => x.Id == imageId);
        var removedVariant = await db.ProductVariants.IgnoreQueryFilters().SingleAsync(x => x.Id == variantId);

        Assert.True(removedProduct.IsRemoved);
        Assert.True(removedImage.IsRemoved);
        Assert.True(removedVariant.IsRemoved);
        Assert.Equal(removedProduct.RemoveTime, removedImage.RemoveTime);
        Assert.Equal(removedProduct.RemoveTime, removedVariant.RemoveTime);
    }

    [Fact]
    public async Task WishlistAdd_RestoresPreviouslyRemovedRow()
    {
        await using var db = CreateDb();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var wishlistId = Guid.NewGuid();

        db.Wishlists.Add(new WishlistDbRecord
        {
            Id = wishlistId,
            CustomerId = customerId,
            ProductId = productId,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            IsRemoved = true,
            RemoveTime = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        IWishlistRepository repository = new SqlWishlistRepository(db);
        var addResult = await repository.AddAsync(customerId, productId);
        Assert.True(addResult.IsSuccess, addResult.Message);

        db.ChangeTracker.Clear();
        var active = await db.Wishlists.SingleAsync(x => x.CustomerId == customerId && x.ProductId == productId);
        var allRows = await db.Wishlists.IgnoreQueryFilters()
            .Where(x => x.CustomerId == customerId && x.ProductId == productId)
            .ToArrayAsync();

        Assert.Equal(wishlistId, active.Id);
        Assert.False(active.IsRemoved);
        Assert.Null(active.RemoveTime);
        Assert.Single(allRows);
    }

    private static TatakaeDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TatakaeDbContext>()
            .UseInMemoryDatabase($"tatakae-soft-delete-{Guid.NewGuid():N}")
            .Options;

        return new TatakaeDbContext(options);
    }
}
