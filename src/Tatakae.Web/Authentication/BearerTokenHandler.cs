using System.Net.Http.Headers;
using Tatakae.Web.State;

namespace Tatakae.Web.Authentication;

public sealed class BearerTokenHandler(IAuthSessionStore sessions) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await sessions.EnsureLoadedAsync();
        if (request.Headers.Authorization is null && !string.IsNullOrWhiteSpace(sessions.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessions.Token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
