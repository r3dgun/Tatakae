using System.Text.Json;
using Microsoft.JSInterop;
using Tatakae.Application.Contracts.Account;

namespace Tatakae.Web.State;

public sealed class BrowserAuthSessionStore(IJSRuntime js) : IAuthSessionStore
{
    private const string StorageKey = "tatakae.identity.session.v1";
    private bool loaded;

    public AccountSessionDto? Current { get; private set; }
    public bool IsSignedIn => Current is not null && Current.ExpiresAt > DateTimeOffset.UtcNow;
    public string? Token => IsSignedIn ? Current?.Token : null;
    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (loaded) return;
        loaded = true;

        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var session = JsonSerializer.Deserialize<AccountSessionDto>(json);
                if (session is not null && session.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    Current = session;
                }
                else
                {
                    await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                }
            }
        }
        catch
        {
            Current = null;
        }
    }

    public async Task SignInAsync(AccountSessionDto session)
    {
        loaded = true;
        Current = session;
        var json = JsonSerializer.Serialize(session);
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        loaded = true;
        Current = null;
        try { await js.InvokeVoidAsync("localStorage.removeItem", StorageKey); }
        catch { /* ignored: sign out should still clear memory */ }
        Changed?.Invoke();
    }

}
