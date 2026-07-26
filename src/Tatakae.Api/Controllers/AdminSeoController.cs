using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/seo")]
[PermissionChecker(PermissionIds.AdminSeoView)]
public sealed class AdminSeoController(ISeoService seo, IConfiguration configuration) : ControllerBase
{
    [HttpGet("audit")]
    public async Task<IActionResult> Audit(CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuration["PublicBaseUrl"])
            ? $"{Request.Scheme}://{Request.Host}"
            : configuration["PublicBaseUrl"];
        var result = await seo.AuditAsync(baseUrl, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("routes")]
    public IActionResult Routes()
        => seo.GetRoutePolicies().ToActionResult(this);
}
