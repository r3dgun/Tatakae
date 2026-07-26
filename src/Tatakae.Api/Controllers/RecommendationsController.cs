using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController(IWishlistService wishlist) : ControllerBase
{
    [HttpGet("similar/{slug}")]
    public async Task<IActionResult> Similar(string slug, [FromQuery] int take = 6, CancellationToken cancellationToken = default)
    {
        var result = await wishlist.SimilarAsync(slug, take, cancellationToken);
        return result.ToActionResult(this);
    }
}
