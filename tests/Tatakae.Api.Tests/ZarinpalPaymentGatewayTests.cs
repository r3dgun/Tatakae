using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Infrastructure.Payments.Zarinpal;

namespace Tatakae.Api.Tests;

public sealed class ZarinpalPaymentGatewayTests
{
    [Fact]
    public async Task RequestAsync_UsesV4EndpointAndBuildsSandboxRedirect()
    {
        var handler = new StubHandler("""
        {
          "data": {
            "code": 100,
            "message": "Success",
            "authority": "S000000000000000000000000000001",
            "fee_type": "Merchant",
            "fee": 0
          },
          "errors": []
        }
        """);
        var gateway = CreateGateway(handler);
        var paymentId = Guid.NewGuid();

        var result = await gateway.RequestAsync(new ZarinpalPaymentRequest(
            paymentId,
            250_000m,
            "IRT",
            "پرداخت سفارش EMB-100",
            "09123456789",
            "EMB-100"));

        Assert.True(result.IsSuccessful);
        Assert.Equal(100, result.Code);
        Assert.Equal("S000000000000000000000000000001", result.Authority);
        Assert.Equal("https://sandbox.zarinpal.com/pg/StartPay/S000000000000000000000000000001", result.RedirectUrl);
        Assert.Equal("https://sandbox.zarinpal.com/pg/v4/payment/request.json", handler.RequestUri?.ToString());
        Assert.Contains("\"merchant_id\":\"00000000-0000-0000-0000-000000000000\"", handler.RequestBody);
        Assert.Contains("\"amount\":250000", handler.RequestBody);
        Assert.Contains("\"currency\":\"IRT\"", handler.RequestBody);
        Assert.Contains($"paymentId={paymentId:D}", handler.RequestBody);
    }

