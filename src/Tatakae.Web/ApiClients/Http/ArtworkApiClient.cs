using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;
using Tatakae.Web.State;

namespace Tatakae.Web.ApiClients.Http;

public sealed class ArtworkApiClient(IApiClientTransport transport, IAuthSessionStore auth) : IArtworkApiClient
{
    public Task<ResultDto<EmbroideryArtworkPolicyDto>> PolicyAsync(CancellationToken cancellationToken = default)
        => transport.GetResultAsync<EmbroideryArtworkPolicyDto>("api/account/artworks/policy", "دریافت قوانین فایل طرح ناموفق بود.", cancellationToken);

    public async Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> MineAsync(CancellationToken cancellationToken = default)
    {
        await auth.EnsureLoadedAsync();
        if (!auth.IsSignedIn)
            return new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>()
                .Unauthorized("برای مشاهده طرح‌ها ابتدا وارد حساب شوید.", "authentication_required");

        return await transport.GetResultAsync<IReadOnlyCollection<EmbroideryArtworkDto>>(
            "api/account/artworks",
            "دریافت طرح‌ها ناموفق بود.",
            cancellationToken);
    }

    public Task<ResultDto<EmbroideryArtworkDto>> SubmitAsync(SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default)
        => transport.SendResultAsync<EmbroideryArtworkDto>(HttpMethod.Post, "api/account/artworks", request, "ثبت طرح ناموفق بود.", cancellationToken);
}
