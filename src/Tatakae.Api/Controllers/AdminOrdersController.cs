using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Admin;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;
using Tatakae.Domain.Enums;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[PermissionChecker(PermissionIds.AdminOrdersView)]
public sealed class AdminOrdersController(IOrderService orders, INotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await orders.GetAllAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("status-options")]
    public IActionResult GetStatusOptions()
        => orders.GetStatusOptions().ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await orders.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}/workflow")]
    public async Task<IActionResult> GetWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var result = await orders.GetWorkflowAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminOrdersManage)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AdminOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            return BadRequest(new Tatakae.Application.Contracts.Common.ResultDto()
                .ValidationFailed("وضعیت سفارش معتبر نیست.", "invalid_order_status"));
        }

        var result = await orders.UpdateStatusAsync(
            id,
            status,
            request.TrackingCode,
            request.AdminNote,
            cancellationToken,
            request.Force,
            User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "admin");

        if (!result.IsSuccess) return result.ToActionResult(this);

        _ = await notifications.QueueOrderStatusChangedAsync(result.Data!, cancellationToken);
        return Ok(result.Data);
    }
}
