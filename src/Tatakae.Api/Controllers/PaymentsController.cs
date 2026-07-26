using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController(
    IPaymentService payments,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await payments.StartAsync(request, CurrentMobile(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetForOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await payments.GetForOrderAsync(orderId, CurrentMobile(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Browser callback configured at Zarinpal. It is intentionally anonymous;
    /// authenticity is established by matching Authority and server-side verify.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("zarinpal/callback")]
    public async Task<IActionResult> ZarinpalCallback(
        [FromQuery] Guid paymentId,
        [FromQuery(Name = "Authority")] string? authority,
        [FromQuery(Name = "Status")] string? status,
        CancellationToken cancellationToken)
    {
        var result = await payments.VerifyZarinpalAsync(paymentId, authority, status, cancellationToken);
        var receipt = result.Data;

        return Redirect(BuildWebReturnUrl(receipt?.OrderId, paymentId));
    }

    private string BuildWebReturnUrl(
        Guid? orderId,
        Guid paymentId)
    {
        var configured = configuration["Payments:WebReturnUrl"];
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var baseUri) ||
            (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            baseUri = new Uri("https://localhost:7076/payment-result");
        }

        var values = new Dictionary<string, string?>
        {
            ["paymentId"] = paymentId.ToString("D"),
            ["orderId"] = orderId?.ToString("D")
        };

        var query = string.Join("&", values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
        var builder = new UriBuilder(baseUri) { Query = query };
        return builder.Uri.ToString();
    }

    private string? CurrentMobile()
        => User.FindFirstValue("mobile") ?? User.FindFirstValue(ClaimTypes.Name);
}
