using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController(IOrderService orders, INotificationService notifications) : ControllerBase
{
    [HttpPost("quote-embroidery")]
    public async Task<IActionResult> Quote([FromBody] EmbroideryCustomizationRequest request, CancellationToken cancellationToken)
    {
        var result = await orders.QuoteEmbroideryAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        ApplyAuthenticatedCustomer(request);
        var result = await orders.CheckoutAsync(request, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);

        _ = await notifications.QueueOrderCreatedAsync(result.Data!, cancellationToken);
        return Created($"/api/admin/orders/{result.Data!.Id}", result.Data);
    }

    private void ApplyAuthenticatedCustomer(CheckoutRequest request)
    {
        request.Mobile = User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name) ?? request.Mobile;
        request.CustomerName = User.FindFirstValue(ClaimTypes.GivenName) ?? request.CustomerName;
        request.Email = User.FindFirstValue(ClaimTypes.Email) ?? request.Email;
    }
}
