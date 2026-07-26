using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account/artworks")]
public sealed class ArtworksController(IEmbroideryArtworkService artworks) : ControllerBase
{
    [HttpGet("policy")]
    public IActionResult Policy() => Ok(artworks.Policy);

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var mobile = CurrentMobile();
        if (string.IsNullOrWhiteSpace(mobile)) return Unauthorized(new ResultDto().Unauthorized("اطلاعات هویتی کاربر پیدا نشد.", "authenticated_user_required"));
        var result = await artworks.GetMineAsync(mobile, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken)
    {
        var result = await artworks.SubmitAsync(CurrentMobile(), request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Created($"/api/account/artworks/{result.Data!.Id}", result.Data);
    }

    private string? CurrentMobile() => User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
}
