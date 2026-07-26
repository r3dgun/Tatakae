using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/artworks")]
[PermissionChecker(PermissionIds.AdminMediaView)]
public sealed class AdminArtworksController(IEmbroideryArtworkService artworks) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await artworks.AdminListAsync(status, cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminMediaManage)]
    [HttpPatch("{id:guid}/moderate")]
    public async Task<IActionResult> Moderate(Guid id, [FromBody] AdminArtworkModerationRequest request, CancellationToken cancellationToken)
    {
        var result = await artworks.AdminModerateAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }
}
