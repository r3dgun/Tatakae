using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/products")]
[PermissionChecker(PermissionIds.AdminProductsView)]
public sealed class AdminProductsController(IAdminCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await catalog.GetAllAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await catalog.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminProductsManage)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminProductRequest request, CancellationToken cancellationToken)
    {
        var result = await catalog.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [PermissionChecker(PermissionIds.AdminProductsManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminProductRequest request, CancellationToken cancellationToken)
    {
        var result = await catalog.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminProductsManage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await catalog.DeleteAsync(id, cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }
}
