using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tatakae.Api.Controllers;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Infrastructure.Payments.Zarinpal;

namespace Tatakae.Api.Tests;

public sealed class ZarinpalDiagnosticsControllerTests
{
    [Fact]
    public void GetConfiguration_WhenMerchantIsMissing_ReportsSandboxNotReadyWithoutSecrets()
    {
        var controller = new ZarinpalDiagnosticsController(Options.Create(new ZarinpalOptions
        {
            Enabled = true,
            Sandbox = true,
            MerchantId = string.Empty,
            CallbackUrl = "https://localhost:7075/api/payments/zarinpal/callback"
        }));

        var action = controller.GetConfiguration();

        var ok = Assert.IsType<OkObjectResult>(action);
        var result = Assert.IsType<ResultDto<ZarinpalConfigurationStatusDto>>(ok.Value);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Sandbox", result.Data.Mode);
        Assert.False(result.Data.MerchantConfigured);
        Assert.False(result.Data.RequestReady);
        Assert.Contains("merchant_id_missing", result.Data.Issues);
    }

    [Fact]
    public void GetConfiguration_WhenSandboxRequestSettingsAreComplete_ReportsReadyAndRefundDisabled()
    {
        var controller = new ZarinpalDiagnosticsController(Options.Create(new ZarinpalOptions
        {
            Enabled = true,
            Sandbox = true,
            MerchantId = "00000000-0000-0000-0000-000000000000",
            CallbackUrl = "https://localhost:7075/api/payments/zarinpal/callback",
            RefundEnabled = false
        }));

        var action = controller.GetConfiguration();

        var ok = Assert.IsType<OkObjectResult>(action);
        var result = Assert.IsType<ResultDto<ZarinpalConfigurationStatusDto>>(ok.Value);
        Assert.True(result.Data!.RequestReady);
        Assert.False(result.Data.RefundEnabled);
        Assert.False(result.Data.RefundReady);
        Assert.Equal("sandbox.zarinpal.com", result.Data.ApiHost);
        Assert.Equal("sandbox.zarinpal.com", result.Data.StartPayHost);
    }
}
