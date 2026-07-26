using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class MediaAssetService(
    IMediaAssetRepository media,
    ILogger<MediaAssetService>? logger = null) : IMediaAssetService
{
    private readonly ILogger<MediaAssetService> _logger = logger ?? NullLogger<MediaAssetService>.Instance;
    public UploadPolicyDto Policy { get; } = new(
        15_000_000,
        ["image/png", "image/jpeg", "image/webp", "image/svg+xml", "application/pdf", "application/octet-stream", "application/x-dst", "application/x-pes"],
        [".png", ".jpg", ".jpeg", ".webp", ".svg", ".pdf", ".dst", ".pes"],
        5);

    public async Task<IReadOnlyCollection<FileUploadDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await media.GetAllAsync(cancellationToken)).RequireData();

    public async Task<FileUploadDto> AddStoredFileAsync(CreateStoredFileRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SizeBytes <= 0 || request.SizeBytes > Policy.MaxSizeBytes) throw new ArgumentException("حجم فایل مجاز نیست.");
        if (!Policy.AllowedContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("نوع فایل مجاز نیست.");
        return (await media.AddAsync(request, cancellationToken)).RequireData();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => (await media.DeleteAsync(id, cancellationToken)).EnsureSuccess();
}
