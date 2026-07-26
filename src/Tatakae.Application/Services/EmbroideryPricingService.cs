using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using System.Text.RegularExpressions;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Services;

/// <summary>Single source of truth for studio eligibility and embroidery pricing.</summary>
public sealed partial class EmbroideryPricingService : IEmbroideryPricingService
{
    private readonly ILogger<EmbroideryPricingService> _logger;

    public EmbroideryPricingService(ILogger<EmbroideryPricingService>? logger = null)
    {
        _logger = logger ?? NullLogger<EmbroideryPricingService>.Instance;
    }
    public EmbroideryQuoteDto Quote(Product product, EmbroideryCustomizationRequest request)
    {
        var policy = product.EmbroideryPolicy;
        var errors = new List<string>();

        if (!Enum.TryParse<EmbroideryPlacement>(request.Placement, true, out var placement) || !policy.AllowedPlacements.Contains(placement))
            errors.Add("محل انتخاب‌شده برای این لباس مجاز نیست.");

        if (!AllowedGarments.Contains(request.GarmentType, StringComparer.OrdinalIgnoreCase))
            errors.Add("نوع لباس انتخاب‌شده معتبر نیست.");

        if (string.IsNullOrWhiteSpace(request.GarmentSize))
            errors.Add("سایز لباس الزامی است.");

        if (!HexColorRegex().IsMatch(request.GarmentColorHex))
            errors.Add("رنگ لباس معتبر نیست.");

        if (request.WidthCm <= 0 || request.WidthCm > policy.MaxWidthCm)
            errors.Add($"عرض گلدوزی باید حداکثر {policy.MaxWidthCm} سانتی‌متر باشد.");

        if (request.HeightCm <= 0 || request.HeightCm > policy.MaxHeightCm)
            errors.Add($"ارتفاع گلدوزی باید حداکثر {policy.MaxHeightCm} سانتی‌متر باشد.");

        if (request.ThreadColorCount < 1 || request.ThreadColorCount > policy.MaxThreadColors)
            errors.Add($"تعداد رنگ نخ باید بین ۱ تا {policy.MaxThreadColors} باشد.");

        if (request.ThreadColorHexes.Count != request.ThreadColorCount)
            errors.Add("تعداد رنگ‌های انتخابی باید با تعداد رنگ نخ برابر باشد.");

        if (request.ThreadColorHexes.Any(hex => !policy.AllowedThreadColors.Contains(hex, StringComparer.OrdinalIgnoreCase)))
            errors.Add("یکی از رنگ‌های نخ برای این محصول مجاز نیست.");

        if (!policy.AllowArtworkUpload && !string.IsNullOrWhiteSpace(request.ArtworkFileUrl))
            errors.Add("آپلود طرح برای این محصول غیرفعال است.");

        if (!policy.AllowTextEmbroidery && !string.IsNullOrWhiteSpace(request.Text))
            errors.Add("گلدوزی متن برای این محصول غیرفعال است.");

        if (!AllowedDesignSources.Contains(request.DesignSource, StringComparer.OrdinalIgnoreCase))
            errors.Add("نوع طرح معتبر نیست.");

        var hasMotif = string.Equals(request.DesignSource, "Motif", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.MotifKey);
        var hasUpload = string.Equals(request.DesignSource, "Upload", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.ArtworkFileUrl);
        var hasText = string.Equals(request.DesignSource, "Text", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.Text);

        if (hasMotif && !AllowedMotifs.Contains(request.MotifKey!, StringComparer.OrdinalIgnoreCase))
            errors.Add("طرح آماده انتخاب‌شده معتبر نیست.");

        if (!hasMotif && !hasUpload && !hasText)
            errors.Add("برای گلدوزی باید طرح آماده، فایل آپلودی یا متن وارد شود.");

        if (request.ScalePercent is < 35 or > 210)
            errors.Add("اندازه طرح باید بین ۳۵٪ تا ۲۱۰٪ باشد.");

        if (request.RotationDegrees is < -180 or > 180)
            errors.Add("چرخش طرح باید بین منفی ۱۸۰ تا مثبت ۱۸۰ درجه باشد.");

        if (request.OpacityPercent is < 35 or > 100)
            errors.Add("شفافیت طرح باید بین ۳۵٪ تا ۱۰۰٪ باشد.");

        var colorsPrice = Math.Max(0, request.ThreadColorCount - 1) * policy.PerThreadColorPrice;
        var areaPrice = Math.Ceiling(request.WidthCm * request.HeightCm) * policy.PerSquareCentimeterPrice;
        var total = policy.BasePrice + colorsPrice + areaPrice;
        var placementLabel = Enum.TryParse<EmbroideryPlacement>(request.Placement, true, out var parsedPlacement)
            ? EmbroideryLabel(parsedPlacement)
            : request.Placement;

        return new EmbroideryQuoteDto(policy.BasePrice, colorsPrice, areaPrice, total, placementLabel, errors.Count == 0, errors);
    }

    public EmbroideryConfiguration CreateConfiguration(Product product, EmbroideryCustomizationRequest request)
    {
        var quote = Quote(product, request);
        if (!quote.IsValid) throw new ArgumentException(string.Join(" ", quote.Errors));
        _ = Enum.TryParse<EmbroideryPlacement>(request.Placement, true, out var placement);
        var configuration = new EmbroideryConfiguration(
            Guid.NewGuid(),
            placement,
            request.WidthCm,
            request.HeightCm,
            request.ThreadColorCount,
            request.ThreadColorHexes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            request.ArtworkFileUrl,
            request.ArtworkFileName,
            request.Text,
            request.FontName,
            request.Note,
            quote.TotalEmbroideryPrice,
            request.GarmentType,
            request.GarmentSize,
            request.GarmentColorHex,
            request.DesignSource,
            request.MotifKey,
            request.PositionX,
            request.PositionY,
            request.ScalePercent,
            request.RotationDegrees,
            request.OpacityPercent);

        product.EmbroideryPolicy.Validate(configuration);
        return configuration;
    }

    public static string EmbroideryLabel(EmbroideryPlacement placement) => placement switch
    {
        EmbroideryPlacement.LeftChest => "سینه چپ",
        EmbroideryPlacement.CenterChest => "وسط سینه",
        EmbroideryPlacement.RightChest => "سینه راست",
        EmbroideryPlacement.BackNeck => "پشت یقه",
        EmbroideryPlacement.LeftSleeve => "آستین چپ",
        EmbroideryPlacement.RightSleeve => "آستین راست",
        _ => placement.ToString()
    };

    private static readonly string[] AllowedGarments = ["TShirt", "Hoodie", "Sweatshirt", "Crewneck"];
    private static readonly string[] AllowedDesignSources = ["Motif", "Upload", "Text"];
    private static readonly string[] AllowedMotifs = ["dragon", "sword", "cloud", "custom"];

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
