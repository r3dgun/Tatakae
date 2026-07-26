using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Contracts.Seo;

public sealed class SeoPageInputDto
{
    [Required, StringLength(65, MinimumLength = 10)]
    public string MetaTitle { get; set; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 30)]
    public string MetaDescription { get; set; } = string.Empty;

    [StringLength(120)]
    public string? FocusKeyword { get; set; }

    [Url, StringLength(1000)]
    public string? OpenGraphImageUrl { get; set; }

    [Url, StringLength(1000)]
    public string? CanonicalUrl { get; set; }

    public bool AllowIndex { get; set; } = true;
    public bool AllowFollow { get; set; } = true;
}

public sealed record SeoPageDto(
    string Title,
    string Description,
    string CanonicalUrl,
    string Robots,
    string? OpenGraphImageUrl,
    string? JsonLd,
    IReadOnlyCollection<BreadcrumbItemDto> Breadcrumbs);

public sealed record SitemapUrlDto(string Location, DateTimeOffset LastModified, string ChangeFrequency, decimal Priority)
{
    public string? ImageUrl { get; init; }
    public string? ImageTitle { get; init; }
}

public sealed record SeoAuditDto(string Url, int Score, IReadOnlyCollection<SeoAuditItemDto> Items);

public sealed record SeoAuditItemDto(string Level, string Code, string Message, string? FixHint);

public sealed record SeoSitemapDocumentDto(
    string PublicBaseUrl,
    DateTimeOffset GeneratedAt,
    IReadOnlyCollection<SitemapUrlDto> Urls);

public sealed record SeoAuditSummaryDto(
    int Score,
    int UrlCount,
    int ErrorCount,
    int WarningCount,
    IReadOnlyCollection<SeoAuditDto> Pages,
    IReadOnlyCollection<SitemapUrlDto> SitemapUrls,
    IReadOnlyCollection<SeoRoutePolicyDto> RoutePolicies);

public sealed record SeoRoutePolicyDto(
    string Path,
    string Robots,
    bool IsPublic,
    string Reason);

public sealed record AiSeoSiteProfileDto(
    string SiteName,
    string OrganizationName,
    string Summary,
    string Language,
    string Currency,
    string AreaServed,
    string? SupportEmail,
    string? SupportPhone,
    int MaxProductsInLlms = 100);

public sealed record AiSeoDocumentDto(
    string Content,
    DateTimeOffset GeneratedAt,
    int ProductCount,
    int CategoryCount,
    int PolicyCount);

public sealed record AiCatalogVariantDto(
    string Sku,
    string Size,
    string ColorName,
    string ColorHex,
    decimal Price,
    decimal? RegularPrice,
    bool IsInStock);

public sealed record AiCatalogProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Url,
    string Category,
    string CategoryUrl,
    string Summary,
    string Description,
    string Material,
    string Fit,
    string CareGuide,
    bool SupportsEmbroidery,
    bool IsReadyMade,
    bool IsInStock,
    decimal StartingPrice,
    string Currency,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<AiCatalogVariantDto> Variants,
    DateTimeOffset UpdatedAt);

public sealed record AiCatalogCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string Url,
    string Description,
    int ProductCount);

public sealed record AiCatalogPolicyDto(
    string Slug,
    string Title,
    string Url,
    string Summary,
    string Content,
    DateTimeOffset UpdatedAt);

public sealed record AiCatalogDocumentDto(
    string SiteName,
    string OrganizationName,
    string Summary,
    string Language,
    string Currency,
    string AreaServed,
    string BaseUrl,
    DateTimeOffset GeneratedAt,
    IReadOnlyCollection<AiCatalogCategoryDto> Categories,
    IReadOnlyCollection<AiCatalogProductDto> Products,
    IReadOnlyCollection<AiCatalogPolicyDto> Policies);
