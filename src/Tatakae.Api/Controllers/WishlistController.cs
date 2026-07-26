using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account/wishlist")]
public sealed class WishlistController(IWishlistService wishlist) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var mobile = ResolveMobile();
        if (string.IsNullOrWhiteSpace(mobile)) return Unauthorized(new ResultDto().Unauthorized("اطلاعات هویتی کاربر پیدا نشد.", "authenticated_user_required"));
        var result = await wishlist.GetAsync(mobile, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{productId:guid}/status")]
    public async Task<IActionResult> Status(Guid productId, CancellationToken cancellationToken)
    {
        var mobile = ResolveMobile();
        if (string.IsNullOrWhiteSpace(mobile)) return Unauthorized(new ResultDto().Unauthorized("اطلاعات هویتی کاربر پیدا نشد.", "authenticated_user_required"));
        var result = await wishlist.IsWishlistedAsync(mobile, productId, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Ok(new { productId, isWishlisted = result.Data });
    }

    [HttpPost("{productId:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid productId, CancellationToken cancellationToken)
    {
        var mobile = ResolveMobile();
        if (string.IsNullOrWhiteSpace(mobile)) return Unauthorized(new ResultDto().Unauthorized("اطلاعات هویتی کاربر پیدا نشد.", "authenticated_user_required"));
        var result = await wishlist.ToggleAsync(mobile, productId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Remove(Guid productId, CancellationToken cancellationToken)
    {
        var mobile = ResolveMobile();
        if (string.IsNullOrWhiteSpace(mobile)) return Unauthorized(new ResultDto().Unauthorized("اطلاعات هویتی کاربر پیدا نشد.", "authenticated_user_required"));
        var result = await wishlist.RemoveAsync(mobile, productId, cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> Recommendations([FromQuery] RecommendationQuery query, CancellationToken cancellationToken)
    {
        var mobile = ResolveMobile();
        if (string.IsNullOrWhiteSpace(mobile)) return Unauthorized(new ResultDto().Unauthorized("اطلاعات هویتی کاربر پیدا نشد.", "authenticated_user_required"));
        var result = await wishlist.RecommendationsAsync(mobile, query, cancellationToken);
        return result.ToActionResult(this);
    }

    private string? ResolveMobile() => User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
}
