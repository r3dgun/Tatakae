using Tatakae.Domain.Common;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Entities;

/// <summary>Embroidery constraints owned by a product.</summary>
public sealed record EmbroideryPolicy
{
    public EmbroideryPolicy(
        decimal BasePrice,
        decimal PerThreadColorPrice,
        decimal PerSquareCentimeterPrice,
        int MaxThreadColors,
        decimal MaxWidthCm,
        decimal MaxHeightCm,
        IReadOnlyCollection<EmbroideryPlacement> AllowedPlacements,
        IReadOnlyCollection<string> AllowedThreadColors,
        bool AllowArtworkUpload = true,
        bool AllowTextEmbroidery = true)
    {
        this.BasePrice = DomainGuard.NonNegative(BasePrice, nameof(BasePrice), "قیمت پایه گلدوزی نمی‌تواند منفی باشد.");
        this.PerThreadColorPrice = DomainGuard.NonNegative(PerThreadColorPrice, nameof(PerThreadColorPrice), "هزینه رنگ نخ نمی‌تواند منفی باشد.");
        this.PerSquareCentimeterPrice = DomainGuard.NonNegative(PerSquareCentimeterPrice, nameof(PerSquareCentimeterPrice), "هزینه سطح گلدوزی نمی‌تواند منفی باشد.");
        this.MaxThreadColors = DomainGuard.Positive(MaxThreadColors, nameof(MaxThreadColors), "حداکثر تعداد رنگ نخ باید بیشتر از صفر باشد.");
        this.MaxWidthCm = DomainGuard.Positive(MaxWidthCm, nameof(MaxWidthCm), "حداکثر عرض گلدوزی باید بیشتر از صفر باشد.");
        this.MaxHeightCm = DomainGuard.Positive(MaxHeightCm, nameof(MaxHeightCm), "حداکثر ارتفاع گلدوزی باید بیشتر از صفر باشد.");
        this.AllowedPlacements = DomainGuard.NotEmpty(AllowedPlacements, nameof(AllowedPlacements), "حداقل یک محل گلدوزی باید مجاز باشد.")
            .Distinct()
            .ToArray();
        this.AllowedThreadColors = DomainGuard.NotEmpty(AllowedThreadColors, nameof(AllowedThreadColors), "حداقل یک رنگ نخ باید مجاز باشد.")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        this.AllowArtworkUpload = AllowArtworkUpload;
        this.AllowTextEmbroidery = AllowTextEmbroidery;
    }

    public decimal BasePrice { get; }
    public decimal PerThreadColorPrice { get; }
    public decimal PerSquareCentimeterPrice { get; }
    public int MaxThreadColors { get; }
    public decimal MaxWidthCm { get; }
    public decimal MaxHeightCm { get; }
    public IReadOnlyCollection<EmbroideryPlacement> AllowedPlacements { get; }
    public IReadOnlyCollection<string> AllowedThreadColors { get; }
    public bool AllowArtworkUpload { get; }
    public bool AllowTextEmbroidery { get; }

    public void Validate(EmbroideryConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!AllowedPlacements.Contains(configuration.Placement))
            throw new InvalidOperationException("محل انتخاب‌شده برای گلدوزی این محصول مجاز نیست.");
        if (configuration.WidthCm > MaxWidthCm || configuration.HeightCm > MaxHeightCm)
            throw new InvalidOperationException("ابعاد گلدوزی از محدوده مجاز محصول بیشتر است.");
        if (configuration.ThreadColorCount > MaxThreadColors)
            throw new InvalidOperationException("تعداد رنگ نخ از سقف مجاز محصول بیشتر است.");
        if (configuration.ThreadColorHexes.Any(x => !AllowedThreadColors.Contains(x, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("یکی از رنگ‌های نخ انتخاب‌شده برای محصول مجاز نیست.");
        if (configuration.HasArtwork && !AllowArtworkUpload)
            throw new InvalidOperationException("آپلود طرح برای این محصول مجاز نیست.");
        if (configuration.HasText && !AllowTextEmbroidery)
            throw new InvalidOperationException("گلدوزی متن برای این محصول مجاز نیست.");
    }
}
