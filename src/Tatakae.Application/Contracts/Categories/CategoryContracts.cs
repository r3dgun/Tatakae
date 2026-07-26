using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Seo;

namespace Tatakae.Application.Contracts.Categories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string? CoverImageUrl,
    int ProductCount,
    bool IsActive,
    int SortOrder,
    SeoDto Seo);

public sealed class AdminCategoryRequest
{
    [Required(ErrorMessage = "نام دسته‌بندی الزامی است.")]
    [StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression(SeoSlug.ValidationPattern, ErrorMessage = "Slug می‌تواند فارسی یا انگلیسی باشد؛ بین واژه‌ها فاصله یا خط تیره بگذارید.")]
    [StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(400)]
    public string Description { get; set; } = string.Empty;

    [Url]
    [StringLength(600)]
    public string? CoverImageUrl { get; set; }

    [Range(0, 9999)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public SeoInput Seo { get; set; } = new();
}

public sealed class SeoInput
{
    [Required]
    [StringLength(65, MinimumLength = 10)]
    public string MetaTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(160, MinimumLength = 30)]
    public string MetaDescription { get; set; } = string.Empty;

    [StringLength(500)]
    public string? CanonicalPath { get; set; }

    [Url]
    public string? OpenGraphImageUrl { get; set; }

    public bool AllowIndex { get; set; } = true;
    public bool AllowFollow { get; set; } = true;
}

public sealed record SeoDto(
    string MetaTitle,
    string MetaDescription,
    string? CanonicalPath,
    string? OpenGraphImageUrl,
    bool AllowIndex,
    bool AllowFollow);
