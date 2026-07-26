using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Cart;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public sealed class CartController(ICartPersistenceService cart) : ControllerBase
{
    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeCartRequest request, CancellationToken cancellationToken)
    {
        var result = await cart.MergeAsync(request, CurrentCustomer(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var result = await cart.ClearAsync(CurrentCustomer(), cancellationToken);
        return result.ToActionResult(this, noContentOnSuccess: true);
    }

    private CartCustomerContext CurrentCustomer()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var identityUserId = Guid.TryParse(rawUserId, out var userId) ? userId : (Guid?)null;
        var mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
        var fullName = User.FindFirstValue(ClaimTypes.GivenName) ?? "مشتری Tatakae";
        var email = User.FindFirstValue(ClaimTypes.Email);

        return new CartCustomerContext(identityUserId, mobile, fullName, email);
    }
}
