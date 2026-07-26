using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Authorize(Policy = PermissionNames.AdminQuestionsView)]
[Route("api/admin/questions")]
public sealed class AdminQuestionsController(IProductEngagementService engagement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await engagement.GetQuestionsForAdminAsync(status, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize(Policy = PermissionNames.AdminQuestionsManage)]
    [HttpPatch("{questionId:guid}/moderate")]
    public async Task<IActionResult> Moderate(Guid questionId, AdminQuestionModerationRequest request, CancellationToken cancellationToken)
    {
        var result = await engagement.ModerateQuestionAsync(questionId, request, null, cancellationToken);
        return result.ToActionResult(this);
    }
}
