using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/payments")]
[PermissionChecker(PermissionIds.AdminOrdersView)]
public sealed class AdminPaymentsController(IPaymentService payments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await payments.AdminListAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminOrdersManage)]
    [HttpPost("{paymentId:guid}/refund")]
    public async Task<IActionResult> Refund(
        Guid paymentId,
        [FromBody] CreateZarinpalRefundRequest request,
        CancellationToken cancellationToken)
    {
        var result = await payments.RefundZarinpalAsync(
            paymentId,
            request,
            User.Identity?.Name ?? "admin",
            cancellationToken);

        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminOrdersManage)]
    [HttpPatch("{paymentId:guid}/status")]
    public async Task<IActionResult> Update(Guid paymentId, [FromBody] UpdatePaymentStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await payments.AdminUpdateStatusAsync(
            paymentId,
            request,
            User.Identity?.Name ?? "admin",
            cancellationToken);

        return result.ToActionResult(this);
    }
}
