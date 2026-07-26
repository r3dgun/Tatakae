using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/media")]
[PermissionChecker(PermissionIds.AdminMediaView)]
public sealed class AdminMediaController(IMediaAssetService media) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await media.GetAllAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminMediaManage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await media.DeleteAsync(id, cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }
}
