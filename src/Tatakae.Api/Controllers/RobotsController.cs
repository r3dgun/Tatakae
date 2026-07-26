using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Api.Seo;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
public sealed class RobotsController(ISeoService seo, IConfiguration configuration) : ControllerBase
{
    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Get()
    {
        var routeResult = seo.GetRoutePolicies();
        if (!routeResult.IsSuccess) return routeResult.ToActionResult(this);

        var configuredBaseUrl = configuration["PublicBaseUrl"];
        var baseUrl = NormalizeBaseUrl(string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : configuredBaseUrl);
        var aiSettings = AiSeoSettings.From(configuration);

        var disallowedPaths = routeResult.Data!
            .Where(x => !x.IsPublic)
            .Select(x => x.Path.EndsWith("/*", StringComparison.Ordinal) ? x.Path[..^2] : x.Path)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        var builder = new StringBuilder();
        AppendAgentGroup(builder, "*", allowPublicContent: true, disallowedPaths);
        AppendAgentGroup(builder, "OAI-SearchBot", aiSettings.AllowOpenAiSearch, disallowedPaths);
        AppendAgentGroup(builder, "ChatGPT-User", aiSettings.AllowOpenAiUserFetch, disallowedPaths);
        AppendAgentGroup(builder, "GPTBot", aiSettings.AllowOpenAiTraining, disallowedPaths);

        builder.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
        builder.AppendLine($"# AI-readable site guide: {baseUrl}/llms.txt");
        builder.AppendLine($"# Public machine-readable catalog: {baseUrl}/ai/catalog.json");

        return Content(builder.ToString(), "text/plain", Encoding.UTF8);
    }

    private static void AppendAgentGroup(
        StringBuilder builder,
        string userAgent,
        bool allowPublicContent,
        IReadOnlyCollection<string> disallowedPaths)
    {
        builder.AppendLine($"User-agent: {userAgent}");
        builder.AppendLine(allowPublicContent ? "Allow: /" : "Disallow: /");
        if (allowPublicContent)
        {
            foreach (var path in disallowedPaths)
            {
                builder.AppendLine($"Disallow: {path}");
            }
        }
        builder.AppendLine();
    }

    private static string NormalizeBaseUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "https://tatakae.example";
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }
}
