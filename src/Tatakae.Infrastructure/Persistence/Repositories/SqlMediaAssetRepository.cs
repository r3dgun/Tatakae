using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlMediaAssetRepository(
    TatakaeDbContext db,
    ILogger<SqlMediaAssetRepository>? logger = null) : IMediaAssetRepository
{
    private readonly ILogger<SqlMediaAssetRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlMediaAssetRepository>.Instance;

    private async Task<IReadOnlyCollection<FileUploadDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.MediaAssets.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => new FileUploadDto(x.Id, x.FileName, x.ContentType, x.SizeBytes, x.Url, x.UsageType.ToString(), x.CreatedAt)).ToArrayAsync(cancellationToken);

    private async Task<FileUploadDto> AddAsync(CreateStoredFileRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new MediaAssetDbRecord
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = request.OwnerEntityId,
            UsageType = Enum.TryParse<MediaUsageType>(request.Purpose, true, out var usage) ? usage : MediaUsageType.EmbroideryArtwork,
            FileName = request.FileName,
            ContentType = request.ContentType,
            Url = request.Url,
            AltText = request.AltText,
            SizeBytes = request.SizeBytes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.MediaAssets.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new FileUploadDto(entity.Id, entity.FileName, entity.ContentType, entity.SizeBytes, entity.Url, entity.UsageType.ToString(), entity.CreatedAt);
    }

    private async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("فایل پیدا نشد.");
        db.SoftDelete(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
