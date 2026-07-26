using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Contracts.Seo;
using Tatakae.Application.Seo;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Services;

public sealed partial class SeoService
{
    public async Task<AiCatalogDocumentDto> BuildAiCatalogAsync(
        string? publicBaseUrl,
        AiSeoSiteProfileDto profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var normalizedProfile = NormalizeProfile(profile);
        var baseUrl = NormalizeBaseUrl(publicBaseUrl);
        var generatedAt = DateTimeOffset.UtcNow;

        var productData = (await products.GetAllAsync(cancellationToken)).RequireData();
        var categoryData = (await categories.GetAllAsync(cancellationToken)).RequireData();
        var publishedPolicies = await legalPages.GetPublishedAsync(cancellationToken);

        var publicProducts = productData
            .Where(x => x.IsPublished && x.Seo.AllowIndex)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.IsInStock)
            .ThenBy(x => x.Name)
            .Take(normalizedProfile.MaxProductsInLlms)
            .ToArray();

        var publicCategories = categoryData
            .Where(x => x.IsActive && x.Seo.AllowIndex)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToArray();

        var productsByCategory = publicProducts
            .GroupBy(x => x.CategoryId)
            .ToDictionary(x => x.Key, x => x.Count());

        var categoryMap = publicCategories.ToDictionary(x => x.Id);
        var catalogCategories = publicCategories.Select(category => new AiCatalogCategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            AbsoluteUrl(baseUrl, category.Seo.CanonicalPath ?? $"/category/{category.Slug}"),
            PlainText(category.Description),
            productsByCategory.GetValueOrDefault(category.Id))).ToArray();

        var catalogProducts = publicProducts.Select(product =>
        {
            var category = categoryMap.GetValueOrDefault(product.CategoryId);
            var categoryPath = category?.Seo.CanonicalPath ?? $"/category/{category?.Slug ?? product.ApparelCategory.ToString().ToLowerInvariant()}";
            var activeVariants = product.Variants
                .Where(x => x.IsActive)
                .OrderBy(x => x.Size)
                .ThenBy(x => x.ColorName)
                .Select(variant => new AiCatalogVariantDto(
                    variant.Sku,
                    variant.Size,
                    variant.ColorName,
                    variant.ColorHex,
                    ToPublishedCurrency(variant.EffectivePrice, normalizedProfile.Currency),
                    variant.SalePrice.HasValue ? ToPublishedCurrency(variant.RegularPrice, normalizedProfile.Currency) : null,
                    variant.IsInStock))
                .ToArray();

            return new AiCatalogProductDto(
                product.Id,
                product.Name,
                product.Slug,
                AbsoluteUrl(baseUrl, product.Seo.CanonicalPath ?? $"/product/{product.Slug}"),
                category?.Name ?? product.ApparelCategory.ToString(),
                AbsoluteUrl(baseUrl, categoryPath),
                PlainText(product.ShortDescription),
                PlainText(product.Description),
                product.Material,
                product.Fit,
                PlainText(product.CareGuide),
                product.SupportsEmbroidery,
                product.IsReadyMade,
                product.IsInStock,
                ToPublishedCurrency(product.StartingPrice, normalizedProfile.Currency),
                normalizedProfile.Currency,
                product.Tags.OrderBy(x => x).ToArray(),
                activeVariants,
                product.UpdatedAt);
        }).ToArray();

        var policies = publishedPolicies
            .Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .Select(page => new AiCatalogPolicyDto(
                page.Slug,
                page.Title,
                AbsoluteUrl(baseUrl, SeoSlug.LegalPagePath(page.Slug)),
                PlainText(string.IsNullOrWhiteSpace(page.Summary) ? page.Body : page.Summary),
                PlainText(page.Body),
                page.UpdatedAt))
            .ToArray();

        return new AiCatalogDocumentDto(
            normalizedProfile.SiteName,
            normalizedProfile.OrganizationName,
            normalizedProfile.Summary,
            normalizedProfile.Language,
            normalizedProfile.Currency,
            normalizedProfile.AreaServed,
            baseUrl,
            generatedAt,
            catalogCategories,
            catalogProducts,
            policies);
    }

    public async Task<AiSeoDocumentDto> BuildLlmsDocumentAsync(
        string? publicBaseUrl,
        AiSeoSiteProfileDto profile,
        bool includeFullCatalog = false,
        CancellationToken cancellationToken = default)
    {
        var catalog = await BuildAiCatalogAsync(publicBaseUrl, profile, cancellationToken);
        var builder = new StringBuilder();

        builder.AppendLine($"# {MarkdownText(catalog.SiteName)}");
        builder.AppendLine();
        builder.AppendLine($"> {MarkdownText(catalog.Summary)}");
        builder.AppendLine();
        builder.AppendLine($"- Language: {catalog.Language}");
        builder.AppendLine($"- Currency: {catalog.Currency}");
        builder.AppendLine($"- Area served: {MarkdownText(catalog.AreaServed)}");
        builder.AppendLine($"- Canonical origin: {catalog.BaseUrl}");
        builder.AppendLine($"- Updated: {catalog.GeneratedAt:O}");
        builder.AppendLine();

        builder.AppendLine("## Primary pages");
        builder.AppendLine($"- [Home]({catalog.BaseUrl}/): معرفی برند و مسیر ورود به فروشگاه");
        builder.AppendLine($"- [Shop]({catalog.BaseUrl}/shop): فهرست محصولات، دسته‌بندی و فیلترها");
        builder.AppendLine($"- [Machine-readable catalog]({catalog.BaseUrl}/ai/catalog.json): داده عمومی محصولات، SKUها، قیمت و موجودی");
        builder.AppendLine($"- [Sitemap]({catalog.BaseUrl}/sitemap.xml): فهرست URLهای عمومی و قابل ایندکس");
        builder.AppendLine();

        if (catalog.Categories.Count > 0)
        {
            builder.AppendLine("## Product categories");
            foreach (var category in catalog.Categories)
            {
                builder.AppendLine($"- [{MarkdownText(category.Name)}]({category.Url}): {MarkdownText(category.Description)} ({category.ProductCount} products)");
            }
            builder.AppendLine();
        }

        builder.AppendLine(includeFullCatalog ? "## Public product catalog" : "## Selected products");
        var products = includeFullCatalog ? catalog.Products : catalog.Products.Take(20).ToArray();
        foreach (var product in products)
        {
            var availability = product.IsInStock ? "in stock" : "out of stock";
            var customization = product.SupportsEmbroidery ? "custom embroidery supported" : "ready-made embroidery";
            builder.AppendLine($"- [{MarkdownText(product.Name)}]({product.Url}): {MarkdownText(product.Summary)}; {availability}; {customization}; from {product.StartingPrice:0.##} {product.Currency}");

            if (!includeFullCatalog)
            {
                continue;
            }

            builder.AppendLine($"  - Category: [{MarkdownText(product.Category)}]({product.CategoryUrl})");
            builder.AppendLine($"  - Material / fit: {MarkdownText(product.Material)} / {MarkdownText(product.Fit)}");
            builder.AppendLine($"  - Care: {MarkdownText(product.CareGuide)}");
            if (product.Tags.Count > 0)
            {
                builder.AppendLine($"  - Tags: {string.Join(", ", product.Tags.Select(MarkdownText))}");
            }
            foreach (var variant in product.Variants)
            {
                builder.AppendLine($"  - SKU {MarkdownText(variant.Sku)}: {MarkdownText(variant.Size)}, {MarkdownText(variant.ColorName)}, {variant.Price:0.##} {product.Currency}, {(variant.IsInStock ? "in stock" : "out of stock")}");
            }
        }
        builder.AppendLine();

        if (catalog.Policies.Count > 0)
        {
            builder.AppendLine("## Policies and trust pages");
            foreach (var policy in catalog.Policies)
            {
                builder.AppendLine($"- [{MarkdownText(policy.Title)}]({policy.Url}): {MarkdownText(policy.Summary)}");
                if (includeFullCatalog && !string.IsNullOrWhiteSpace(policy.Content))
                {
                    builder.AppendLine($"  - Full public text: {MarkdownText(policy.Content)}");
                }
            }
            builder.AppendLine();
        }

        builder.AppendLine("## Usage notes");
        builder.AppendLine("- Cite the canonical product or policy URL when answering about Tatakae.");
        builder.AppendLine("- Treat price, availability and policy details as time-sensitive and verify the linked page or catalog feed.");
        builder.AppendLine("- Do not infer private customer, order, account, checkout or payment information; those routes are intentionally excluded.");

        return new AiSeoDocumentDto(
            builder.ToString(),
            catalog.GeneratedAt,
            catalog.Products.Count,
            catalog.Categories.Count,
            catalog.Policies.Count);
    }

    private static AiSeoSiteProfileDto NormalizeProfile(AiSeoSiteProfileDto profile)
        => profile with
        {
            SiteName = string.IsNullOrWhiteSpace(profile.SiteName) ? "Tatakae" : profile.SiteName.Trim(),
            OrganizationName = string.IsNullOrWhiteSpace(profile.OrganizationName) ? "Tatakae" : profile.OrganizationName.Trim(),
            Summary = string.IsNullOrWhiteSpace(profile.Summary)
                ? "فروشگاه ایرانی پوشاک گلدوزی آماده و قابل شخصی‌سازی."
                : PlainText(profile.Summary),
            Language = string.IsNullOrWhiteSpace(profile.Language) ? "fa-IR" : profile.Language.Trim(),
            Currency = string.IsNullOrWhiteSpace(profile.Currency) ? "IRR" : profile.Currency.Trim().ToUpperInvariant(),
            AreaServed = string.IsNullOrWhiteSpace(profile.AreaServed) ? "Iran" : profile.AreaServed.Trim(),
            MaxProductsInLlms = Math.Clamp(profile.MaxProductsInLlms, 1, 500)
        };

    private static string AbsoluteUrl(string baseUrl, string path)
        => baseUrl + NormalizePath(path);

    private static decimal ToPublishedCurrency(decimal amount, string currency)
        => currency.Equals("IRR", StringComparison.OrdinalIgnoreCase) ? amount * 10m : amount;

    private static string PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var withoutMarkup = Regex.Replace(value, "<[^>]+>", " ", RegexOptions.CultureInvariant);
        return Regex.Replace(WebUtility.HtmlDecode(withoutMarkup), @"\s+", " ").Trim();
    }

    private static string MarkdownText(string? value)
        => PlainText(value).Replace("[", "\\[").Replace("]", "\\]");
}
