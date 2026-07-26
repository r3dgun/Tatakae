using Tatakae.Web.State;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Tatakae.Application.Security;

namespace Tatakae.Web.Authentication;

public sealed class TatakaeAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private readonly IAuthSessionStore sessions;

    public TatakaeAuthenticationStateProvider(IAuthSessionStore sessions)
    {
        this.sessions = sessions;
        sessions.Changed += NotifyAuthenticationChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await sessions.EnsureLoadedAsync();
        return new AuthenticationState(CreatePrincipal());
    }

    private ClaimsPrincipal CreatePrincipal()
    {
        var session = sessions.Current;
        if (!sessions.IsSignedIn || session is null) return Anonymous;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.CustomerId.ToString()),
            new(ClaimTypes.Name, session.Mobile),
            new(ClaimTypes.GivenName, session.FullName),
            new("mobile", session.Mobile),
            new("session_id", session.SessionKey)
        };

        if (!string.IsNullOrWhiteSpace(session.Email)) claims.Add(new Claim(ClaimTypes.Email, session.Email));
        foreach (var role in session.Roles ?? Array.Empty<string>()) claims.Add(new Claim(ClaimTypes.Role, role));
        foreach (var permission in session.Permissions ?? Array.Empty<string>()) claims.Add(new Claim(PermissionClaimTypes.Permission, permission));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TatakaeIdentityJwt"));
    }

    private void NotifyAuthenticationChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() => sessions.Changed -= NotifyAuthenticationChanged;
}
