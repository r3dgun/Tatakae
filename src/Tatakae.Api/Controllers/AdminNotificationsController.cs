using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[PermissionChecker(PermissionIds.AdminNotificationsView)]
public sealed class AdminNotificationsController(INotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AdminNotificationFilter filter, CancellationToken cancellationToken)
    {
        var result = await notifications.AdminListAsync(filter, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [PermissionChecker(PermissionIds.AdminNotificationsManage)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await notifications.AdminCreateAsync(request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Created($"/api/admin/notifications/{result.Data!.Id}", result.Data);
    }

    [HttpPatch("{id:guid}/status")]
    [PermissionChecker(PermissionIds.AdminNotificationsManage)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateNotificationStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await notifications.AdminUpdateStatusAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }
}
