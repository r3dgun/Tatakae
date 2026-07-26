using Tatakae.Application.Contracts.Common;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Tatakae.Application.Contracts.Files;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface IFileUploadApiClient
{
    Task<ResultDto<UploadPolicyDto>> PolicyAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<FileUploadDto>> UploadAsync(IBrowserFile file, string purpose = "EmbroideryArtwork", CancellationToken cancellationToken = default);
}
