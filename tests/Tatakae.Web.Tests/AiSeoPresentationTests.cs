using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Seo;

namespace Tatakae.Web.Tests;

public sealed class AiSeoPresentationTests
{
    [Fact]
    public void Product_structured_data_contains_variants_reviews_questions_and_breadcrumbs()
    {
        var product = Product();
        var rating = new ProductRatingSummaryDto(
            product.Id,
            4.8m,
            2,
            new Dictionary<int, int> { [5] = 2 },
            2,
            100);
        var reviews = new[]
        {
            new ProductReviewDto(
                Guid.NewGuid(), product.Id, product.Name, "مشتری تست", 5,
                "کیفیت عالی", "کیفیت پارچه و گلدوزی بسیار خوب بود.", true, true,
                "Approved", "تأییدشده", ["دوخت تمیز"], [], null, null, DateTimeOffset.UtcNow)
        };
        var questions = new[]
        {
            new ProductQuestionDto(
                Guid.NewGuid(), product.Id, "کاربر", "آیا امکان گلدوزی متن وجود دارد؟",
                "بله، این محصول از گلدوزی متن پشتیبانی می‌کند.", DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow, true)
        };

        var json = AiStructuredData.BuildProductGraph(
            "https://tatakae.test",
            product,
            rating,
            reviews,
            questions,
            "https://tatakae.test/product/test-product",
            ["/images/test-product.webp"]);

        Assert.Contains("\"ProductGroup\"", json);
        Assert.Contains("\"hasVariant\"", json);
        Assert.Contains("\"AggregateRating\"", json);
        Assert.Contains("\"Review\"", json);
        Assert.Contains("\"FAQPage\"", json);
        Assert.Contains("\"BreadcrumbList\"", json);
        Assert.Contains("9000000", json);
    }

    [Fact]
    public void Site_structured_data_identifies_organization_and_website()
    {
        var json = AiStructuredData.BuildSiteGraph(
            "https://tatakae.test/",
            "Tatakae",
            "فروشگاه پوشاک گلدوزی",
            supportEmail: "support@example.com");

        Assert.Contains("\"Organization\"", json);
        Assert.Contains("\"WebSite\"", json);
        Assert.Contains("support@example.com", json);
        Assert.Contains("https://tatakae.test/#organization", json);
    }

    [Fact]
    public void Web_head_exposes_ai_discovery_documents()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ArchitectureFixtures");
        var head = File.ReadAllText(Path.Combine(root, "Shared", "SeoHead.razor"));
        var index = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "index.html"));

        Assert.Contains("/llms.txt", head);
        Assert.Contains("/ai/catalog.json", head);
        Assert.Contains("/llms.txt", index);
        Assert.Contains("/ai/catalog.json", index);
    }

    private static ProductDetailDto Product()
    {
        var id = Guid.NewGuid();
        return new ProductDetailDto(
            id,
            "محصول تست",
            "test-product",
            "تی‌شرت",
            "embroidered-tshirts",
            "TShirt",
            "تی‌شرت پنبه‌ای قابل شخصی‌سازی با گلدوزی متن و طرح.",
            "محصول تست با توضیحات کامل و قابل مشاهده برای کاربر.",
            "پنبه",
            "Regular",
            "شستشو با آب سرد",
            string.Empty,
            true,
            true,
            true,
            [new ProductImageDto(Guid.NewGuid(), "/images/test-product.webp", "تی‌شرت تست", true, 0)],
            [
                new ProductVariantDto(Guid.NewGuid(), "TT-AI-M-BLK", "M", "مشکی", "#111111", 900_000m, null, 900_000m, 5, true, true),
                new ProductVariantDto(Guid.NewGuid(), "TT-AI-L-WHT", "L", "سفید", "#FFFFFF", 950_000m, null, 950_000m, 3, true, true)
            ],
            [new ProductSpecificationDto("وزن پارچه", "۲۴۰ گرم", 0)],
            ["گلدوزی", "تی‌شرت"],
            new EmbroideryPolicyDto(0, 0, 0, 6, 12, 12, ["LeftChest"], ["#111111"], true, true),
            new SeoDto(
                "محصول تست گلدوزی | Tatakae",
                "خرید محصول تست گلدوزی با انتخاب سایز، رنگ و امکان شخصی‌سازی آنلاین.",
                "/product/test-product",
                "/images/test-product.webp",
                true,
                true));
    }
}
