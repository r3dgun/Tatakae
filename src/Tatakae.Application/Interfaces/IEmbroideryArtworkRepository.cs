using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Interfaces;

public interface IEmbroideryArtworkRepository
{
    Task<ResultDto<EmbroideryArtworkDto>> SubmitAsync(Guid? customerId, SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> GetForAdminAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ResultDto<EmbroideryArtworkDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<EmbroideryArtworkDto>> ModerateAsync(Guid id, EmbroideryArtworkStatus status, string? adminNote, string? rejectionReason, string? previewImageUrl, string? productionFileExtension, CancellationToken cancellationToken = default);
}
