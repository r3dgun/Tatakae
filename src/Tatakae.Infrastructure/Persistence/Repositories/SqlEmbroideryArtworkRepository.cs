using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlEmbroideryArtworkRepository(
    TatakaeDbContext db,
    ILogger<SqlEmbroideryArtworkRepository>? logger = null) : IEmbroideryArtworkRepository
{
    private readonly ILogger<SqlEmbroideryArtworkRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlEmbroideryArtworkRepository>.Instance;

    private async Task<EmbroideryArtworkDto?> SubmitAsync(Guid? customerId, SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default)
    {
        var media = await db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.MediaAssetId, cancellationToken)
            ?? throw new KeyNotFoundException("فایل آپلود شده پیدا نشد.");

        var existing = await db.EmbroideryArtworks.FirstOrDefaultAsync(x => x.MediaAssetId == request.MediaAssetId, cancellationToken);
        if (existing is not null)
        {
            existing.CustomerId = customerId ?? existing.CustomerId;
            existing.ProductId = request.ProductId ?? existing.ProductId;
            existing.OrderId = request.OrderId ?? existing.OrderId;
            existing.OrderLineId = request.OrderLineId ?? existing.OrderLineId;
            existing.WidthCm = request.WidthCm;
            existing.HeightCm = request.HeightCm;
            existing.ThreadColorCount = request.ThreadColorCount;
            existing.CustomerNote = request.CustomerNote;
            existing.Status = EmbroideryArtworkStatus.PendingReview;
            existing.RejectionReason = null;
            existing.AdminNote = null;
            existing.ReviewedAt = null;
            await db.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(existing.Id, cancellationToken);
        }

        var entity = new EmbroideryArtworkDbRecord
        {
            Id = Guid.NewGuid(),
            MediaAssetId = media.Id,
            CustomerId = customerId,
            ProductId = request.ProductId,
            OrderId = request.OrderId,
            OrderLineId = request.OrderLineId,
            OriginalFileName = media.FileName,
            ContentType = media.ContentType,
            FileUrl = media.Url,
            SizeBytes = media.SizeBytes,
            Status = EmbroideryArtworkStatus.PendingReview,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            ThreadColorCount = request.ThreadColorCount,
            CustomerNote = request.CustomerNote,
            SubmittedAt = DateTimeOffset.UtcNow
        };
        db.EmbroideryArtworks.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task<IReadOnlyCollection<EmbroideryArtworkDto>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await db.EmbroideryArtworks.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => ToDto(x))
            .ToArrayAsync(cancellationToken);

    private async Task<IReadOnlyCollection<EmbroideryArtworkDto>> GetForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = db.EmbroideryArtworks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmbroideryArtworkStatus>(status, true, out var parsed))
        {
            query = query.Where(x => x.Status == parsed);
        }

        return await query.OrderByDescending(x => x.SubmittedAt).Select(x => ToDto(x)).ToArrayAsync(cancellationToken);
    }

    private async Task<EmbroideryArtworkDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.EmbroideryArtworks.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<EmbroideryArtworkDto?> ModerateAsync(Guid id, EmbroideryArtworkStatus status, string? adminNote, string? rejectionReason, string? previewImageUrl, string? productionFileExtension, CancellationToken cancellationToken = default)
    {
        var entity = await db.EmbroideryArtworks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        entity.Status = status;
        entity.AdminNote = adminNote;
        entity.RejectionReason = rejectionReason;
        entity.PreviewImageUrl = previewImageUrl;
        entity.ProductionFileExtension = productionFileExtension;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static EmbroideryArtworkDto ToDto(EmbroideryArtworkDbRecord x)
        => new(
            x.Id,
            x.MediaAssetId,
            x.CustomerId,
            x.ProductId,
            x.OrderId,
            x.OrderLineId,
            x.OriginalFileName,
            x.ContentType,
            x.FileUrl,
            x.SizeBytes,
            x.Status.ToString(),
            StatusLabel(x.Status),
            x.WidthCm,
            x.HeightCm,
            x.ThreadColorCount,
            x.CustomerNote,
            x.AdminNote,
            x.RejectionReason,
            x.PreviewImageUrl,
            x.ProductionFileExtension,
            x.SubmittedAt,
            x.ReviewedAt);

    private static string StatusLabel(EmbroideryArtworkStatus status) => status switch
    {
        EmbroideryArtworkStatus.PendingReview => "در انتظار بررسی",
        EmbroideryArtworkStatus.Approved => "تأیید شده",
        EmbroideryArtworkStatus.Rejected => "رد شده",
        EmbroideryArtworkStatus.NeedsRevision => "نیازمند اصلاح",
        EmbroideryArtworkStatus.Archived => "آرشیو شده",
        _ => status.ToString()
    };
}
