using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Security;
using Tatakae.Api.Filters;
using Tatakae.Infrastructure.Payments.Zarinpal;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/admin/payments/zarinpal")]
[PermissionChecker(PermissionIds.AdminOrdersView)]
public sealed class ZarinpalDiagnosticsController(
    IOptions<ZarinpalOptions> optionsAccessor) : ControllerBase
{
    [HttpGet("configuration")]
    public IActionResult GetConfiguration()
    {
        var options = optionsAccessor.Value;
        var issues = new List<string>();

        if (!options.Enabled)
            issues.Add("zarinpal_disabled");
        if (string.IsNullOrWhiteSpace(options.MerchantId))
            issues.Add("merchant_id_missing");
        if (!TryHttpUri(options.CallbackUrl, out _))
            issues.Add("callback_url_invalid");
        if (!TryHttpUri(options.Sandbox ? options.SandboxApiBaseUrl : options.ProductionApiBaseUrl, out var apiUri))
            issues.Add("api_base_url_invalid");
        if (!TryHttpUri(options.Sandbox ? options.SandboxStartPayBaseUrl : options.ProductionStartPayBaseUrl, out var startPayUri))
            issues.Add("start_pay_url_invalid");
        if (!string.Equals(options.Currency?.Trim(), "IRT", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Currency?.Trim(), "IRR", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("currency_invalid");
        }

        var requestReady = options.Enabled &&
                           !issues.Contains("merchant_id_missing", StringComparer.Ordinal) &&
                           !issues.Contains("callback_url_invalid", StringComparer.Ordinal) &&
                           !issues.Contains("api_base_url_invalid", StringComparer.Ordinal) &&
                           !issues.Contains("start_pay_url_invalid", StringComparer.Ordinal) &&
                           !issues.Contains("currency_invalid", StringComparer.Ordinal);

        var refundUriValue = options.Sandbox ? options.SandboxGraphQlUrl : options.GraphQlUrl;
        var refundReady = requestReady &&
                          options.RefundEnabled &&
                          !string.IsNullOrWhiteSpace(options.AccessToken) &&
                          TryHttpUri(refundUriValue, out _);

        if (options.RefundEnabled && string.IsNullOrWhiteSpace(options.AccessToken))
            issues.Add("refund_access_token_missing");
        if (options.RefundEnabled && !TryHttpUri(refundUriValue, out _))
            issues.Add(options.Sandbox ? "sandbox_refund_endpoint_missing" : "refund_endpoint_invalid");

        var status = new ZarinpalConfigurationStatusDto(
            options.Enabled,
            options.Sandbox ? "Sandbox" : "Production",
            !string.IsNullOrWhiteSpace(options.MerchantId),
            requestReady,
            options.RefundEnabled,
            refundReady,
            string.IsNullOrWhiteSpace(options.Currency) ? "IRT" : options.Currency.Trim().ToUpperInvariant(),
            options.CallbackUrl,
            apiUri?.Host ?? string.Empty,
            startPayUri?.Host ?? string.Empty,
            issues.Distinct(StringComparer.Ordinal).ToArray());

        return Ok(new ResultDto<ZarinpalConfigurationStatusDto>().Success(
            requestReady
                ? "تنظیمات پرداخت زرین‌پال برای ایجاد و Verify درخواست آماده است."
                : "تنظیمات زرین‌پال کامل نیست.",
            status));
    }

    private static bool TryHttpUri(string? value, out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) &&
            (string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            uri = parsed;
            return true;
        }

        uri = null;
        return false;
    }
}
