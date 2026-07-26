using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(ILocationService locations) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All(CancellationToken cancellationToken)
        => (await locations.GetProvincesAsync(cancellationToken)).ToActionResult(this);

    [HttpGet("provinces")]
    public async Task<IActionResult> Provinces(CancellationToken cancellationToken)
        => (await locations.GetProvincesAsync(cancellationToken)).ToActionResult(this);

    [HttpGet("cities")]
    public async Task<IActionResult> Cities([FromQuery] string province, CancellationToken cancellationToken)
        => (await locations.GetCitiesAsync(province, cancellationToken)).ToActionResult(this);
}
