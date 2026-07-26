using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Enums;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class EmbroideryArtworkService(
    IEmbroideryArtworkRepository artworks, ICustomerRepository customers,
    ILogger<EmbroideryArtworkService>? logger = null) : IEmbroideryArtworkService
{
    private readonly ILogger<EmbroideryArtworkService> _logger = logger ?? NullLogger<EmbroideryArtworkService>.Instance;
    private static readonly string[] AllowedContentTypes =
    [
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/svg+xml",
        "application/pdf",
        "application/octet-stream",
        "application/x-dst",
        "application/x-pes"
    ];

    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".svg", ".pdf", ".dst", ".pes"];
    private static readonly string[] ProductionExtensions = [".dst", ".pes"];

    public EmbroideryArtworkPolicyDto Policy { get; } = new(
        15_000_000,
        AllowedContentTypes,
        AllowedExtensions,
        ProductionExtensions,
        5,
        40m,
        40m,
        12);

    public async Task<EmbroideryArtworkDto?> SubmitAsync(string? mobile, SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var customer = string.IsNullOrWhiteSpace(mobile) ? null : (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        return (await artworks.SubmitAsync(customer?.Id, request, cancellationToken)).DataOrDefault();
    }

    public async Task<IReadOnlyCollection<EmbroideryArtworkDto>> GetMineAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).RequireData();
        return (await artworks.GetForCustomerAsync(customer.Id, cancellationToken)).RequireData();
    }

    public async Task<IReadOnlyCollection<EmbroideryArtworkDto>> AdminListAsync(string? status = null, CancellationToken cancellationToken = default)
        => (await artworks.GetForAdminAsync(status, cancellationToken)).RequireData();

    public async Task<EmbroideryArtworkDto> AdminModerateAsync(Guid id, AdminArtworkModerationRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<EmbroideryArtworkStatus>(request.Status, true, out var status))
            throw new ArgumentException("وضعیت طرح معتبر نیست.");

        if ((status == EmbroideryArtworkStatus.Rejected || status == EmbroideryArtworkStatus.NeedsRevision) && string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new ArgumentException("برای رد یا نیاز به اصلاح، دلیل باید ثبت شود.");

        var result = await artworks.ModerateAsync(id, status, request.AdminNote, request.RejectionReason, request.PreviewImageUrl, request.ProductionFileExtension, cancellationToken);
        return result.RequireData();
    }

    private void ValidateRequest(SubmitEmbroideryArtworkRequest request)
    {
        if (request.MediaAssetId == Guid.Empty) throw new ArgumentException("فایل طرح انتخاب نشده است.");
        if (request.WidthCm is < 1 or > 40) throw new ArgumentException("عرض طرح باید بین ۱ تا ۴۰ سانتی‌متر باشد.");
        if (request.HeightCm is < 1 or > 40) throw new ArgumentException("ارتفاع طرح باید بین ۱ تا ۴۰ سانتی‌متر باشد.");
        if (request.ThreadColorCount is < 1 or > 12) throw new ArgumentException("تعداد رنگ نخ باید بین ۱ تا ۱۲ باشد.");
    }

    public static string StatusLabel(string status) => status switch
    {
        "PendingReview" => "در انتظار بررسی",
        "Approved" => "تأیید شده",
        "Rejected" => "رد شده",
        "NeedsRevision" => "نیازمند اصلاح",
        "Archived" => "آرشیو شده",
        _ => status
    };
}
