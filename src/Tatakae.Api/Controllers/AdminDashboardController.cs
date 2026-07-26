using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[PermissionChecker(PermissionIds.AdminDashboardView)]
public sealed class AdminDashboardController(IAdminDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await dashboard.GetAsync(cancellationToken);
        return result.ToActionResult(this);
    }
}
