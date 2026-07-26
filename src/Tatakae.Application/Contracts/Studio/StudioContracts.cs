using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Contracts.Embroidery;

namespace Tatakae.Application.Contracts.Studio;

public sealed class StudioPreviewRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid VariantId { get; set; }

    [Required]
    public EmbroideryCustomizationRequest Customization { get; set; } = new();
}

public sealed record StudioPresetDto(string Key, string Title, string Description, string SvgMarkup, int DefaultScalePercent, IReadOnlyCollection<string> RecommendedPlacements);

public sealed record StudioGarmentOptionDto(string Type, string Label, string SvgPathKey, IReadOnlyCollection<string> SupportedSizes);

public sealed record StudioStateDto(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string ProductSlug,
    string ProductImageUrl,
    IReadOnlyCollection<StudioGarmentOptionDto> Garments,
    IReadOnlyCollection<StudioPresetDto> Presets,
    IReadOnlyCollection<string> AllowedPlacements,
    IReadOnlyCollection<string> AllowedThreadColors,
    EmbroideryCustomizationRequest DefaultCustomization);

public sealed record StudioPreviewDto(string SvgMarkup, decimal EmbroideryPrice, decimal TotalUnitPrice, IReadOnlyCollection<string> ProductionWarnings);
