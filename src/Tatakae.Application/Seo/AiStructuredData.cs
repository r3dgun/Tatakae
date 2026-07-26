using System.Text.Json;
using System.Text.Json.Serialization;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;

namespace Tatakae.Application.Seo;

/// <summary>
/// Builds factual JSON-LD graphs from the same DTOs rendered to users. The
/// output intentionally excludes private data and does not invent reviews,
/// prices, availability, contact details or policies.
/// </summary>
public static class AiStructuredData
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null
    };

    public static string BuildSiteGraph(
        string baseUrl,
        string siteName,
        string description,
        string? logoUrl = null,
        string language = "fa-IR",
        string? supportEmail = null,
        string? supportPhone = null)
    {
        baseUrl = NormalizeBaseUrl(baseUrl);
        var organization = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["@id"] = $"{baseUrl}/#organization",
            ["name"] = siteName,
            ["url"] = $"{baseUrl}/",
            ["description"] = description,
            ["logo"] = string.IsNullOrWhiteSpace(logoUrl) ? null : new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = AbsoluteUrl(baseUrl, logoUrl)
            },
            ["contactPoint"] = string.IsNullOrWhiteSpace(supportEmail) && string.IsNullOrWhiteSpace(supportPhone)
                ? null
                : new Dictionary<string, object?>
                {
                    ["@type"] = "ContactPoint",
                    ["contactType"] = "customer support",
                    ["email"] = NullIfEmpty(supportEmail),
                    ["telephone"] = NullIfEmpty(supportPhone),
                    ["availableLanguage"] = new[] { "Persian" }
                }
        };

        var website = new Dictionary<string, object?>
        {
            ["@type"] = "WebSite",
            ["@id"] = $"{baseUrl}/#website",
            ["url"] = $"{baseUrl}/",
            ["name"] = siteName,
            ["description"] = description,
            ["inLanguage"] = language,
            ["publisher"] = new Dictionary<string, object?> { ["@id"] = $"{baseUrl}/#organization" }
        };

        return SerializeGraph(organization, website);
    }

    public static string BuildShopGraph(
        string baseUrl,
        IReadOnlyCollection<ProductCardDto> products,
        string title,
        string description,
        string language = "fa-IR")
    {
        baseUrl = NormalizeBaseUrl(baseUrl);
        var shopUrl = $"{baseUrl}/shop";
        var page = new Dictionary<string, object?>
        {
            ["@type"] = "CollectionPage",
            ["@id"] = $"{shopUrl}#page",
            ["name"] = title,
            ["description"] = description,
            ["url"] = shopUrl,
            ["inLanguage"] = language,
            ["isPartOf"] = new Dictionary<string, object?> { ["@id"] = $"{baseUrl}/#website" },
            ["mainEntity"] = ProductItemList(baseUrl, products)
        };

        return SerializeGraph(page, Breadcrumb(baseUrl, [("خانه", "/"), ("فروشگاه", "/shop")]));
    }

    public static string BuildCategoryGraph(
        string baseUrl,
        CategoryDto category,
        PagedResult<ProductCardDto> products,
        string canonicalUrl,
        string language = "fa-IR")
    {
        baseUrl = NormalizeBaseUrl(baseUrl);
        var page = new Dictionary<string, object?>
        {
            ["@type"] = "CollectionPage",
            ["@id"] = $"{canonicalUrl}#page",
            ["name"] = category.Name,
            ["description"] = category.Seo.MetaDescription,
            ["url"] = canonicalUrl,
            ["image"] = AbsoluteUrlOrNull(baseUrl, category.Seo.OpenGraphImageUrl ?? category.CoverImageUrl),
            ["inLanguage"] = language,
            ["isPartOf"] = new Dictionary<string, object?> { ["@id"] = $"{baseUrl}/#website" },
            ["about"] = new Dictionary<string, object?>
            {
                ["@type"] = "Thing",
                ["name"] = category.Name,
                ["description"] = category.Description
            },
            ["mainEntity"] = ProductItemList(baseUrl, products.Items, products.TotalCount)
        };

        return SerializeGraph(page, Breadcrumb(baseUrl,
        [
            ("خانه", "/"),
            ("فروشگاه", "/shop"),
            (category.Name, canonicalUrl)
        ]));
    }

    public static string BuildProductGraph(
        string baseUrl,
        ProductDetailDto product,
        ProductRatingSummaryDto rating,
        IReadOnlyCollection<ProductReviewDto> reviews,
        IReadOnlyCollection<ProductQuestionDto> questions,
        string canonicalUrl,
        IReadOnlyCollection<string> imageUrls,
        string currency = "IRR",
        string language = "fa-IR")
    {
        baseUrl = NormalizeBaseUrl(baseUrl);
        var activeVariants = product.Variants.Where(x => x.IsActive).ToArray();
        var productNode = new Dictionary<string, object?>
        {
            ["@type"] = activeVariants.Length > 1 ? "ProductGroup" : "Product",
            ["@id"] = $"{canonicalUrl}#product",
            ["name"] = product.Name,
            ["description"] = product.Seo.MetaDescription,
            ["url"] = canonicalUrl,
            ["image"] = imageUrls.Select(x => AbsoluteUrl(baseUrl, x)).Distinct().ToArray(),
            ["brand"] = new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = "Tatakae" },
            ["category"] = product.CategoryName,
            ["material"] = product.Material,
            ["inLanguage"] = language,
            ["productGroupID"] = activeVariants.Length > 1 ? product.Id.ToString("N") : null,
            ["variesBy"] = activeVariants.Length > 1
                ? new[] { "https://schema.org/size", "https://schema.org/color" }
                : null,
            ["additionalProperty"] = ProductProperties(product),
            ["hasVariant"] = activeVariants.Length > 1
                ? activeVariants.Select(x => VariantNode(baseUrl, canonicalUrl, product, x, currency)).ToArray()
                : null,
            ["sku"] = activeVariants.Length == 1 ? activeVariants[0].Sku : null,
            ["size"] = activeVariants.Length == 1 ? activeVariants[0].Size : null,
            ["color"] = activeVariants.Length == 1 ? activeVariants[0].ColorName : null,
            ["offers"] = activeVariants.Length == 1
                ? OfferNode(canonicalUrl, activeVariants[0], currency)
                : null
        };

        if (rating.ReviewCount > 0)
        {
            productNode["aggregateRating"] = new Dictionary<string, object?>
            {
                ["@type"] = "AggregateRating",
                ["ratingValue"] = rating.AverageRating,
                ["reviewCount"] = rating.ReviewCount,
                ["bestRating"] = 5,
                ["worstRating"] = 1
            };

            var publicReviews = reviews.Take(5).Select(review => new Dictionary<string, object?>
            {
                ["@type"] = "Review",
                ["author"] = new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = review.CustomerName },
                ["datePublished"] = review.CreatedAt.ToString("yyyy-MM-dd"),
                ["name"] = review.Title,
                ["reviewBody"] = review.Body,
                ["reviewRating"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Rating",
                    ["ratingValue"] = review.Rating,
                    ["bestRating"] = 5,
                    ["worstRating"] = 1
                }
            }).ToArray();
            if (publicReviews.Length > 0) productNode["review"] = publicReviews;
        }

        var graph = new List<object>
        {
            productNode,
            Breadcrumb(baseUrl,
            [
                ("خانه", "/"),
                ("فروشگاه", "/shop"),
                (product.CategoryName, $"/category/{product.CategorySlug}"),
                (product.Name, canonicalUrl)
            ])
        };

        var answeredQuestions = questions
            .Where(x => x.IsAnswered && !string.IsNullOrWhiteSpace(x.AnswerText))
            .Take(20)
            .ToArray();
        if (answeredQuestions.Length > 0)
        {
            graph.Add(new Dictionary<string, object?>
            {
                ["@type"] = "FAQPage",
                ["@id"] = $"{canonicalUrl}#questions",
                ["url"] = $"{canonicalUrl}#questions",
                ["mainEntity"] = answeredQuestions.Select(question => new Dictionary<string, object?>
                {
                    ["@type"] = "Question",
                    ["name"] = question.QuestionText,
                    ["acceptedAnswer"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Answer",
                        ["text"] = question.AnswerText,
                        ["dateCreated"] = question.AnsweredAt?.ToString("O")
                    }
                }).ToArray()
            });
        }

        return SerializeGraph(graph.ToArray());
    }

    private static Dictionary<string, object?> ProductItemList(
        string baseUrl,
        IReadOnlyCollection<ProductCardDto> products,
        int? totalCount = null)
        => new()
        {
            ["@type"] = "ItemList",
            ["numberOfItems"] = totalCount ?? products.Count,
            ["itemListElement"] = products.Select((item, index) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["url"] = AbsoluteUrl(baseUrl, $"/product/{item.Slug}"),
                ["name"] = item.Name,
                ["image"] = AbsoluteUrlOrNull(baseUrl, item.PrimaryImageUrl)
            }).ToArray()
        };

    private static object[] ProductProperties(ProductDetailDto product)
    {
        var properties = product.Specifications.Select(x => new Dictionary<string, object?>
        {
            ["@type"] = "PropertyValue",
            ["name"] = x.Name,
            ["value"] = x.Value
        }).Cast<object>().ToList();
        properties.Add(new Dictionary<string, object?> { ["@type"] = "PropertyValue", ["name"] = "Fit", ["value"] = product.Fit });
        properties.Add(new Dictionary<string, object?> { ["@type"] = "PropertyValue", ["name"] = "Custom embroidery", ["value"] = product.SupportsEmbroidery });
        return properties.ToArray();
    }

    private static Dictionary<string, object?> VariantNode(
        string baseUrl,
        string canonicalUrl,
        ProductDetailDto product,
        ProductVariantDto variant,
        string currency)
        => new()
        {
            ["@type"] = "Product",
            ["@id"] = $"{canonicalUrl}#sku-{Uri.EscapeDataString(variant.Sku)}",
            ["name"] = $"{product.Name} - {variant.Size} - {variant.ColorName}",
            ["sku"] = variant.Sku,
            ["size"] = variant.Size,
            ["color"] = variant.ColorName,
            ["image"] = AbsoluteUrlOrNull(baseUrl, variant.ImageUrl ?? product.Images.FirstOrDefault(x => x.IsPrimary)?.Url),
            ["offers"] = OfferNode(canonicalUrl, variant, currency)
        };

    private static Dictionary<string, object?> OfferNode(
        string canonicalUrl,
        ProductVariantDto variant,
        string currency)
        => new()
        {
            ["@type"] = "Offer",
            ["url"] = $"{canonicalUrl}?variant={variant.Id}",
            ["priceCurrency"] = currency,
            ["price"] = CurrencyAmount(variant.EffectivePrice, currency),
            ["availability"] = variant.IsInStock
                ? "https://schema.org/InStock"
                : "https://schema.org/OutOfStock",
            ["itemCondition"] = "https://schema.org/NewCondition"
        };

    private static Dictionary<string, object?> Breadcrumb(
        string baseUrl,
        IReadOnlyCollection<(string Name, string Path)> items)
        => new()
        {
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items.Select((item, index) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["name"] = item.Name,
                ["item"] = AbsoluteUrl(baseUrl, item.Path)
            }).ToArray()
        };

    private static string SerializeGraph(params object[] nodes)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = nodes
        }, JsonOptions);

    private static decimal CurrencyAmount(decimal amount, string currency)
        => currency.Equals("IRR", StringComparison.OrdinalIgnoreCase) ? amount * 10m : amount;

    private static string NormalizeBaseUrl(string value)
        => value.Trim().TrimEnd('/');

    private static string AbsoluteUrl(string baseUrl, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute)) return absolute.ToString();
        return NormalizeBaseUrl(baseUrl) + SeoSlug.NormalizeCanonicalPath(path, "/");
    }

    private static string? AbsoluteUrlOrNull(string baseUrl, string? path)
        => string.IsNullOrWhiteSpace(path) ? null : AbsoluteUrl(baseUrl, path);

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
