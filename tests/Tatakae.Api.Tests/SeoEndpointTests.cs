using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Tatakae.Api.Controllers;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Api.Tests;

public sealed class SeoEndpointTests
{
    private static readonly Guid CategoryId = Guid.Parse("a1000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Sitemap_Get_ReturnsWellFormedUtf8XmlUsingConfiguredPublicBaseUrl()
    {
        var productUpdatedAt = new DateTimeOffset(2026, 7, 1, 8, 30, 0, TimeSpan.Zero);
        var service = CreateSeoService(
            [Product("تی-شرت-گلدوزی", productUpdatedAt)],
            [Category("تی‌شرت", "تی-شرت")],
            [LegalPage("terms", new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero))]);
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["PublicBaseUrl"] = "https://shop.example.com/"
        });
        var controller = new SitemapController(service, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Get(CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/xml; charset=utf-8", file.ContentType);
        var xmlText = Encoding.UTF8.GetString(file.FileContents);
        Assert.True(xmlText.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", StringComparison.OrdinalIgnoreCase));

        var document = XDocument.Parse(xmlText);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace imageNs = "http://www.google.com/schemas/sitemap-image/1.1";
        Assert.Equal(ns + "urlset", document.Root?.Name);

        var urls = document.Root!
            .Elements(ns + "url")
            .Select(element => new
            {
                Location = element.Element(ns + "loc")?.Value,
                LastModified = element.Element(ns + "lastmod")?.Value,
                ChangeFrequency = element.Element(ns + "changefreq")?.Value,
                Priority = element.Element(ns + "priority")?.Value,
                ImageLocation = element.Element(imageNs + "image")?.Element(imageNs + "loc")?.Value
            })
            .ToArray();

        Assert.Contains(urls, item => item.Location == "https://shop.example.com/product/تی-شرت-گلدوزی"
            && item.LastModified == "2026-07-01"
            && item.ImageLocation == "https://shop.example.com/ink/assets/fallback-red.svg");
        Assert.Contains(urls, item => item.Location == "https://shop.example.com/category/تی-شرت" && item.ChangeFrequency == "weekly");
        Assert.Contains(urls, item => item.Location == "https://shop.example.com/rules" && item.Priority == "0.65");
        Assert.False(xmlText.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Robots_Get_ReturnsPrivateDisallowRulesAndAbsoluteSitemapUrl()
    {
        var service = CreateSeoService([], [], []);
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["PublicBaseUrl"] = "https://shop.example.com/"
        });
        var controller = new RobotsController(service, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = Assert.IsType<ContentResult>(result);
        var body = Assert.IsType<string>(content.Content);
        Assert.NotNull(content.ContentType);
        Assert.True(content.ContentType!.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("User-agent: *", body);
        Assert.Contains("Allow: /", body);
        Assert.Contains("Disallow: /admin", body);
        Assert.Contains("Disallow: /account", body);
        Assert.Contains("Disallow: /checkout", body);
        Assert.Contains("Disallow: /payment", body);
        Assert.Contains("Sitemap: https://shop.example.com/sitemap.xml", body);
        Assert.Contains("User-agent: OAI-SearchBot", body);
        Assert.Contains("User-agent: ChatGPT-User", body);
        Assert.Contains("User-agent: GPTBot", body);
        var normalizedRobots = body.Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Contains("User-agent: OAI-SearchBot\nAllow: /\n", normalizedRobots);
        Assert.Contains("User-agent: ChatGPT-User\nAllow: /\n", normalizedRobots);
        Assert.Contains("User-agent: GPTBot\nDisallow: /\n", normalizedRobots);
        Assert.Contains("# AI-readable site guide: https://shop.example.com/llms.txt", body);
        Assert.DoesNotContain("Disallow: /product", body);
        Assert.DoesNotContain("Disallow: /category", body);
    }

    [Fact]
    public void Robots_Get_WhenBaseUrlIsNotConfigured_UsesCurrentRequestOrigin()
    {
        var service = CreateSeoService([], [], []);
        var controller = new RobotsController(service, Configuration([]));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("store.test", 8443);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = controller.Get();

        var content = Assert.IsType<ContentResult>(result);
        var body = Assert.IsType<string>(content.Content);
        Assert.Contains("Sitemap: https://store.test:8443/sitemap.xml", body);
    }

    [Fact]
    public async Task AiSeo_Llms_ReturnsMarkdownWithCanonicalPublicLinks()
    {
        var service = CreateSeoService(
            [Product("ai-product", DateTimeOffset.UtcNow)],
            [Category("تی‌شرت", "embroidered-tshirts")],
            [LegalPage("terms", DateTimeOffset.UtcNow)]);
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["PublicBaseUrl"] = "https://shop.example.com",
            ["AiSeo:SiteName"] = "Tatakae",
            ["AiSeo:ExposeFullCatalog"] = "true"
        });
        var controller = new AiSeoController(service, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = await controller.Llms(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(response);
        Assert.StartsWith("text/markdown", content.ContentType ?? string.Empty);
        Assert.Contains("# Tatakae", content.Content);
        Assert.Contains("https://shop.example.com/product/ai-product", content.Content);
        Assert.Contains("https://shop.example.com/ai/catalog.json", content.Content);
        Assert.DoesNotContain("/admin", content.Content);
        Assert.DoesNotContain("/checkout", content.Content);
    }

    [Fact]
    public async Task AiSeo_Catalog_ReturnsPublicProductDataWithoutPrivateRoutes()
    {
        var service = CreateSeoService(
            [Product("catalog-product", DateTimeOffset.UtcNow)],
            [Category("تی‌شرت", "embroidered-tshirts")],
            []);
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["PublicBaseUrl"] = "https://shop.example.com",
            ["AiSeo:Currency"] = "IRR"
        });
        var controller = new AiSeoController(service, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = await controller.Catalog(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        var catalog = Assert.IsType<Tatakae.Application.Contracts.Seo.AiCatalogDocumentDto>(ok.Value);
        var product = Assert.Single(catalog.Products);
        Assert.Equal("https://shop.example.com/product/catalog-product", product.Url);
        Assert.Equal("IRR", product.Currency);
        Assert.DoesNotContain(catalog.Products, item => item.Url.Contains("/account", StringComparison.OrdinalIgnoreCase));
    }

    private static IConfiguration Configuration(IEnumerable<KeyValuePair<string, string?>> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static SeoService CreateSeoService(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<Category> categories,
        IReadOnlyCollection<StorePolicyPageDto> legalPages)
        => new(new FakeProductRepository(products), new FakeCategoryRepository(categories), new FakeStorePolicyPageReader(legalPages));

    private static Category Category(string name, string slug)
        => new(
            CategoryId,
            name,
            slug,
            $"خرید {name} گلدوزی‌شده Tatakae",
            "/ink/assets/fallback-red.svg",
            new SeoMetadata(
                $"{name} گلدوزی Tatakae",
                $"خرید و سفارش آنلاین {name} گلدوزی‌شده در فروشگاه Tatakae.",
                $"/category/{slug}",
                "/ink/assets/fallback-red.svg"),
            sortOrder: 1,
            isActive: true);

    private static Product Product(string slug, DateTimeOffset updatedAt)
        => Tatakae.Domain.Entities.Product.Rehydrate(
            Guid.NewGuid(),
            "محصول تست سئو",
            slug,
            ApparelCategory.TShirt,
            CategoryId,
            "توضیح کوتاه محصول گلدوزی‌شده برای تست sitemap.",
            "توضیح کامل محصول تستی برای بررسی خروجی XML نقشه سایت.",
            "پنبه",
            "Regular",
            "شستشو با آب سرد",
            string.Empty,
            new SeoMetadata(
                "تی‌شرت گلدوزی تست | Tatakae",
                "خرید آنلاین تی‌شرت گلدوزی‌شده با انتخاب سایز و رنگ از فروشگاه Tatakae.",
                $"/product/{slug}",
                "/ink/assets/fallback-red.svg"),
            new EmbroideryPolicy(0, 0, 0, 8, 20, 20, [EmbroideryPlacement.LeftChest], ["#111111"]),
            [new ProductImage(Guid.NewGuid(), "/ink/assets/fallback-red.svg", slug, true, 0)],
            [new ProductVariant(Guid.NewGuid(), "TT-SEO-001", "M", "مشکی", "#111111", 900_000m, null, 5)],
            [],
            ["seo", "test"],
            isPublished: true,
            isFeatured: false,
            supportsEmbroidery: true,
            createdAt: updatedAt.AddDays(-10),
            updatedAt: updatedAt);

    private static StorePolicyPageDto LegalPage(string slug, DateTimeOffset updatedAt)
        => new(
            Guid.NewGuid(),
            slug,
            "قوانین فروشگاه",
            "خلاصه قوانین فروشگاه Tatakae برای خرید، ارسال و ثبت سفارش.",
            "<p>متن کامل قوانین فروشگاه</p>",
            "قوانین فروشگاه | Tatakae",
            "قوانین خرید، ارسال و ثبت سفارش در فروشگاه Tatakae.",
            true,
            1,
            updatedAt);

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
    private sealed class FakeStorePolicyPageReader(IReadOnlyCollection<StorePolicyPageDto> items) : IStorePolicyPageReader
    {
        public Task<IReadOnlyCollection<StorePolicyPageDto>> GetPublishedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(items);
    }
}
