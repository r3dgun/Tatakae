using Tatakae.Application.Contracts.Account;

namespace Tatakae.Web.State;

public interface IAuthSessionStore
{
    AccountSessionDto? Current { get; }
    bool IsSignedIn { get; }
    string? Token { get; }
    event Action? Changed;
    Task EnsureLoadedAsync();
    Task SignInAsync(AccountSessionDto session);
    Task SignOutAsync();
}
