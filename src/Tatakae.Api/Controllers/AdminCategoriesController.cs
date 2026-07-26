using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/categories")]
[PermissionChecker(PermissionIds.AdminCategoriesView)]
public sealed class AdminCategoriesController(IAdminCategoryService categories) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await categories.GetAllAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminCategoriesManage)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categories.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Created($"/api/admin/categories/{result.Data!.Id}", result.Data);
    }

    [PermissionChecker(PermissionIds.AdminCategoriesManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categories.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminCategoriesManage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await categories.DeleteAsync(id, cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }
}
