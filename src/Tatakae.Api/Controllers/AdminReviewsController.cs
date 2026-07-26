using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Authorize(Policy = PermissionNames.AdminQuestionsView)]
[Route("api/admin/reviews")]
public sealed class AdminReviewsController(IProductEngagementService engagement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await engagement.GetReviewsForAdminAsync(status, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize(Policy = PermissionNames.AdminQuestionsManage)]
    [HttpPatch("{reviewId:guid}/moderate")]
    public async Task<IActionResult> Moderate(Guid reviewId, AdminReviewModerationRequest request, CancellationToken cancellationToken)
    {
        var result = await engagement.ModerateReviewAsync(reviewId, request, cancellationToken);
        return result.ToActionResult(this);
    }
}
