using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/shipping")]
public sealed class ShippingController(IShippingService shipping) : ControllerBase
{
    [HttpPost("quote")]
    public async Task<IActionResult> Quote([FromBody] ShippingQuoteRequest request, CancellationToken cancellationToken)
    {
        var result = await shipping.GetCheckoutMethodsAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("methods")]
    public async Task<IActionResult> Methods(CancellationToken cancellationToken)
    {
        var request = new ShippingQuoteRequest { Province = "تهران", City = "تهران", CartSubtotal = 0, ItemCount = 1 };
        var result = await shipping.GetCheckoutMethodsAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
