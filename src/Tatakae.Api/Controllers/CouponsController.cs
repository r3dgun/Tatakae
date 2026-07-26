using Microsoft.AspNetCore.Mvc;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/coupons")]
public sealed class CouponsController(ICouponService coupons) : ControllerBase
{
    [HttpPost("quote")]
    [ProducesResponseType(typeof(ResultDto<CouponQuoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultDto<CouponQuoteDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultDto<CouponQuoteDto>>> Quote(
        [FromBody] CouponQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await coupons.QuoteAsync(request, cancellationToken);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }
}