    [Fact]
    public async Task VerifyAsync_AcceptsCode101AsIdempotentSuccess()
    {
        var handler = new StubHandler("""
        {
          "data": {
            "code": 101,
            "message": "Verified",
            "ref_id": 987654321,
            "card_pan": "6219-****-****-1234",
            "card_hash": "hash",
            "fee": 0
          },
          "errors": []
        }
        """);
        var gateway = CreateGateway(handler);

        var result = await gateway.VerifyAsync(new ZarinpalVerifyRequest(
            250_000m,
            "IRT",
            "S000000000000000000000000000001"));

        Assert.True(result.IsSuccessful);
        Assert.True(result.WasAlreadyVerified);
        Assert.Equal(101, result.Code);
        Assert.Equal(987654321L, result.ReferenceId);
        Assert.Equal("6219-****-****-1234", result.CardPan);
        Assert.Equal("https://sandbox.zarinpal.com/pg/v4/payment/verify.json", handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task ReverseAsync_UsesOfficialV4EndpointAndMerchantAuthorityPayload()
    {
        var handler = new StubHandler("""
        {
          "data": {
            "code": 100,
            "message": "Reversed"
          },
          "errors": []
        }
        """);
        var gateway = CreateGateway(handler);

        var result = await gateway.ReverseAsync(new ZarinpalReverseRequest(
            "S000000000000000000000000000001"));

        Assert.True(result.IsSuccessful);
        Assert.Equal(100, result.Code);
        Assert.Equal("https://sandbox.zarinpal.com/pg/v4/payment/reverse.json", handler.RequestUri?.ToString());
        Assert.Contains("\"merchant_id\":\"00000000-0000-0000-0000-000000000000\"", handler.RequestBody);
        Assert.Contains("\"authority\":\"S000000000000000000000000000001\"", handler.RequestBody);
    }

    [Fact]
    public async Task RefundAsync_UsesOfficialGraphQlMutationAndBearerAccessToken()
    {
        var handler = new StubHandler("""
        {
          "data": {
            "resource": {
              "terminal_id": "TERM-1",
              "id": "REFUND-1001",
              "amount": 250000,
              "timeline": {
                "refund_amount": 250000,
                "refund_time": "2026-07-23T10:00:00Z",
                "refund_status": "PAID"
              }
            }
          }
        }
        """);
        var gateway = CreateGateway(handler, sandbox: false, refundEnabled: true);

        var result = await gateway.RefundAsync(new ZarinpalRefundRequest(
            "S000000000000000000000000000001",
            250_000m,
            "IRT",
            "Refund order EMB-100"));

        Assert.True(result.IsSuccessful);
        Assert.True(result.IsCompleted);
        Assert.Equal("REFUND-1001", result.RefundId);
        Assert.Equal(250_000m, result.RefundedAmount);
        Assert.Equal("https://next.zarinpal.com/api/v4/graphql/", handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("test-access-token", handler.AuthorizationParameter);
        Assert.Contains("AddRefund", handler.RequestBody);
        Assert.Contains("\"session_id\":\"S000000000000000000000000000001\"", handler.RequestBody);
        Assert.Contains("\"amount\":250000", handler.RequestBody);
    }


    [Fact]
    public async Task RefundAsync_InSandbox_IsBlockedBeforeProductionGraphQlIsCalled()
    {
        var handler = new StubHandler("{\"data\":{}}");
        var gateway = CreateGateway(handler);

        var result = await gateway.RefundAsync(new ZarinpalRefundRequest(
            "S000000000000000000000000000001",
            250_000m,
            "IRT",
            "Refund order EMB-100"));

        Assert.False(result.IsSuccessful);
        Assert.Equal("zarinpal_refund_configuration_error", result.ErrorCode);
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task RequestAsync_MapsGatewayErrorWithoutThrowingHttpDetailsToApplication()
    {
        var handler = new StubHandler("""
        {
          "data": [],
          "errors": {
            "code": -9,
            "message": "Validation error"
          }
        }
        """, HttpStatusCode.UnprocessableEntity);
        var gateway = CreateGateway(handler);

        var result = await gateway.RequestAsync(new ZarinpalPaymentRequest(
            Guid.NewGuid(),
            250_000m,
            "IRT",
            "پرداخت سفارش",
            "09123456789",
            "EMB-100"));

        Assert.False(result.IsSuccessful);
        Assert.Equal(-9, result.Code);
        Assert.Equal("Validation error", result.Message);
    }


    [Fact]
    public async Task RequestAsync_WhenCurrencyIsIrr_ConvertsStoredTomanToRial()
    {
        var handler = new StubHandler("""
        {
          "data": {
            "code": 100,
            "message": "Success",
            "authority": "S000000000000000000000000000002"
          },
          "errors": []
        }
        """);
        var gateway = CreateGateway(handler, currency: "IRR");

        var result = await gateway.RequestAsync(new ZarinpalPaymentRequest(
            Guid.NewGuid(),
            250_000m,
            "IRR",
            "پرداخت سفارش",
            "09123456789",
            "EMB-101"));

        Assert.True(result.IsSuccessful);
        Assert.Contains("\"amount\":2500000", handler.RequestBody);
        Assert.Contains("\"currency\":\"IRR\"", handler.RequestBody);
    }

    [Fact]
    public async Task VerifyAsync_RedactsCardHashFromPersistableRawResponse()
    {
        var handler = new StubHandler("""
        {
          "data": {
            "code": 100,
            "message": "Verified",
            "ref_id": 123456789,
            "card_pan": "6219-****-****-1234",
            "card_hash": "sensitive-card-hash",
            "fee": 0
          },
          "errors": []
        }
        """);
        var gateway = CreateGateway(handler);

        var result = await gateway.VerifyAsync(new ZarinpalVerifyRequest(
            250_000m,
            "IRT",
            "S000000000000000000000000000001"));

        Assert.True(result.IsSuccessful);
        Assert.DoesNotContain("sensitive-card-hash", result.RawResponse);
        Assert.Contains("[redacted]", result.RawResponse);
    }

    private static ZarinpalPaymentGateway CreateGateway(
        HttpMessageHandler handler,
        string currency = "IRT",
        bool sandbox = true,
        bool refundEnabled = false)
    {
        var options = Options.Create(new ZarinpalOptions
        {
            MerchantId = "00000000-0000-0000-0000-000000000000",
            AccessToken = "test-access-token",
            Enabled = true,
            Sandbox = sandbox,
            RefundEnabled = refundEnabled,
            Currency = currency,
            CallbackUrl = "https://api.example.com/api/payments/zarinpal/callback"
        });

        return new ZarinpalPaymentGateway(new HttpClient(handler), options);
    }

    private sealed class StubHandler(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
