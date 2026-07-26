using Tatakae.Application.Seo;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Services;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Seo;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class SeoServiceTests
{
    private static readonly Guid CategoryId = Guid.Parse("a1000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetSitemapAsync_IncludesStaticCategoryAndPublishedProductUrls()
    {
        var category = Category("تی‌شرت", "embroidered-tshirts");
        var product = Product("premium-cotton-embroidered-tshirt", allowIndex: true, isPublished: true);
        var hiddenProduct = Product("hidden-noindex-product", allowIndex: false, isPublished: true);
        var service = CreateService([product, hiddenProduct], [category]);

        var sitemap = await service.GetSitemapAsync("https://tatakae.test/");

        Assert.Contains(sitemap.Urls, x => x.Location == "https://tatakae.test/");
        Assert.Contains(sitemap.Urls, x => x.Location == "https://tatakae.test/shop");
        Assert.Contains(sitemap.Urls, x => x.Location == "https://tatakae.test/about");
        Assert.Contains(sitemap.Urls, x => x.Location == "https://tatakae.test/category/embroidered-tshirts");
        Assert.Contains(sitemap.Urls, x => x.Location == "https://tatakae.test/product/premium-cotton-embroidered-tshirt"
            && x.ImageUrl == "https://tatakae.test/ink/assets/fallback-red.svg");
        Assert.DoesNotContain(sitemap.Urls, x => x.Location.Contains("hidden-noindex-product", StringComparison.OrdinalIgnoreCase));
    }


    [Fact]
    public async Task GetSitemapAsync_UsesPersistedProductUpdatedAt()
    {
        var updatedAt = new DateTimeOffset(2026, 7, 1, 8, 30, 0, TimeSpan.Zero);
        var product = Product("dated-product", allowIndex: true, isPublished: true, updatedAt: updatedAt);
        var service = CreateService([product], [Category("تی‌شرت", "embroidered-tshirts")]);

        var sitemap = await service.GetSitemapAsync("https://tatakae.test");

        var item = Assert.Single(sitemap.Urls.Where(x => x.Location.EndsWith("/product/dated-product", StringComparison.Ordinal)));
        Assert.Equal(updatedAt, item.LastModified);
    }

    [Fact]
    public async Task AuditAsync_FindsSeoWarningsForWeakProductMetadata()
    {
        var weak = Product("weak-product", allowIndex: true, isPublished: true, title: "short", description: "too short", imageUrl: string.Empty, canonicalPath: null);
        var service = CreateService([weak], [Category("هودی", "embroidered-hoodies")]);

        var audit = await service.AuditAsync("https://tatakae.test");

        var page = Assert.Single(audit.Pages.Where(x => x.Url.EndsWith("/product/weak-product", StringComparison.OrdinalIgnoreCase)));
        Assert.True(page.Score < 100);
        Assert.Contains(page.Items, x => x.Code == "title_short");
        Assert.Contains(page.Items, x => x.Code == "description_short");
        Assert.Contains(page.Items, x => x.Code == "canonical_missing");
        Assert.Contains(page.Items, x => x.Code == "og_image_missing");
    }

    [Fact]
    public void RoutePolicies_MarkPrivateFlowsAsNoIndex()
    {
        var service = CreateService(Array.Empty<Product>(), Array.Empty<Category>());

        var policies = service.GetRoutePolicies();

        Assert.Contains(policies, x => x.Path == "/admin/*" && x.Robots == "noindex,nofollow" && !x.IsPublic);
        Assert.Contains(policies, x => x.Path == "/account/*" && x.Robots == "noindex,nofollow" && !x.IsPublic);
        Assert.Contains(policies, x => x.Path == "/product/*" && x.Robots == "index,follow" && x.IsPublic);
        Assert.Contains(policies, x => x.Path == "/customize/*" && x.Robots == "noindex,nofollow" && !x.IsPublic);
        Assert.Contains(policies, x => x.Path == "/payment/*" && x.Robots == "noindex,nofollow" && !x.IsPublic);
    }


    [Fact]
    public async Task GetSitemapAsync_FiltersInactiveAndNoIndexCategoriesAndUnpublishedProducts()
    {
        var visibleCategory = Category("تی‌شرت", "visible-category");
        var inactiveCategory = new Category(
            Guid.NewGuid(),
            "غیرفعال",
            "inactive-category",
            "دسته غیرفعال",
            "/ink/assets/fallback-red.svg",
            new SeoMetadata("دسته غیرفعال Tatakae", "توضیح معتبر برای دسته غیرفعال فروشگاه Tatakae.", "/category/inactive-category", "/ink/assets/fallback-red.svg"),
            sortOrder: 2,
            isActive: false);
        var noIndexCategory = new Category(
            Guid.NewGuid(),
            "بدون ایندکس",
            "noindex-category",
            "دسته بدون ایندکس",
            "/ink/assets/fallback-red.svg",
            new SeoMetadata("دسته بدون ایندکس Tatakae", "توضیح معتبر برای دسته بدون ایندکس فروشگاه Tatakae.", "/category/noindex-category", "/ink/assets/fallback-red.svg", false, true),
            sortOrder: 3,
            isActive: true);
        var visibleProduct = Product("visible-product", allowIndex: true, isPublished: true);
        var unpublishedProduct = Product("unpublished-product", allowIndex: true, isPublished: false);
        var service = CreateService([visibleProduct, unpublishedProduct], [visibleCategory, inactiveCategory, noIndexCategory]);

        var sitemap = await service.GetSitemapAsync("https://tatakae.test");

        Assert.Contains(sitemap.Urls, x => x.Location.EndsWith("/category/visible-category", StringComparison.Ordinal));
        Assert.DoesNotContain(sitemap.Urls, x => x.Location.Contains("inactive-category", StringComparison.Ordinal));
        Assert.DoesNotContain(sitemap.Urls, x => x.Location.Contains("noindex-category", StringComparison.Ordinal));
        Assert.Contains(sitemap.Urls, x => x.Location.EndsWith("/product/visible-product", StringComparison.Ordinal));
        Assert.DoesNotContain(sitemap.Urls, x => x.Location.Contains("unpublished-product", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetSitemapAsync_SetsPriorityAndChangeFrequencyFromProductState()
    {
        var featured = Product("featured-product", allowIndex: true, isPublished: true, isFeatured: true, stockQuantity: 5);
        var outOfStock = Product("out-of-stock-product", allowIndex: true, isPublished: true, stockQuantity: 0);
        var service = CreateService([featured, outOfStock], []);

        var sitemap = await service.GetSitemapAsync("https://tatakae.test/");

        var featuredUrl = Assert.Single(sitemap.Urls.Where(x => x.Location.EndsWith("/product/featured-product", StringComparison.Ordinal)));
        Assert.Equal("weekly", featuredUrl.ChangeFrequency);
        Assert.Equal(0.90m, featuredUrl.Priority);

        var outOfStockUrl = Assert.Single(sitemap.Urls.Where(x => x.Location.EndsWith("/product/out-of-stock-product", StringComparison.Ordinal)));
        Assert.Equal("monthly", outOfStockUrl.ChangeFrequency);
        Assert.Equal(0.75m, outOfStockUrl.Priority);
    }

    [Fact]
    public async Task GetSitemapAsync_DeduplicatesPagesThatShareCanonicalLocation()
    {
        var first = Product("first-product", allowIndex: true, isPublished: true, canonicalPath: "/product/shared-canonical");
        var second = Product("second-product", allowIndex: true, isPublished: true, canonicalPath: "https://tatakae.test/product/shared-canonical?utm_source=test");
        var service = CreateService([first, second], []);

        var sitemap = await service.GetSitemapAsync("https://tatakae.test");

        Assert.Single(sitemap.Urls.Where(x => x.Location == "https://tatakae.test/product/shared-canonical"));
    }

    [Fact]
    public async Task AuditAsync_FindsLongMetadataCanonicalMismatchInactiveVariantAndNoIndex()
    {
        var product = Product(
            "audit-product",
            allowIndex: false,
            isPublished: true,
            title: new string('ع', 70),
            description: new string('ت', 170),
            canonicalPath: "/product/wrong-path",
            variantActive: false);
        var service = CreateService([product], []);

        var audit = await service.AuditAsync("https://tatakae.test");

        var page = Assert.Single(audit.Pages.Where(x => x.Url.EndsWith("/product/wrong-path", StringComparison.Ordinal)));
        Assert.Contains(page.Items, x => x.Code == "title_long");
        Assert.Contains(page.Items, x => x.Code == "description_long");
        Assert.Contains(page.Items, x => x.Code == "canonical_mismatch");
        Assert.Contains(page.Items, x => x.Code == "variant_missing");
        Assert.Contains(page.Items, x => x.Code == "product_noindex");
        Assert.Equal(40, page.Score);
    }

    [Fact]
    public async Task BuildLlmsDocumentAsync_IncludesPublicCatalogAndExcludesNoIndexProducts()
    {
        var visible = Product("public-ai-product", allowIndex: true, isPublished: true);
        var hidden = Product("private-ai-product", allowIndex: false, isPublished: true);
        var service = CreateService([visible, hidden], [Category("تی‌شرت", "embroidered-tshirts")]);

        var document = await service.BuildLlmsDocumentAsync(
            "https://tatakae.test",
            AiProfile(),
            includeFullCatalog: false);

        Assert.Contains("# Tatakae", document.Content);
        Assert.Contains("https://tatakae.test/ai/catalog.json", document.Content);
        Assert.Contains("public-ai-product", document.Content);
        Assert.DoesNotContain("private-ai-product", document.Content);
        Assert.Contains("درباره ما", document.Content);
    }

    [Fact]
    public async Task BuildAiCatalogAsync_UsesCanonicalUrlsAndPublishesIrrPrices()
    {
        var product = Product("catalog-product", allowIndex: true, isPublished: true, stockQuantity: 4);
        var service = CreateService([product], [Category("تی‌شرت", "embroidered-tshirts")]);

        var catalog = await service.BuildAiCatalogAsync("https://tatakae.test/", AiProfile());

        var item = Assert.Single(catalog.Products);
        Assert.Equal("https://tatakae.test/product/catalog-product", item.Url);
        Assert.Equal("IRR", item.Currency);
        Assert.Equal(9_000_000m, item.StartingPrice);
        Assert.True(item.IsInStock);
        Assert.Single(item.Variants);
        Assert.Equal(9_000_000m, item.Variants.Single().Price);
        Assert.False(catalog.BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildLlmsDocumentAsync_FullCatalogIncludesVariantFactsAndUsageNotes()
    {
        var service = CreateService(
            [Product("full-ai-product", allowIndex: true, isPublished: true)],
            [Category("تی‌شرت", "embroidered-tshirts")]);

        var document = await service.BuildLlmsDocumentAsync(
            "https://tatakae.test",
            AiProfile(),
            includeFullCatalog: true);

        Assert.Contains("## Public product catalog", document.Content);
        Assert.Contains("SKU TT-FULL-AI-PRODUCT", document.Content);
        Assert.Contains("Treat price, availability and policy details as time-sensitive", document.Content);
    }

    [Theory]
    [InlineData("about", "/about")]
    [InlineData("terms", "/rules")]
    [InlineData("rules", "/rules")]
    [InlineData("shipping", "/shipping-policy")]
    [InlineData("shipping-policy", "/shipping-policy")]
    [InlineData("تماس ویژه", "/pages/تماس-ویژه")]
    public void LegalPagePath_MapsKnownAliasesAndCustomSlugs(string slug, string expected)
        => Assert.Equal(expected, SeoSlug.LegalPagePath(slug));

    [Theory]
    [InlineData(null, "https://localhost:7076")]
    [InlineData("   ", "https://localhost:7076")]
    [InlineData(" https://shop.example.com/// ", "https://shop.example.com")]
    public void NormalizeBaseUrl_ReturnsStableOrigin(string? value, string expected)
        => Assert.Equal(expected, SeoService.NormalizeBaseUrl(value));

    private static AiSeoSiteProfileDto AiProfile()
        => new(
            "Tatakae",
            "Tatakae",
            "فروشگاه ایرانی پوشاک گلدوزی آماده و قابل شخصی‌سازی.",
            "fa-IR",
            "IRR",
            "Iran",
            null,
            null,
            100);

    private static SeoService CreateService(IReadOnlyCollection<Product> products, IReadOnlyCollection<Category> categories)
        => new(new FakeProductRepository(products), new FakeCategoryRepository(categories), new FakeStorePolicyPageReader());

    private static Category Category(string name, string slug)
        => new(CategoryId, name, slug, $"خرید {name} گلدوزی‌شده Tatakae", "/ink/assets/fallback-red.svg", new SeoMetadata($"{name} گلدوزی Tatakae", $"خرید و سفارش آنلاین {name} گلدوزی‌شده در فروشگاه Tatakae.", $"/category/{slug}", "/ink/assets/fallback-red.svg"), null, 1, true);

    private static Product Product(string slug, bool allowIndex, bool isPublished, string? title = null, string? description = null, string? imageUrl = "/ink/assets/fallback-red.svg", string? canonicalPath = "__default", DateTimeOffset? updatedAt = null, bool isFeatured = false, int stockQuantity = 5, bool variantActive = true)
    {
        var seoCanonical = canonicalPath == "__default" ? $"/product/{slug}" : canonicalPath;
        var images = new[] { new ProductImage(Guid.NewGuid(), imageUrl ?? string.Empty, slug, true, 0) };
        return Tatakae.Domain.Entities.Product.Rehydrate(
            Guid.NewGuid(),
            slug.Replace('-', ' '),
            slug,
            ApparelCategory.TShirt,
            CategoryId,
            description ?? "توضیح کوتاه محصول گلدوزی‌شده برای فروشگاه Tatakae.",
            "توضیح کامل محصول تستی برای بررسی سئو و انتشار.",
            "پنبه",
            "Regular",
            "شستشو با آب سرد",
            string.Empty,
            new SeoMetadata(title ?? $"{slug} | Tatakae", description ?? "خرید آنلاین محصول گلدوزی‌شده از فروشگاه Tatakae با امکان انتخاب سایز، رنگ و گلدوزی.", seoCanonical, imageUrl, allowIndex, true),
            new EmbroideryPolicy(0, 0, 0, 8, 20, 20, [EmbroideryPlacement.LeftChest], ["#111111"]),
            images,
            [new ProductVariant(Guid.NewGuid(), $"TT-{slug.ToUpperInvariant()}", "M", "مشکی", "#111111", 900_000m, null, stockQuantity, variantActive)],
            Array.Empty<ProductSpecification>(),
            ["seo", "test"],
            isPublished,
            isFeatured,
            true,
            (updatedAt ?? DateTimeOffset.UtcNow).AddDays(-10),
            updatedAt ?? DateTimeOffset.UtcNow);
    }

    private sealed class FakeProductRepository(IReadOnlyCollection<Product> items) : IProductRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Product>>().Success("محصولات دریافت شدند.", items));

        public Task<ResultDto<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = items.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(item is null
                ? new ResultDto<Product>().NotFound("محصول پیدا نشد.")
                : new ResultDto<Product>().Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = items.SingleOrDefault(x => x.Slug == slug);
            return Task.FromResult(item is null
                ? new ResultDto<Product>().NotFound("محصول پیدا نشد.")
                : new ResultDto<Product>().Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> UpsertAsync(Product product, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Product>().Success("محصول ذخیره شد.", product));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("محصول حذف شد."));
    }

    private sealed class FakeCategoryRepository(IReadOnlyCollection<Category> items) : ICategoryRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Category>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Category>>().Success("دسته‌بندی‌ها دریافت شدند.", items));

        public Task<ResultDto<Category>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = items.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(item is null
                ? new ResultDto<Category>().NotFound("دسته‌بندی پیدا نشد.")
                : new ResultDto<Category>().Success("دسته‌بندی دریافت شد.", item));
        }

        public Task<ResultDto<Category>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = items.SingleOrDefault(x => x.Slug == slug);
            return Task.FromResult(item is null
                ? new ResultDto<Category>().NotFound("دسته‌بندی پیدا نشد.")
                : new ResultDto<Category>().Success("دسته‌بندی دریافت شد.", item));
        }

        public Task<ResultDto<Category>> UpsertAsync(Category category, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Category>().Success("دسته‌بندی ذخیره شد.", category));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("دسته‌بندی حذف شد."));
    }
    private sealed class FakeStorePolicyPageReader : IStorePolicyPageReader
    {
        public Task<IReadOnlyCollection<StorePolicyPageDto>> GetPublishedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<StorePolicyPageDto>>
            ([
                new(Guid.NewGuid(), "about", "درباره ما", "معرفی فروشگاه تخصصی گلدوزی Tatakae برای مشتریان و سفارش‌های اختصاصی.", "<p>درباره ما</p>", "درباره Tatakae | فروشگاه گلدوزی", "معرفی فروشگاه تخصصی پوشاک گلدوزی و سفارش اختصاصی Tatakae.", true, 1, DateTimeOffset.UtcNow)
            ]);
    }

}
