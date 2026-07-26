using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[PermissionChecker(PermissionIds.AdminProductsView)]
public sealed class AdminInventoryController(IInventoryService inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await inventory.GetInventoryAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPatch("{variantId:guid}/adjust")]
    [PermissionChecker(PermissionIds.AdminProductsManage)]
    public async Task<IActionResult> Adjust(Guid variantId, [FromBody] InventoryAdjustmentRequest request, CancellationToken cancellationToken)
    {
        request.VariantId = variantId;
        var result = await inventory.AdjustAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
