using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Seo;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Seo;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class SeoService(
    IProductRepository products, ICategoryRepository categories, IStorePolicyPageReader legalPages,
    ILogger<SeoService>? logger = null) : ISeoService
{
    private readonly ILogger<SeoService> _logger = logger ?? NullLogger<SeoService>.Instance;
    private static readonly string[] PublicStaticPages = ["/", "/shop"];

    private static readonly SeoRoutePolicyDto[] RoutePolicies =
    [
        new("/", "index,follow", true, "صفحه اصلی قابل ایندکس است."),
        new("/shop", "index,follow", true, "فروشگاه قابل ایندکس است."),
        new("/category/*", "index,follow", true, "دسته‌بندی‌های فعال قابل ایندکس هستند."),
        new("/product/*", "index,follow", true, "محصولات منتشرشده قابل ایندکس هستند."),
        new("/about", "index,follow", true, "صفحه اعتماد و معرفی برند قابل ایندکس است."),
        new("/rules", "index,follow", true, "صفحه قوانین برای اعتماد مشتری قابل ایندکس است."),
        new("/terms", "index,follow", true, "صفحه شرایط استفاده قابل ایندکس است."),
        new("/privacy", "index,follow", true, "صفحه حریم خصوصی قابل ایندکس است."),
        new("/returns", "index,follow", true, "صفحه مرجوعی قابل ایندکس است."),
        new("/shipping-policy", "index,follow", true, "صفحه ارسال قابل ایندکس است."),
        new("/contact", "index,follow", true, "صفحه تماس قابل ایندکس است."),
        new("/llms.txt", "noindex,follow", true, "راهنمای عمومی و قابل خزش برای دستیارهای هوش مصنوعی است."),
        new("/llms-full.txt", "noindex,follow", true, "نسخه کامل راهنمای عمومی هوش مصنوعی است و نباید نتیجه مستقل جستجو باشد."),
        new("/ai/catalog.json", "noindex,follow", true, "کاتالوگ عمومی و ماشین‌خوان محصولات است."),
        new("/admin/*", "noindex,nofollow", false, "پنل مدیریت نباید وارد نتایج جستجو شود."),
        new("/account/*", "noindex,nofollow", false, "اطلاعات حساب مشتری خصوصی است."),
        new("/checkout", "noindex,nofollow", false, "فرآیند خرید صفحه عمومی سئو نیست."),
        new("/cart", "noindex,nofollow", false, "سبد خرید محتوای عمومی قابل ایندکس ندارد."),
        new("/customize/*", "noindex,nofollow", false, "استودیو شامل تنظیمات موقت و شخصی مشتری است."),
        new("/payment/*", "noindex,nofollow", false, "صفحه پرداخت خصوصی است."),
        new("/order-success/*", "noindex,nofollow", false, "نتیجه سفارش و شناسه خرید خصوصی است."),
        new("/kimi-award", "noindex,nofollow", false, "نمای آزمایشی رابط کاربری وارد نتایج جستجو نمی‌شود."),
        new("/login", "noindex,nofollow", false, "صفحه ورود نباید ایندکس شود."),
        new("/register", "noindex,nofollow", false, "صفحه ثبت‌نام نباید ایندکس شود.")
    ];

    public IReadOnlyCollection<SeoRoutePolicyDto> GetRoutePolicies() => RoutePolicies;

    public async Task<SeoSitemapDocumentDto> GetSitemapAsync(string? publicBaseUrl, CancellationToken cancellationToken = default)
    {
        var baseUrl = NormalizeBaseUrl(publicBaseUrl);
        var now = DateTimeOffset.UtcNow;
        var urls = new List<SitemapUrlDto>();

        foreach (var path in PublicStaticPages)
        {
            urls.Add(new SitemapUrlDto(baseUrl + path, now, path == "/" ? "daily" : "weekly", path == "/" ? 1.0m : 0.85m));
        }

        var publishedLegalPages = await legalPages.GetPublishedAsync(cancellationToken);
        foreach (var page in publishedLegalPages.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
        {
            urls.Add(new SitemapUrlDto(baseUrl + SeoSlug.LegalPagePath(page.Slug), page.UpdatedAt, "monthly", 0.65m));
        }

        var categoryData = (await categories.GetAllAsync(cancellationToken)).RequireData();
        foreach (var category in categoryData.Where(x => x.IsActive && x.Seo.AllowIndex).OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
        {
            var path = category.Seo.CanonicalPath ?? $"/category/{category.Slug}";
            var image = category.Seo.OpenGraphImageUrl ?? category.CoverImageUrl;
            urls.Add(new SitemapUrlDto(baseUrl + NormalizePath(path), now, "weekly", 0.80m)
            {
                ImageUrl = string.IsNullOrWhiteSpace(image) ? null : AbsolutePublicUrl(baseUrl, image),
                ImageTitle = category.Name
            });
        }

        var productData = (await products.GetAllAsync(cancellationToken)).RequireData();
        foreach (var product in productData.Where(x => x.IsPublished && x.Seo.AllowIndex).OrderByDescending(x => x.IsFeatured).ThenBy(x => x.Name))
        {
            var path = product.Seo.CanonicalPath ?? $"/product/{product.Slug}";
            var image = product.Seo.OpenGraphImageUrl ?? product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).FirstOrDefault()?.Url;
            urls.Add(new SitemapUrlDto(baseUrl + NormalizePath(path), product.UpdatedAt, product.IsInStock ? "weekly" : "monthly", product.IsFeatured ? 0.90m : 0.75m)
            {
                ImageUrl = string.IsNullOrWhiteSpace(image) ? null : AbsolutePublicUrl(baseUrl, image),
                ImageTitle = product.Name
            });
        }

        return new SeoSitemapDocumentDto(baseUrl, now, urls.DistinctBy(x => x.Location).ToArray());
    }

    public async Task<SeoAuditSummaryDto> AuditAsync(string? publicBaseUrl, CancellationToken cancellationToken = default)
    {
        var sitemap = await GetSitemapAsync(publicBaseUrl, cancellationToken);
        var pageAudits = new List<SeoAuditDto>();

        var productsData = (await products.GetAllAsync(cancellationToken)).RequireData();
        foreach (var product in productsData.Where(x => x.IsPublished))
        {
            pageAudits.Add(AuditProduct(sitemap.PublicBaseUrl, product));
        }

        var categoryData = (await categories.GetAllAsync(cancellationToken)).RequireData();
        foreach (var category in categoryData.Where(x => x.IsActive))
        {
            pageAudits.Add(AuditCategory(sitemap.PublicBaseUrl, category));
        }

        pageAudits.Add(AuditStaticPage(sitemap.PublicBaseUrl + "/", "TATAKAE | Ink Editorial Embroidery Store", "فروشگاه لباس گلدوزی با پوسته Ink Editorial Anime Store؛ انتخاب محصول، استودیو گلدوزی و ثبت سفارش آنلاین."));
        pageAudits.Add(AuditStaticPage(sitemap.PublicBaseUrl + "/shop", "TATAKAE | فروشگاه گلدوزی", "فروشگاه لباس گلدوزی با استایل Ink Editorial؛ تی‌شرت، هودی، دورس و پولوشرت قابل سفارشی‌سازی."));

        var publishedLegalPages = await legalPages.GetPublishedAsync(cancellationToken);
        foreach (var page in publishedLegalPages)
        {
            pageAudits.Add(AuditStaticPage(
                sitemap.PublicBaseUrl + SeoSlug.LegalPagePath(page.Slug),
                string.IsNullOrWhiteSpace(page.SeoTitle) ? $"{page.Title} | Tatakae" : page.SeoTitle,
                string.IsNullOrWhiteSpace(page.SeoDescription) ? page.Summary : page.SeoDescription));
        }

        var errors = pageAudits.Sum(x => x.Items.Count(i => i.Level == "Error"));
        var warnings = pageAudits.Sum(x => x.Items.Count(i => i.Level == "Warning"));
        var totalScore = pageAudits.Count == 0 ? 100 : (int)Math.Round(pageAudits.Average(x => x.Score));

        return new SeoAuditSummaryDto(totalScore, sitemap.Urls.Count, errors, warnings, pageAudits.OrderBy(x => x.Score).ToArray(), sitemap.Urls, RoutePolicies);
    }

    private static SeoAuditDto AuditProduct(string baseUrl, Product product)
    {
        var url = baseUrl + NormalizePath(product.Seo.CanonicalPath ?? $"/product/{product.Slug}");
        var items = new List<SeoAuditItemDto>();
        CheckTitle(product.Seo.MetaTitle, items);
        CheckDescription(product.Seo.MetaDescription, items);
        CheckCanonical(product.Seo.CanonicalPath, $"/product/{product.Slug}", items);
        if (string.IsNullOrWhiteSpace(product.Seo.OpenGraphImageUrl) && product.Images.All(x => string.IsNullOrWhiteSpace(x.Url)))
        {
            items.Add(new SeoAuditItemDto("Warning", "og_image_missing", "تصویر Open Graph یا تصویر اصلی محصول خالی است.", "برای محصول تصویر اصلی یا OpenGraphImageUrl ثبت کن."));
        }
        if (!product.Variants.Any(x => x.IsActive))
        {
            items.Add(new SeoAuditItemDto("Warning", "variant_missing", "محصول SKU فعال ندارد.", "حداقل یک تنوع فعال برای محصول ثبت کن."));
        }
        if (!product.Seo.AllowIndex)
        {
            items.Add(new SeoAuditItemDto("Warning", "product_noindex", "محصول منتشر شده ولی noindex است.", "اگر محصول باید در گوگل بیاید AllowIndex را فعال کن."));
        }
        if (product.ShortDescription.Length < 50)
        {
            items.Add(new SeoAuditItemDto("Warning", "ai_summary_short", "خلاصه محصول برای پاسخ مستقیم و موتورهای هوش مصنوعی کوتاه است.", "یک خلاصه factual حداقل ۵۰ کاراکتری شامل نوع محصول، کاربرد و مزیت اصلی بنویس."));
        }
        if (product.Specifications.Count == 0)
        {
            items.Add(new SeoAuditItemDto("Warning", "ai_specs_missing", "محصول مشخصات ساختاریافته ندارد.", "جنس، فیت، وزن پارچه، روش نگهداری یا ویژگی‌های قابل استناد را ثبت کن."));
        }
        if (product.Tags.Count == 0)
        {
            items.Add(new SeoAuditItemDto("Warning", "ai_tags_missing", "محصول واژگان موضوعی و تگ ندارد.", "تگ‌های واقعی محصول و کاربرد را بدون انباشت کلمه کلیدی ثبت کن."));
        }
        return new SeoAuditDto(url, Score(items), items);
    }

    private static SeoAuditDto AuditCategory(string baseUrl, Category category)
    {
        var url = baseUrl + NormalizePath(category.Seo.CanonicalPath ?? $"/category/{category.Slug}");
        var items = new List<SeoAuditItemDto>();
        CheckTitle(category.Seo.MetaTitle, items);
        CheckDescription(category.Seo.MetaDescription, items);
        CheckCanonical(category.Seo.CanonicalPath, $"/category/{category.Slug}", items);
        if (string.IsNullOrWhiteSpace(category.CoverImageUrl) && string.IsNullOrWhiteSpace(category.Seo.OpenGraphImageUrl))
        {
            items.Add(new SeoAuditItemDto("Warning", "category_image_missing", "دسته‌بندی تصویر کاور یا Open Graph ندارد.", "برای دسته‌بندی تصویر کاور ثبت کن."));
        }
        if (category.Description.Length < 50)
        {
            items.Add(new SeoAuditItemDto("Warning", "ai_category_summary_short", "توضیح دسته‌بندی برای پاسخ‌های هوش مصنوعی کافی نیست.", "تعریف دسته، نوع محصولات، قابلیت سفارشی‌سازی و مخاطب آن را توضیح بده."));
        }
        return new SeoAuditDto(url, Score(items), items);
    }

    private static SeoAuditDto AuditStaticPage(string url, string title, string description)
    {
        var items = new List<SeoAuditItemDto>();
        CheckTitle(title, items);
        CheckDescription(description, items);
        return new SeoAuditDto(url, Score(items), items);
    }

    private static void CheckTitle(string? title, ICollection<SeoAuditItemDto> items)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            items.Add(new SeoAuditItemDto("Error", "title_missing", "عنوان متا خالی است.", "برای صفحه MetaTitle بنویس."));
            return;
        }
        if (title.Length < 10) items.Add(new SeoAuditItemDto("Warning", "title_short", "عنوان متا خیلی کوتاه است.", "عنوان را حداقل ۱۰ کاراکتر کن."));
        if (title.Length > 65) items.Add(new SeoAuditItemDto("Warning", "title_long", "عنوان متا طولانی است.", "عنوان را زیر ۶۵ کاراکتر نگه دار."));
    }

    private static void CheckDescription(string? description, ICollection<SeoAuditItemDto> items)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            items.Add(new SeoAuditItemDto("Error", "description_missing", "توضیحات متا خالی است.", "برای صفحه MetaDescription بنویس."));
            return;
        }
        if (description.Length < 30) items.Add(new SeoAuditItemDto("Warning", "description_short", "توضیحات متا کوتاه است.", "توضیحات را حداقل ۳۰ کاراکتر کن."));
        if (description.Length > 160) items.Add(new SeoAuditItemDto("Warning", "description_long", "توضیحات متا طولانی است.", "توضیحات را زیر ۱۶۰ کاراکتر نگه دار."));
    }

    private static void CheckCanonical(string? canonicalPath, string expectedPath, ICollection<SeoAuditItemDto> items)
    {
        if (string.IsNullOrWhiteSpace(canonicalPath))
        {
            items.Add(new SeoAuditItemDto("Warning", "canonical_missing", "Canonical خالی است.", $"Canonical را روی {expectedPath} بگذار."));
            return;
        }
        if (!NormalizePath(canonicalPath).Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new SeoAuditItemDto("Warning", "canonical_mismatch", "Canonical با مسیر اصلی صفحه یکی نیست.", $"Canonical بهتر است {expectedPath} باشد."));
        }
    }

    private static int Score(IReadOnlyCollection<SeoAuditItemDto> items)
    {
        var score = 100 - (items.Count(x => x.Level == "Error") * 35) - (items.Count(x => x.Level == "Warning") * 10);
        return Math.Clamp(score, 0, 100);
    }

    [Obsolete("Use SeoSlug.LegalPagePath instead.")]
    public static string LegalPagePath(string slug) => SeoSlug.LegalPagePath(slug);

    public static string NormalizeBaseUrl(string? publicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl)) return "https://localhost:7076";
        return publicBaseUrl.Trim().TrimEnd('/');
    }

    private static string AbsolutePublicUrl(string baseUrl, string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : baseUrl + NormalizePath(value);

    public static string NormalizePath(string path)
        => SeoSlug.NormalizeCanonicalPath(path, "/");
}
