using Microsoft.AspNetCore.Components.Authorization;
using Tatakae.Application.Security;

namespace Tatakae.Web.Authorization;

public sealed class ClaimsUiPermissionEvaluator(AuthenticationStateProvider authStateProvider) : IUiPermissionEvaluator
{
    public async Task<bool> HasAsync(string permission)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true && state.User.HasClaim(PermissionClaimTypes.Permission, permission);
    }
}
