using Tatakae.Application.Contracts.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Embroidery;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface IArtworkApiClient
{
    Task<ResultDto<EmbroideryArtworkPolicyDto>> PolicyAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> MineAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<EmbroideryArtworkDto>> SubmitAsync(SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default);
}
