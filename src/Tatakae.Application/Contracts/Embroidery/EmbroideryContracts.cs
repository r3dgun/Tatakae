using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Embroidery;

/// <summary>
/// Complete studio payload based on the supplied Kimi-style studio:
/// garment type, size, garment color, ready motif/upload/text, precise drag position,
/// scale, rotation, opacity, embroidery placement, dimensions and thread colors.
/// </summary>
public sealed class EmbroideryCustomizationRequest : IValidatableObject
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid VariantId { get; set; }

    [Required]
    [RegularExpression("^(TShirt|Hoodie|Sweatshirt|Crewneck)$", ErrorMessage = "نوع لباس معتبر نیست.")]
    public string GarmentType { get; set; } = "TShirt";

    [Required]
    [StringLength(20)]
    public string GarmentSize { get; set; } = "L";

    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "رنگ لباس باید به شکل #RRGGBB باشد.")]
    public string GarmentColorHex { get; set; } = "#111827";

    [Required]
    [RegularExpression("^(LeftChest|CenterChest|RightChest|BackNeck|LeftSleeve|RightSleeve)$")]
    public string Placement { get; set; } = "CenterChest";

    [Range(typeof(decimal), "1", "40")]
    public decimal WidthCm { get; set; } = 9m;

    [Range(typeof(decimal), "1", "40")]
    public decimal HeightCm { get; set; } = 9m;

    [Range(1, 12)]
    public int ThreadColorCount { get; set; } = 1;

    [MinLength(1)]
    [MaxLength(12)]
    public List<string> ThreadColorHexes { get; set; } = ["#FFFFFF"];

    [Required]
    [RegularExpression("^(Motif|Upload|Text)$", ErrorMessage = "نوع طرح معتبر نیست.")]
    public string DesignSource { get; set; } = "Motif";

    [RegularExpression("^(dragon|sword|cloud|custom)?$", ErrorMessage = "موتیف انتخابی معتبر نیست.")]
    [StringLength(30)]
    public string? MotifKey { get; set; } = "dragon";

    [StringLength(10000000)]
    public string? ArtworkFileUrl { get; set; }

    [StringLength(150)]
    public string? ArtworkFileName { get; set; }

    [StringLength(30)]
    public string? Text { get; set; }

    [StringLength(80)]
    public string? FontName { get; set; }

    [Range(-260, 260)]
    public int PositionX { get; set; }

    [Range(-260, 260)]
    public int PositionY { get; set; }

    [Range(35, 210)]
    public int ScalePercent { get; set; } = 100;

    [Range(-180, 180)]
    public int RotationDegrees { get; set; }

    [Range(35, 100)]
    public int OpacityPercent { get; set; } = 100;

    [StringLength(500, ErrorMessage = "یادداشت سفارش حداکثر ۵۰۰ کاراکتر است.")]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductId == Guid.Empty)
        {
            yield return new ValidationResult("محصول برای شخصی‌سازی معتبر نیست.", [nameof(ProductId)]);
        }
        if (VariantId == Guid.Empty)
        {
            yield return new ValidationResult("سایز و رنگ محصول را انتخاب کنید.", [nameof(VariantId)]);
        }
        if (ThreadColorHexes.Count != ThreadColorCount)
        {
            yield return new ValidationResult("تعداد رنگ‌های نخ با رنگ‌های انتخاب‌شده هم‌خوان نیست.", [nameof(ThreadColorCount), nameof(ThreadColorHexes)]);
        }
        if (ThreadColorHexes.Any(x => !System.Text.RegularExpressions.Regex.IsMatch(x ?? string.Empty, "^#[0-9A-Fa-f]{6}$")))
        {
            yield return new ValidationResult("یکی از رنگ‌های نخ معتبر نیست.", [nameof(ThreadColorHexes)]);
        }
        if (DesignSource == "Text" && string.IsNullOrWhiteSpace(Text))
        {
            yield return new ValidationResult("متن گلدوزی را وارد کنید.", [nameof(Text)]);
        }
        if (DesignSource == "Upload" && string.IsNullOrWhiteSpace(ArtworkFileUrl))
        {
            yield return new ValidationResult("فایل طرح گلدوزی را آپلود کنید.", [nameof(ArtworkFileUrl)]);
        }
        if (DesignSource == "Motif" && string.IsNullOrWhiteSpace(MotifKey))
        {
            yield return new ValidationResult("یک طرح آماده انتخاب کنید.", [nameof(MotifKey)]);
        }
    }
}

public sealed record EmbroideryQuoteDto(
    decimal BaseEmbroideryPrice,
    decimal ThreadColorsPrice,
    decimal AreaPrice,
    decimal TotalEmbroideryPrice,
    string PlacementLabel,
    bool IsValid,
    IReadOnlyCollection<string> Errors)
{
    // Backward-compatible alias used by the Studio UI.
    // Pricing/service validation still fills Errors; the UI can safely read Warnings.
    public IReadOnlyCollection<string> Warnings => Errors;
}

public sealed record EmbroideryConfigurationDto(
    Guid Id,
    string Placement,
    string PlacementLabel,
    decimal WidthCm,
    decimal HeightCm,
    int ThreadColorCount,
    IReadOnlyCollection<string> ThreadColorHexes,
    string? ArtworkFileUrl,
    string? ArtworkFileName,
    string? Text,
    string? FontName,
    string? Note,
    decimal CalculatedPrice,
    string GarmentType,
    string GarmentSize,
    string GarmentColorHex,
    string DesignSource,
    string? MotifKey,
    int PositionX,
    int PositionY,
    int ScalePercent,
    int RotationDegrees,
    int OpacityPercent);
