using Microsoft.EntityFrameworkCore;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Api.Tests;

public sealed class StoreDataSeederTests
{
    [Fact]
    public async Task RunningSeedTwice_DoesNotDuplicateDevelopmentFixtures()
    {
        await using var db = CreateDbContext();

        await StoreDataSeeder.EnsureCatalogAsync(db);
        await StoreDataSeeder.EnsureDevelopmentFixturesAsync(db);
        var firstCounts = await CountsAsync(db);

        db.ChangeTracker.Clear();
        await StoreDataSeeder.EnsureCatalogAsync(db);
        await StoreDataSeeder.EnsureDevelopmentFixturesAsync(db);
        var secondCounts = await CountsAsync(db);

        Assert.Equal(firstCounts, secondCounts);
        Assert.Equal(4, secondCounts.Categories);
        Assert.Equal(8, secondCounts.Products);
        Assert.Equal(2, secondCounts.Customers);
        Assert.Equal(1, secondCounts.Addresses);
        Assert.Equal(1, secondCounts.Orders);
        Assert.Equal(2, secondCounts.Questions);
    }

    [Fact]
    public async Task CatalogSeed_WhenDevelopmentFixturesAreDisabled_ExcludesOutOfStockTestProduct()
    {
        await using var db = CreateDbContext();

        await StoreDataSeeder.EnsureCatalogAsync(db, includeDevelopmentFixtures: false);

        Assert.False(await db.Products.AnyAsync(x => x.Id == DevelopmentSeedCatalog.OutOfStockProductId));
        Assert.Equal(7, await db.Products.CountAsync());
    }

