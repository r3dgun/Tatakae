using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account/notifications")]
public sealed class NotificationsController(INotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await notifications.GetMineAsync(CurrentMobile(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var result = await notifications.CountUnreadAsync(CurrentMobile(), cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);
        return Ok(new { unread = result.Data });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await notifications.MarkReadAsync(CurrentMobile(), id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await notifications.MarkAllReadAsync(CurrentMobile(), cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }

    private string CurrentMobile()
        => User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
