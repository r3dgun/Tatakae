using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Files;

public sealed class CreateFileUploadRequest
{
    [Required, StringLength(180)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [RegularExpression("^(image/png|image/jpeg|image/webp|image/svg\\+xml|application/pdf|application/octet-stream|application/x-dst|application/x-pes)$")]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, 15_000_000)]
    public long SizeBytes { get; set; }

    [Required, RegularExpression("^(EmbroideryArtwork|ProductImage|ProductGallery|Banner|Avatar|ReviewImage|InvoiceAttachment)$")]
    public string Purpose { get; set; } = "EmbroideryArtwork";
}


public sealed class CreateStoredFileRequest
{
    public Guid? OwnerEntityId { get; set; }
    public string Purpose { get; set; } = "Artwork";
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public long SizeBytes { get; set; }
}

public sealed record FileUploadDto(Guid Id, string FileName, string ContentType, long SizeBytes, string Url, string Purpose, DateTimeOffset CreatedAt);

public sealed record UploadPolicyDto(
    long MaxSizeBytes,
    IReadOnlyCollection<string> AllowedContentTypes,
    IReadOnlyCollection<string> AllowedExtensions,
    int MaxArtworkFilesPerOrder);
