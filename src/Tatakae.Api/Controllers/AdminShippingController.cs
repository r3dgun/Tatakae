using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/shipping-methods")]
[PermissionChecker(PermissionIds.AdminShippingView)]
public sealed class AdminShippingController(IShippingService shipping) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await shipping.GetAdminMethodsAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminShippingManage)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertManualShippingMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await shipping.UpsertAsync(null, request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Created($"/api/admin/shipping-methods/{result.Data!.Id}", result.Data);
    }

    [PermissionChecker(PermissionIds.AdminShippingManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertManualShippingMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await shipping.UpsertAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminShippingManage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await shipping.DeleteAsync(id, cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }
}
