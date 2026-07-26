using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Account;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(IIdentityAuthService identity, IAccountService accounts) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("AuthLimit")]
    public async Task<IActionResult> Register(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await identity.RegisterAsync(request, RequestMetadata(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLimit")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await identity.LoginAsync(request, RequestMetadata(), cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await identity.CurrentAsync(CurrentSession(), cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await identity.LogoutAsync(CurrentSession(), cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }

    [Authorize]
    [HttpGet("orders")]
    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
    {
        var mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(mobile))
            return new Tatakae.Application.Contracts.Common.ResultDto().Unauthorized("هویت کاربر معتبر نیست.", "account_identity_invalid").ToActionResult(this);

        return (await accounts.OrdersAsync(mobile, cancellationToken)).ToActionResult(this);
    }

    [Authorize]
    [HttpGet("orders/{id:guid}/tracking")]
    public async Task<IActionResult> OrderTracking(Guid id, CancellationToken cancellationToken)
    {
        var mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(mobile))
            return new Tatakae.Application.Contracts.Common.ResultDto().Unauthorized("هویت کاربر معتبر نیست.", "account_identity_invalid").ToActionResult(this);

        return (await accounts.OrderTrackingAsync(mobile, id, cancellationToken)).ToActionResult(this);
    }

    [Authorize]
    [HttpGet("addresses")]
    public async Task<IActionResult> Addresses(CancellationToken cancellationToken)
    {
        var mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(mobile))
            return new Tatakae.Application.Contracts.Common.ResultDto().Unauthorized("هویت کاربر معتبر نیست.", "account_identity_invalid").ToActionResult(this);

        return (await accounts.AddressesAsync(mobile, cancellationToken)).ToActionResult(this);
    }

    [Authorize]
    [HttpPost("addresses")]
    public async Task<IActionResult> CreateAddress(CustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(mobile))
            return new Tatakae.Application.Contracts.Common.ResultDto().Unauthorized("هویت کاربر معتبر نیست.", "account_identity_invalid").ToActionResult(this);

        return (await accounts.UpsertAddressAsync(mobile, null, request, cancellationToken)).ToActionResult(this);
    }

    [Authorize]
    [HttpPut("addresses/{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, CustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(mobile))
            return new Tatakae.Application.Contracts.Common.ResultDto().Unauthorized("هویت کاربر معتبر نیست.", "account_identity_invalid").ToActionResult(this);

        return (await accounts.UpsertAddressAsync(mobile, id, request, cancellationToken)).ToActionResult(this);
    }

    [Authorize]
    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        var mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(mobile))
            return new Tatakae.Application.Contracts.Common.ResultDto().Unauthorized("هویت کاربر معتبر نیست.", "account_identity_invalid").ToActionResult(this);

        return (await accounts.DeleteAddressAsync(mobile, id, cancellationToken)).ToActionResult(this, noContentOnSuccess: true);
    }

    [Authorize]
    [HttpGet("profile/{mobile}")]
    public async Task<IActionResult> Profile(string mobile, CancellationToken cancellationToken)
    {
        return (await accounts.ProfileAsync(mobile, cancellationToken)).ToActionResult(this);
    }

    private ClientRequestMetadata RequestMetadata()
        => new(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString());

    private AuthenticatedSessionContext CurrentSession()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        _ = Guid.TryParse(rawUserId, out var userId);

        var sessionKey = User.FindFirstValue("sid")
            ?? User.FindFirstValue("session_id");

        return new AuthenticatedSessionContext(userId, sessionKey);
    }
}
