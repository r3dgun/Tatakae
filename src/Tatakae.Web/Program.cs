using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tatakae.Application.Security;
using Tatakae.Web;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Http;
using Tatakae.Web.ApiClients.Results;
using Tatakae.Web.Authentication;
using Tatakae.Web.Authorization;
using Tatakae.Web.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7075/";

builder.Services.AddScoped<IAuthSessionStore, BrowserAuthSessionStore>();
builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddScoped<HttpClient>(CreateApiHttpClient);

HttpClient CreateApiHttpClient(IServiceProvider services)
{
    var handler = services.GetRequiredService<BearerTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
}

builder.Services.AddScoped<IApiResultReader, ApiResultReader>();
builder.Services.AddScoped<IApiClientTransport, ApiClientTransport>();

builder.Services.AddScoped<IStoreApiClient, StoreApiClient>();
builder.Services.AddScoped<ICheckoutApiClient, CheckoutApiClient>();
builder.Services.AddScoped<IPaymentApiClient, PaymentApiClient>();
builder.Services.AddScoped<ICartApiClient, CartApiClient>();
builder.Services.AddScoped<IAdminApiClient, AdminApiClient>();
builder.Services.AddScoped<IFileUploadApiClient, FileUploadApiClient>();
builder.Services.AddScoped<IAccountApiClient, AccountApiClient>();
builder.Services.AddScoped<IWishlistApiClient, WishlistApiClient>();
builder.Services.AddScoped<IArtworkApiClient, ArtworkApiClient>();

builder.Services.AddScoped<ICartState, BrowserCartState>();
builder.Services.AddScoped<IUiPermissionEvaluator, ClaimsUiPermissionEvaluator>();
builder.Services.AddScoped<AuthenticationStateProvider, TatakaeAuthenticationStateProvider>();

builder.Services.AddAuthorizationCore(options =>
{
    foreach (var permission in PermissionNames.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAuthenticatedUser()
                .RequireClaim(PermissionClaimTypes.Permission, permission));
    }
});

await builder.Build().RunAsync();
