using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Security;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/security")]
[PermissionChecker(PermissionIds.AdminSecurityView)]
public sealed class AdminSecurityController(ISecurityAdminService security) : ControllerBase
{
    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions(CancellationToken cancellationToken)
        => (await security.GetPermissionsAsync(cancellationToken)).ToActionResult(this);

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken)
        => (await security.GetRolesAsync(cancellationToken)).ToActionResult(this);

    [PermissionChecker(PermissionIds.AdminSecurityManage)]
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
        => (await security.CreateRoleAsync(request, cancellationToken)).ToActionResult(this);

    [PermissionChecker(PermissionIds.AdminSecurityManage)]
    [HttpPut("roles/{roleId:guid}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(Guid roleId, [FromBody] AssignRolePermissionsRequest request, CancellationToken cancellationToken)
        => (await security.UpdateRolePermissionsAsync(roleId, request, cancellationToken)).ToActionResult(this);

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
        => (await security.GetUsersAsync(cancellationToken)).ToActionResult(this);

    [PermissionChecker(PermissionIds.AdminSecurityManage)]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken)
        => (await security.CreateAdminUserAsync(request, cancellationToken)).ToActionResult(this);

    [PermissionChecker(PermissionIds.AdminSecurityManage)]
    [HttpPut("users/{userId:guid}/roles")]
    public async Task<IActionResult> UpdateUserRoles(Guid userId, [FromBody] AssignUserRolesRequest request, CancellationToken cancellationToken)
        => (await security.UpdateUserRolesAsync(userId, request, cancellationToken)).ToActionResult(this);

    [HttpGet("admin-pages")]
    public async Task<IActionResult> AdminPages(CancellationToken cancellationToken)
        => (await security.GetAdminPagesAsync(cancellationToken)).ToActionResult(this);

    [HttpGet("login-audits")]
    public async Task<IActionResult> LoginAudits(CancellationToken cancellationToken)
        => (await security.GetLoginAuditsAsync(cancellationToken)).ToActionResult(this);

    [PermissionChecker(PermissionIds.AdminSecurityManage)]
    [HttpPut("admin-pages/{id:guid}")]
    public async Task<IActionResult> UpsertAdminPage(Guid id, [FromBody] UpsertAdminPageAccessRequest request, CancellationToken cancellationToken)
        => (await security.UpsertAdminPageAsync(id, request, cancellationToken)).ToActionResult(this);
}
