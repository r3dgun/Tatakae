using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Embroidery;

public sealed class SubmitEmbroideryArtworkRequest : IValidatableObject
{
    [Required]
    public Guid MediaAssetId { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? OrderLineId { get; set; }

    [Range(typeof(decimal), "1", "60")]
    public decimal? WidthCm { get; set; }

    [Range(typeof(decimal), "1", "60")]
    public decimal? HeightCm { get; set; }

    [Range(1, 24)]
    public int? ThreadColorCount { get; set; }

    [StringLength(1200, ErrorMessage = "یادداشت مشتری حداکثر ۱۲۰۰ کاراکتر است.")]
    public string? CustomerNote { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MediaAssetId == Guid.Empty)
        {
            yield return new ValidationResult("فایل طرح را انتخاب و آپلود کنید.", [nameof(MediaAssetId)]);
        }

        if (WidthCm.HasValue != HeightCm.HasValue)
        {
            yield return new ValidationResult("عرض و ارتفاع طرح را با هم وارد کنید.", [nameof(WidthCm), nameof(HeightCm)]);
        }
    }
}

public sealed class AdminArtworkModerationRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^(Approved|Rejected|NeedsRevision|Archived|PendingReview)$")]
    public string Status { get; set; } = "Approved";

    [StringLength(1200)]
    public string? AdminNote { get; set; }

    [StringLength(1200)]
    public string? RejectionReason { get; set; }

    [Url]
    [StringLength(1200)]
    public string? PreviewImageUrl { get; set; }

    [StringLength(20, ErrorMessage = "فرمت تولید حداکثر ۲۰ کاراکتر است.")]
    [RegularExpression("^[A-Za-z0-9.]*$", ErrorMessage = "فرمت تولید فقط شامل حروف انگلیسی و عدد است.")]
    public string? ProductionFileExtension { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((Status == "Rejected" || Status == "NeedsRevision") && string.IsNullOrWhiteSpace(RejectionReason))
        {
            yield return new ValidationResult("برای رد یا درخواست اصلاح، دلیل را وارد کنید.", [nameof(RejectionReason)]);
        }
    }
}

public sealed record EmbroideryArtworkDto(
    Guid Id,
    Guid MediaAssetId,
    Guid? CustomerId,
    Guid? ProductId,
    Guid? OrderId,
    Guid? OrderLineId,
    string OriginalFileName,
    string ContentType,
    string FileUrl,
    long SizeBytes,
    string Status,
    string StatusLabel,
    decimal? WidthCm,
    decimal? HeightCm,
    int? ThreadColorCount,
    string? CustomerNote,
    string? AdminNote,
    string? RejectionReason,
    string? PreviewImageUrl,
    string? ProductionFileExtension,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt);

public sealed record EmbroideryArtworkPolicyDto(
    long MaxSizeBytes,
    IReadOnlyCollection<string> AllowedContentTypes,
    IReadOnlyCollection<string> AllowedExtensions,
    IReadOnlyCollection<string> ProductionExtensions,
    int MaxArtworkFilesPerOrder,
    decimal MaxWidthCm,
    decimal MaxHeightCm,
    int MaxThreadColors);
