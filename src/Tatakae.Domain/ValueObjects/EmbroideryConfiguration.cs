using Tatakae.Domain.Common;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Entities;

public sealed record EmbroideryConfiguration
{
    public EmbroideryConfiguration(
        Guid Id,
        EmbroideryPlacement Placement,
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
        string GarmentType = "TShirt",
        string GarmentSize = "L",
        string GarmentColorHex = "#111827",
        string DesignSource = "Motif",
        string? MotifKey = "dragon",
        int PositionX = 0,
        int PositionY = 0,
        int ScalePercent = 100,
        int RotationDegrees = 0,
        int OpacityPercent = 100)
    {
        this.Id = DomainGuard.NotEmpty(Id, nameof(Id), "شناسه تنظیمات گلدوزی معتبر نیست.");
        this.Placement = Placement;
        this.WidthCm = DomainGuard.NonNegative(WidthCm, nameof(WidthCm), "عرض گلدوزی نمی‌تواند منفی باشد.");
        this.HeightCm = DomainGuard.NonNegative(HeightCm, nameof(HeightCm), "ارتفاع گلدوزی نمی‌تواند منفی باشد.");
        this.ThreadColorCount = DomainGuard.NonNegative(ThreadColorCount, nameof(ThreadColorCount), "تعداد رنگ نخ نمی‌تواند منفی باشد.");
        this.ThreadColorHexes = (ThreadColorHexes ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (this.ThreadColorCount != this.ThreadColorHexes.Count)
            throw new ArgumentException("تعداد رنگ نخ با فهرست رنگ‌های انتخاب‌شده سازگار نیست.", nameof(ThreadColorCount));

        this.ArtworkFileUrl = DomainGuard.Optional(ArtworkFileUrl);
        this.ArtworkFileName = DomainGuard.Optional(ArtworkFileName);
        this.Text = DomainGuard.Optional(Text);
        this.FontName = DomainGuard.Optional(FontName);
        this.Note = DomainGuard.Optional(Note);
        this.CalculatedPrice = DomainGuard.NonNegative(CalculatedPrice, nameof(CalculatedPrice), "قیمت گلدوزی نمی‌تواند منفی باشد.");
        this.GarmentType = DomainGuard.Required(GarmentType, nameof(GarmentType), "نوع لباس الزامی است.");
        this.GarmentSize = DomainGuard.Required(GarmentSize, nameof(GarmentSize), "سایز لباس الزامی است.");
        this.GarmentColorHex = DomainGuard.Required(GarmentColorHex, nameof(GarmentColorHex), "رنگ لباس الزامی است.");
        this.DesignSource = DomainGuard.Required(DesignSource, nameof(DesignSource), "منبع طرح الزامی است.");
        this.MotifKey = DomainGuard.Optional(MotifKey);
        this.PositionX = PositionX;
        this.PositionY = PositionY;
        DomainGuard.InRange(ScalePercent, 1, 500, nameof(ScalePercent), "مقیاس طرح باید بین ۱ تا ۵۰۰ درصد باشد.");
        this.ScalePercent = ScalePercent;
        if (RotationDegrees < -360 || RotationDegrees > 360)
            throw new ArgumentOutOfRangeException(nameof(RotationDegrees), RotationDegrees, "چرخش طرح باید بین منفی ۳۶۰ تا ۳۶۰ درجه باشد.");
        this.RotationDegrees = RotationDegrees;
        DomainGuard.InRange(OpacityPercent, 0, 100, nameof(OpacityPercent), "شفافیت طرح باید بین صفر تا ۱۰۰ باشد.");
        this.OpacityPercent = OpacityPercent;

        if (!HasArtwork && !HasText && !HasMotif && !string.Equals(DesignSource, "ReadyMade", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("حداقل یک طرح، متن یا موتیف برای گلدوزی لازم است.", nameof(DesignSource));
    }

    public Guid Id { get; }
    public EmbroideryPlacement Placement { get; }
    public decimal WidthCm { get; }
    public decimal HeightCm { get; }
    public int ThreadColorCount { get; }
    public IReadOnlyCollection<string> ThreadColorHexes { get; }
    public string? ArtworkFileUrl { get; }
    public string? ArtworkFileName { get; }
    public string? Text { get; }
    public string? FontName { get; }
    public string? Note { get; }
    public decimal CalculatedPrice { get; }
    public string GarmentType { get; }
    public string GarmentSize { get; }
    public string GarmentColorHex { get; }
    public string DesignSource { get; }
    public string? MotifKey { get; }
    public int PositionX { get; }
    public int PositionY { get; }
    public int ScalePercent { get; }
    public int RotationDegrees { get; }
    public int OpacityPercent { get; }

    public bool HasArtwork => !string.IsNullOrWhiteSpace(ArtworkFileUrl);
    public bool HasText => !string.IsNullOrWhiteSpace(Text);
    public bool HasMotif => string.Equals(DesignSource, "Motif", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(MotifKey);
}
