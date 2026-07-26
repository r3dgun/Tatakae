using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/customers")]
[PermissionChecker(PermissionIds.AdminCustomersView)]
public sealed class AdminCustomersController(ICustomerService customers) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await customers.GetAllAsync(cancellationToken);
        return result.ToActionResult(this);
    }
}
