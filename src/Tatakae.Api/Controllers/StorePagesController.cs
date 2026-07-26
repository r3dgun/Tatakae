using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/store-pages")]
public sealed class StorePagesController(ILegalContentService legal) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Published(CancellationToken cancellationToken)
    {
        var result = await legal.GetPublishedPagesAsync(cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Page(string slug, CancellationToken cancellationToken)
    {
        var result = await legal.GetPublishedPageAsync(slug, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("contact")]
    public async Task<IActionResult> Contact([FromBody] SubmitContactMessageRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await legal.SubmitContactAsync(request, ip, cancellationToken);
        return result.ToActionResult(this);
    }
}
