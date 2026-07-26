using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Seo;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

/// <summary>Public machine-readable discovery endpoints for answer engines and AI assistants.</summary>
[ApiController]
public sealed class AiSeoController(ISeoService seo, IConfiguration configuration) : ControllerBase
{
    [HttpGet("/llms.txt")]
    [ResponseCache(Duration = 3600)]
    public Task<IActionResult> Llms(CancellationToken cancellationToken)
        => BuildLlmsAsync(includeFullCatalog: false, cancellationToken: cancellationToken);

    [HttpGet("/llms-full.txt")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> LlmsFull(CancellationToken cancellationToken)
    {
        var settings = AiSeoSettings.From(configuration);
        if (!settings.ExposeFullCatalog)
        {
            return new Tatakae.Application.Contracts.Common.ResultDto().NotFound(
                "نسخه کامل کاتالوگ هوش مصنوعی منتشر نشده است.",
                "llms_full_disabled").ToActionResult(this);
        }

        return await BuildLlmsAsync(includeFullCatalog: true, cancellationToken);
    }

    [HttpGet("/ai/catalog.json")]
    [ResponseCache(Duration = 1800)]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken)
    {
        var settings = AiSeoSettings.From(configuration);
        var result = await seo.GetAiCatalogAsync(BaseUrl(), settings.ToProfile(), cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);

        Response.Headers["Cache-Control"] = "public,max-age=1800";
        Response.Headers["X-Robots-Tag"] = "noindex, follow";
        return Ok(result.Data);
    }

    private async Task<IActionResult> BuildLlmsAsync(bool includeFullCatalog, CancellationToken cancellationToken)
    {
        var settings = AiSeoSettings.From(configuration);
        var result = await seo.GetLlmsDocumentAsync(BaseUrl(), settings.ToProfile(), includeFullCatalog, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);

        Response.Headers["Cache-Control"] = "public,max-age=3600";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Robots-Tag"] = "noindex, follow";
        return Content(result.Data!.Content, "text/markdown", Encoding.UTF8);
    }

    private string BaseUrl()
    {
        var configuredBaseUrl = configuration["PublicBaseUrl"];
        return string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : configuredBaseUrl;
    }
}