    [Fact]
    public async Task SeededDatabase_ContainsRequiredInventoryScenarios()
    {
        await using var db = CreateDbContext();
        await StoreDataSeeder.EnsureCatalogAsync(db);

        var products = await db.Products.AsNoTracking().Include(x => x.Variants).ToListAsync();
        var ready = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.ReadyMadeProductId));
        var customizable = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.CustomizableProductId));
        var discounted = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.DiscountedProductId));
        var unavailable = Assert.Single(products.Where(x => x.Id == DevelopmentSeedCatalog.OutOfStockProductId));

        Assert.False(ready.SupportsEmbroidery);
        Assert.True(customizable.SupportsEmbroidery);
        Assert.Contains(discounted.Variants, x => x.SalePrice.HasValue && x.SalePrice < x.RegularPrice);
        Assert.All(unavailable.Variants, x => Assert.Equal(0, x.StockQuantity));
    }

    [Fact]
    public async Task DevelopmentFixtures_ContainTestOrderAddressAndQuestionStates()
    {
        await using var db = CreateDbContext();
        await StoreDataSeeder.EnsureCatalogAsync(db);
        await StoreDataSeeder.EnsureDevelopmentFixturesAsync(db);

        var address = await db.CustomerAddresses.AsNoTracking().SingleAsync(x => x.Id == DevelopmentSeedCatalog.CustomerAddressId);
        var order = await db.Orders.AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.StatusHistory)
            .SingleAsync(x => x.Id == DevelopmentSeedCatalog.TestOrderId);
        var questions = await db.ProductQuestions.AsNoTracking().OrderBy(x => x.Id).ToListAsync();

        Assert.True(address.IsDefault);
        Assert.Equal(DevelopmentSeedCatalog.CustomerId, address.CustomerId);
        Assert.Equal(DevelopmentSeedCatalog.TestOrderNumber, order.OrderNumber);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.InEmbroidery, order.Status);
        Assert.Single(order.Lines);
        Assert.Single(order.StatusHistory);
        Assert.Contains(questions, x => x.Id == DevelopmentSeedCatalog.AnsweredQuestionId && x.Status == QuestionStatus.Answered && x.AnswerText != null);
        Assert.Contains(questions, x => x.Id == DevelopmentSeedCatalog.PendingQuestionId && x.Status == QuestionStatus.Pending && x.AnswerText == null);
    }

    [Fact]
    public async Task CatalogSeed_RepairsProductWithMissingVariants()
    {
        await using var db = CreateDbContext();
        await StoreDataSeeder.EnsureCatalogAsync(db);

        var product = await db.Products.Include(x => x.Variants).SingleAsync(x => x.Id == DevelopmentSeedCatalog.OutOfStockProductId);
        var originalVariantIds = product.Variants.Select(x => x.Id).OrderBy(x => x).ToArray();

        db.ProductVariants.RemoveRange(product.Variants);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Empty(await db.ProductVariants.Where(x => x.ProductId == product.Id).ToListAsync());
        Assert.Equal(
            originalVariantIds.Length,
            await db.ProductVariants.IgnoreQueryFilters().CountAsync(x => x.ProductId == product.Id));

        await StoreDataSeeder.EnsureCatalogAsync(db);

        var repaired = await db.Products.AsNoTracking().Include(x => x.Variants).SingleAsync(x => x.Id == DevelopmentSeedCatalog.OutOfStockProductId);
        var allStoredVariants = await db.ProductVariants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ProductId == product.Id)
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(originalVariantIds, repaired.Variants.Select(x => x.Id).OrderBy(x => x).ToArray());
        Assert.Equal(originalVariantIds.Length, allStoredVariants.Count);
        Assert.All(repaired.Variants, x => Assert.Equal(0, x.StockQuantity));
        Assert.All(allStoredVariants, x => Assert.False(x.IsRemoved));
    }


    [Fact]
    public async Task CatalogSeed_RestoresSoftDeletedSeedProductWithoutDuplicateKey()
    {
        await using var db = CreateDbContext();
        await StoreDataSeeder.EnsureCatalogAsync(db);

        var product = await db.Products.SingleAsync(x => x.Id == DevelopmentSeedCatalog.ReadyMadeProductId);
        db.SoftDelete(product);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.False(await db.Products.AnyAsync(x => x.Id == DevelopmentSeedCatalog.ReadyMadeProductId));
        Assert.Single(await db.Products.IgnoreQueryFilters().Where(x => x.Id == DevelopmentSeedCatalog.ReadyMadeProductId).ToListAsync());

        await StoreDataSeeder.EnsureCatalogAsync(db);

        var restored = await db.Products.SingleAsync(x => x.Id == DevelopmentSeedCatalog.ReadyMadeProductId);
        var storedRows = await db.Products
            .IgnoreQueryFilters()
            .Where(x => x.Id == DevelopmentSeedCatalog.ReadyMadeProductId)
            .ToListAsync();

        Assert.False(restored.IsRemoved);
        Assert.Null(restored.RemoveTime);
        Assert.Single(storedRows);
    }

    [Fact]
    public async Task CatalogSeed_PreservesNonSeedBusinessProducts()
    {
        await using var db = CreateDbContext();
        var category = new CategoryDbRecord
        {
            Id = Guid.NewGuid(),
            Name = "دسته سفارشی",
            Slug = "custom-category",
            Description = "دسته ساخته‌شده خارج از Seed",
            CoverImageUrl = "https://example.com/custom.jpg",
            SeoMetaTitle = "دسته سفارشی",
            SeoMetaDescription = "دسته ساخته‌شده خارج از Seed",
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        await StoreDataSeeder.EnsureCatalogAsync(db);

        Assert.True(await db.Categories.AnyAsync(x => x.Id == category.Id));
        Assert.Equal(5, await db.Categories.CountAsync());
    }

    private static TatakaeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TatakaeDbContext>()
            .UseInMemoryDatabase($"tatakae-phase14-seed-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;
        return new TatakaeDbContext(options);
    }

    private static async Task<SeedCounts> CountsAsync(TatakaeDbContext db) => new(
        await db.Categories.CountAsync(),
        await db.Products.CountAsync(),
        await db.Customers.CountAsync(),
        await db.CustomerAddresses.CountAsync(),
        await db.Orders.CountAsync(),
        await db.ProductQuestions.CountAsync());

    private sealed record SeedCounts(int Categories, int Products, int Customers, int Addresses, int Orders, int Questions);
}
