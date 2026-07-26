using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

/// <summary>Dynamic SEO sitemap. In production set PublicBaseUrl from configuration.</summary>
[ApiController]
public sealed class SitemapController(ISeoService seo, IConfiguration configuration) : ControllerBase
{
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var configuredBaseUrl = configuration["PublicBaseUrl"];
        var result = await seo.GetSitemapAsync(
            string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}"
                : configuredBaseUrl,
            cancellationToken);

        if (!result.IsSuccess) return result.ToActionResult(this);
        var sitemap = result.Data!;

        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            await writer.WriteStartDocumentAsync();
            const string sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
            const string imageNamespace = "http://www.google.com/schemas/sitemap-image/1.1";
            await writer.WriteStartElementAsync(null, "urlset", sitemapNamespace);
            await writer.WriteAttributeStringAsync("xmlns", "image", null, imageNamespace);
            foreach (var item in sitemap.Urls)
            {
                await writer.WriteStartElementAsync(null, "url", null);
                await writer.WriteElementStringAsync(null, "loc", null, item.Location);
                await writer.WriteElementStringAsync(null, "lastmod", null, item.LastModified.ToString("yyyy-MM-dd"));
                await writer.WriteElementStringAsync(null, "changefreq", null, item.ChangeFrequency);
                await writer.WriteElementStringAsync(null, "priority", null, item.Priority.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(item.ImageUrl))
                {
                    await writer.WriteStartElementAsync("image", "image", imageNamespace);
                    await writer.WriteElementStringAsync("image", "loc", imageNamespace, item.ImageUrl);
                    if (!string.IsNullOrWhiteSpace(item.ImageTitle))
                    {
                        await writer.WriteElementStringAsync("image", "title", imageNamespace, item.ImageTitle);
                    }
                    await writer.WriteEndElementAsync();
                }
                await writer.WriteEndElementAsync();
            }
            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
            await writer.FlushAsync();
        }

        return File(stream.ToArray(), "application/xml; charset=utf-8");
    }
}
