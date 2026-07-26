using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Filters;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Security;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/legal")]
[PermissionChecker(PermissionIds.AdminLegalView)]
public sealed class AdminLegalController(ILegalContentService legal) : ControllerBase
{
    [HttpGet("pages")]
    public async Task<IActionResult> Pages(CancellationToken cancellationToken)
    {
        var result = await legal.GetAllPagesAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminLegalManage)]
    [HttpPut("pages/{slug}")]
    public async Task<IActionResult> UpsertPage(string slug, [FromBody] UpsertStorePolicyPageRequest request, CancellationToken cancellationToken)
    {
        request.Slug = string.IsNullOrWhiteSpace(request.Slug) ? slug : request.Slug;
        var result = await legal.UpsertPageAsync(slug, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("contact-messages")]
    public async Task<IActionResult> ContactMessages(CancellationToken cancellationToken)
    {
        var result = await legal.GetContactMessagesAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [PermissionChecker(PermissionIds.AdminLegalManage)]
    [HttpPatch("contact-messages/{id:guid}")]
    public async Task<IActionResult> UpdateContactMessage(Guid id, [FromBody] UpdateContactMessageStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await legal.UpdateContactMessageAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }
}
