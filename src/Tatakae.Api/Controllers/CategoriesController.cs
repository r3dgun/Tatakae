using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await catalog.GetNavigationCategoriesAsync(cancellationToken);
        return result.ToActionResult(this);
    }
}
