using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Files;

namespace Tatakae.Application.Interfaces;

public interface IMediaAssetRepository
{
    Task<ResultDto<IReadOnlyCollection<FileUploadDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<FileUploadDto>> AddAsync(CreateStoredFileRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

