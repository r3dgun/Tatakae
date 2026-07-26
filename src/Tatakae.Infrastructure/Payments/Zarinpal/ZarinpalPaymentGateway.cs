using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces.Gateways;

namespace Tatakae.Infrastructure.Payments.Zarinpal;

/// <summary>
/// Zarinpal v4 HTTP adapter. It has no knowledge of EF Core, orders, or application
/// workflow and only translates between the external JSON API and gateway results.
/// </summary>
public sealed class ZarinpalPaymentGateway(
    HttpClient httpClient,
    IOptions<ZarinpalOptions> optionsAccessor) : IZarinpalPaymentGateway
{
    private readonly ZarinpalOptions options = optionsAccessor.Value;

    public string Currency => NormalizeCurrency(options.Currency);

    public string GetRedirectUrl(string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            throw new ArgumentException("Authority زرین‌پال الزامی است.", nameof(authority));

        return $"{StartPayBaseUrl().TrimEnd('/')}/{Uri.EscapeDataString(authority.Trim())}";
    }

    public async Task<ZarinpalRequestResult> RequestAsync(
        ZarinpalPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            ValidateConfiguration();
            EnsureRequestedCurrency(request.Currency);

            var payload = new
            {
                merchant_id = options.MerchantId.Trim(),
                amount = ToGatewayAmount(request.Amount),
                currency = Currency,
                callback_url = AppendPaymentId(options.CallbackUrl, request.PaymentId),
                description = request.Description,
                metadata = new
                {
                    mobile = request.CustomerMobile,
                    order_id = request.OrderNumber
                }
            };

            using var response = await httpClient.PostAsJsonAsync(
                new Uri(ApiBaseUrl(), "pg/v4/payment/request.json"),
                payload,
                cancellationToken);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = ParseResponse(raw);
            var authority = parsed.StringValue("authority");
            var successful = response.IsSuccessStatusCode &&
                             parsed.Code == 100 &&
                             !string.IsNullOrWhiteSpace(authority);

            return new ZarinpalRequestResult(
                successful,
                parsed.Code,
                parsed.Message(successful
                    ? "درخواست پرداخت زرین‌پال ایجاد شد."
                    : "ایجاد درخواست زرین‌پال ناموفق بود."),
                authority,
                successful ? GetRedirectUrl(authority!) : null,
                SanitizeGatewayResponse(raw));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ZarinpalRequestResult(
                false,
                0,
                "مهلت ارتباط با زرین‌پال به پایان رسید.",
                null,
                null,
                "zarinpal_timeout");
        }
        catch (HttpRequestException ex)
        {
            return new ZarinpalRequestResult(
                false,
                0,
                "ارتباط با زرین‌پال برقرار نشد.",
                null,
                null,
                ex.Message);
        }
        catch (JsonException ex)
        {
            return new ZarinpalRequestResult(
                false,
                0,
                "پاسخ زرین‌پال قابل پردازش نبود.",
                null,
                null,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new ZarinpalRequestResult(
                false,
                0,
                ex.Message,
                null,
                null,
                "zarinpal_configuration_error");
        }
        catch (ArgumentException ex)
        {
            return new ZarinpalRequestResult(
                false,
                0,
                ex.Message,
                null,
                null,
                "zarinpal_invalid_request");
        }
        catch (OverflowException ex)
        {
            return new ZarinpalRequestResult(
                false,
                0,
                "مبلغ پرداخت از محدوده مجاز زرین‌پال بیشتر است.",
                null,
                null,
                "zarinpal_amount_overflow");
        }
    }

    public async Task<ZarinpalVerifyResult> VerifyAsync(
        ZarinpalVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Authority))
            throw new ArgumentException("Authority زرین‌پال الزامی است.", nameof(request));

        try
        {
            ValidateConfiguration();
            EnsureRequestedCurrency(request.Currency);

            var payload = new
            {
                merchant_id = options.MerchantId.Trim(),
                amount = ToGatewayAmount(request.Amount),
                currency = Currency,
                authority = request.Authority.Trim()
            };

            using var response = await httpClient.PostAsJsonAsync(
                new Uri(ApiBaseUrl(), "pg/v4/payment/verify.json"),
                payload,
                cancellationToken);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = ParseResponse(raw);
            var successful = response.IsSuccessStatusCode && parsed.Code is 100 or 101;

            return new ZarinpalVerifyResult(
                successful,
                parsed.Code == 101,
                parsed.Code,
                parsed.Message(successful
                    ? "پرداخت توسط زرین‌پال تأیید شد."
                    : "تأیید پرداخت زرین‌پال ناموفق بود."),
                parsed.LongValue("ref_id"),
                parsed.StringValue("card_pan"),
                parsed.LongValue("fee"),
                SanitizeGatewayResponse(raw));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ZarinpalVerifyResult(
                false,
                false,
                0,
                "مهلت تأیید پرداخت زرین‌پال به پایان رسید.",
                null,
                null,
                null,
                "zarinpal_timeout");
        }
        catch (HttpRequestException ex)
        {
            return new ZarinpalVerifyResult(
                false,
                false,
                0,
                "ارتباط با زرین‌پال برای تأیید پرداخت برقرار نشد.",
                null,
                null,
                null,
                ex.Message);
        }
        catch (JsonException ex)
        {
            return new ZarinpalVerifyResult(
                false,
                false,
                0,
                "پاسخ تأیید زرین‌پال قابل پردازش نبود.",
                null,
                null,
                null,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new ZarinpalVerifyResult(
                false,
                false,
                0,
                ex.Message,
                null,
                null,
                null,
                "zarinpal_configuration_error");
        }
        catch (ArgumentException ex)
        {
            return new ZarinpalVerifyResult(
                false,
                false,
                0,
                ex.Message,
                null,
                null,
                null,
                "zarinpal_invalid_request");
        }
        catch (OverflowException ex)
        {
            return new ZarinpalVerifyResult(
                false,
                false,
                0,
                "مبلغ پرداخت از محدوده مجاز زرین‌پال بیشتر است.",
                null,
                null,
                null,
                ex.Message);
        }
    }

    public async Task<ZarinpalReverseResult> ReverseAsync(
        ZarinpalReverseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Authority))
            throw new ArgumentException("Authority زرین‌پال الزامی است.", nameof(request));

        try
        {
            ValidateConfiguration();

            var payload = new
            {
                merchant_id = options.MerchantId.Trim(),
                authority = request.Authority.Trim()
            };

            using var response = await httpClient.PostAsJsonAsync(
                new Uri(ApiBaseUrl(), "pg/v4/payment/reverse.json"),
                payload,
                cancellationToken);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = ParseResponse(raw);
            var successful = response.IsSuccessStatusCode && parsed.Code is 100 or 101;

            return new ZarinpalReverseResult(
                successful,
                parsed.Code == 101,
                parsed.Code,
                parsed.Message(successful
                    ? "تراکنش با موفقیت در زرین‌پال برگشت داده شد."
                    : "برگشت تراکنش زرین‌پال ناموفق بود."),
                SanitizeGatewayResponse(raw));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ZarinpalReverseResult(
                false,
                false,
                0,
                "مهلت برگشت تراکنش زرین‌پال به پایان رسید.",
                "zarinpal_reverse_timeout");
        }
        catch (HttpRequestException ex)
        {
            return new ZarinpalReverseResult(
                false,
                false,
                0,
                "ارتباط با زرین‌پال برای برگشت تراکنش برقرار نشد.",
                ex.Message);
        }
        catch (JsonException ex)
        {
            return new ZarinpalReverseResult(
                false,
                false,
                0,
                "پاسخ برگشت تراکنش زرین‌پال قابل پردازش نبود.",
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new ZarinpalReverseResult(
                false,
                false,
                0,
                ex.Message,
                "zarinpal_configuration_error");
        }
        catch (ArgumentException ex)
        {
            return new ZarinpalReverseResult(
                false,
                false,
                0,
                ex.Message,
                "zarinpal_invalid_reverse_request");
        }
    }

    public async Task<ZarinpalRefundResult> RefundAsync(
        ZarinpalRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SessionId))
            throw new ArgumentException("SessionId/Authority زرین‌پال الزامی است.", nameof(request));

        try
        {
            EnsureRefundConfiguration();
            EnsureRequestedCurrency(request.Currency);

            const string query = """
                mutation AddRefund(
                  $session_id: ID!,
                  $amount: BigInteger!,
                  $description: String,
                  $reason: RefundReasonEnum
                ) {
                  resource: AddRefund(
                    session_id: $session_id,
                    amount: $amount,
                    description: $description,
                    reason: $reason
                  ) {
                    terminal_id
                    id
                    amount
                    timeline {
                      refund_amount
                      refund_time
                      refund_status
                    }
                  }
                }
                """;

            var payload = new
            {
                query,
                variables = new
                {
                    session_id = request.SessionId.Trim(),
                    amount = ToGatewayAmount(request.Amount),
                    description = request.Description.Trim(),
                    reason = string.IsNullOrWhiteSpace(request.Reason)
                        ? "CUSTOMER_REQUEST"
                        : request.Reason.Trim().ToUpperInvariant()
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, RefundGraphQlUri())
            {
                Content = JsonContent.Create(payload)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                options.AccessToken.Trim());

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            if (!response.IsSuccessStatusCode)
            {
                var graphError = ReadGraphQlError(root);
                return new ZarinpalRefundResult(
                    false,
                    false,
                    graphError.Code,
                    graphError.Message ?? "درخواست Refund زرین‌پال ناموفق بود.",
                    null,
                    null,
                    null,
                    SanitizeGatewayResponse(raw));
            }

            if (!root.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Object ||
                !data.TryGetProperty("resource", out var resource) ||
                resource.ValueKind != JsonValueKind.Object)
            {
                var graphError = ReadGraphQlError(root);
                return new ZarinpalRefundResult(
                    false,
                    false,
                    graphError.Code ?? "zarinpal_refund_resource_missing",
                    graphError.Message ?? "پاسخ Refund زرین‌پال شامل شناسه استرداد نبود.",
                    null,
                    null,
                    null,
                    SanitizeGatewayResponse(raw));
            }

            var refundId = ReadFlexibleString(resource, "id");
            var providerStatus = ReadRefundStatus(resource);
            var refundedAmount = ReadRefundAmount(resource);
            var completed = IsCompletedRefundStatus(providerStatus);
            var accepted = !string.IsNullOrWhiteSpace(refundId);

            return new ZarinpalRefundResult(
                accepted,
                accepted && completed,
                accepted ? null : "zarinpal_refund_id_missing",
                accepted
                    ? completed
                        ? "Refund زرین‌پال با موفقیت تکمیل شد."
                        : "درخواست Refund توسط زرین‌پال پذیرفته شد و در انتظار تکمیل است."
                    : "درخواست Refund زرین‌پال پذیرفته نشد.",
                refundId,
                refundedAmount is null ? null : FromGatewayAmount(refundedAmount.Value),
                providerStatus,
                SanitizeGatewayResponse(raw));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ZarinpalRefundResult(
                false,
                false,
                "zarinpal_refund_timeout",
                "مهلت ارتباط با سرویس Refund زرین‌پال به پایان رسید.",
                null,
                null,
                null,
                "zarinpal_refund_timeout");
        }
        catch (HttpRequestException ex)
        {
            return new ZarinpalRefundResult(
                false,
                false,
                "zarinpal_refund_connection_failed",
                "ارتباط با سرویس Refund زرین‌پال برقرار نشد.",
                null,
                null,
                null,
                ex.Message);
        }
        catch (JsonException ex)
        {
            return new ZarinpalRefundResult(
                false,
                false,
                "zarinpal_refund_invalid_response",
                "پاسخ Refund زرین‌پال قابل پردازش نبود.",
                null,
                null,
                null,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new ZarinpalRefundResult(
                false,
                false,
                "zarinpal_refund_configuration_error",
                ex.Message,
                null,
                null,
                null,
                "zarinpal_refund_configuration_error");
        }
        catch (ArgumentException ex)
        {
            return new ZarinpalRefundResult(
                false,
                false,
                "zarinpal_refund_invalid_request",
                ex.Message,
                null,
                null,
                null,
                "zarinpal_refund_invalid_request");
        }
        catch (OverflowException ex)
        {
            return new ZarinpalRefundResult(
                false,
                false,
                "zarinpal_refund_amount_overflow",
                "مبلغ Refund از محدوده مجاز زرین‌پال بیشتر است.",
                null,
                null,
                null,
                ex.Message);
        }
    }

    private void EnsureRequestedCurrency(string? requestedCurrency)
    {
        if (!string.Equals(
                NormalizeCurrency(requestedCurrency),
                Currency,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "واحد پول درخواست پرداخت با تنظیمات زرین‌پال مطابقت ندارد.");
        }
    }

    private static string SanitizeGatewayResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        try
        {
            var root = JsonNode.Parse(raw);
            if (root?["data"] is JsonObject data && data.ContainsKey("card_hash"))
                data["card_hash"] = "[redacted]";

            return root?.ToJsonString() ?? raw;
        }
        catch (JsonException)
        {
            return raw;
        }
    }

    private void EnsureRefundConfiguration()
    {
        if (!options.Enabled)
            throw new InvalidOperationException("درگاه زرین‌پال غیرفعال است.");

        if (!options.RefundEnabled)
            throw new InvalidOperationException(
                options.Sandbox
                    ? "Refund زرین‌پال در حالت Sandbox عمداً غیرفعال است تا endpoint مالی Production فراخوانی نشود."
                    : "Refund زرین‌پال در تنظیمات غیرفعال است.");

        if (string.IsNullOrWhiteSpace(options.AccessToken))
            throw new InvalidOperationException(
                "AccessToken سرویس Refund زرین‌پال تنظیم نشده است.");

        _ = RefundGraphQlUri();
    }

    private Uri RefundGraphQlUri()
    {
        var value = options.Sandbox
            ? options.SandboxGraphQlUrl
            : options.GraphQlUrl;

        if (options.Sandbox && string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "برای Sandbox هیچ endpoint رسمی Refund تنظیم نشده است؛ Refund تا زمان دریافت endpoint آزمایشی از زرین‌پال غیرفعال می‌ماند.");
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException("آدرس GraphQL زرین‌پال معتبر نیست.");
    }

    private decimal FromGatewayAmount(decimal amount)
        => Currency == "IRR" ? amount / 10m : amount;

    private static string? ReadFlexibleString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static decimal? ReadFlexibleDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;

        return value.ValueKind == JsonValueKind.String &&
               decimal.TryParse(
                   value.GetString(),
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out number)
            ? number
            : null;
    }

    private static decimal? ReadRefundAmount(JsonElement resource)
    {
        if (resource.TryGetProperty("timeline", out var timeline))
        {
            if (timeline.ValueKind == JsonValueKind.Object)
            {
                var amount = ReadFlexibleDecimal(timeline, "refund_amount");
                if (amount is not null)
                    return amount;
            }
            else if (timeline.ValueKind == JsonValueKind.Array)
            {
                decimal? lastAmount = null;
                foreach (var item in timeline.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                        continue;

                    var amount = ReadFlexibleDecimal(item, "refund_amount");
                    if (amount is not null)
                        lastAmount = amount;
                }

                if (lastAmount is not null)
                    return lastAmount;
            }
        }

        return ReadFlexibleDecimal(resource, "amount");
    }

    private static string? ReadRefundStatus(JsonElement resource)
    {
        if (!resource.TryGetProperty("timeline", out var timeline))
            return null;

        if (timeline.ValueKind == JsonValueKind.Object)
            return ReadFlexibleString(timeline, "refund_status");

        if (timeline.ValueKind == JsonValueKind.Array)
        {
            string? lastStatus = null;
            foreach (var item in timeline.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var status = ReadFlexibleString(item, "refund_status");
                if (!string.IsNullOrWhiteSpace(status))
                    lastStatus = status;
            }

            return lastStatus;
        }

        return null;
    }

    private static bool IsCompletedRefundStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        var normalized = status.Trim().ToUpperInvariant();
        return normalized.Contains("PAID", StringComparison.Ordinal) ||
               normalized.Contains("SUCCESS", StringComparison.Ordinal) ||
               normalized.Contains("DONE", StringComparison.Ordinal) ||
               normalized.Contains("COMPLETED", StringComparison.Ordinal);
    }

    private static (string? Code, string? Message) ReadGraphQlError(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) ||
            errors.ValueKind != JsonValueKind.Array ||
            errors.GetArrayLength() == 0)
        {
            return (null, null);
        }

        var error = errors[0];
        if (error.ValueKind != JsonValueKind.Object)
            return (null, null);

        var message = ReadFlexibleString(error, "message");
        string? code = null;
        if (error.TryGetProperty("extensions", out var extensions) &&
            extensions.ValueKind == JsonValueKind.Object)
        {
            code = ReadFlexibleString(extensions, "code");
        }

        return (code, message);
    }

    private void ValidateConfiguration()
    {
        if (!options.Enabled)
            throw new InvalidOperationException("درگاه زرین‌پال غیرفعال است.");

        if (string.IsNullOrWhiteSpace(options.MerchantId))
            throw new InvalidOperationException("MerchantId زرین‌پال تنظیم نشده است.");

        if (!Uri.TryCreate(options.CallbackUrl, UriKind.Absolute, out var callback) ||
            (!string.Equals(callback.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(callback.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "CallbackUrl زرین‌پال باید یک آدرس مطلق HTTP/HTTPS باشد.");
        }

        _ = NormalizeCurrency(options.Currency);
        _ = ApiBaseUrl();

        if (!Uri.TryCreate(StartPayBaseUrl(), UriKind.Absolute, out _))
            throw new InvalidOperationException("آدرس StartPay زرین‌پال معتبر نیست.");
    }

    private long ToGatewayAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "مبلغ پرداخت باید بیشتر از صفر باشد.");

        if (decimal.Truncate(amount) != amount)
            throw new ArgumentException(
                "مبلغ پرداخت زرین‌پال باید عدد صحیح باشد.",
                nameof(amount));

        // Tatakae stores prices in toman. Zarinpal accepts both IRT and IRR.
        // If IRR is selected, convert toman to rial before sending the request.
        var gatewayAmount = Currency == "IRR"
            ? checked(amount * 10m)
            : amount;

        if (gatewayAmount > long.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "مبلغ پرداخت از محدوده مجاز بیشتر است.");

        return checked((long)gatewayAmount);
    }

    private Uri ApiBaseUrl()
    {
        var value = options.Sandbox
            ? options.SandboxApiBaseUrl
            : options.ProductionApiBaseUrl;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("آدرس API زرین‌پال معتبر نیست.");

        return uri;
    }

    private string StartPayBaseUrl()
        => options.Sandbox
            ? options.SandboxStartPayBaseUrl
            : options.ProductionStartPayBaseUrl;

    private static string AppendPaymentId(string callbackUrl, Guid paymentId)
    {
        var separator = callbackUrl.Contains("?", StringComparison.Ordinal)
            ? '&'
            : '?';

        return $"{callbackUrl}{separator}paymentId={paymentId:D}";
    }

    private static string NormalizeCurrency(string? currency)
    {
        var normalized = string.IsNullOrWhiteSpace(currency)
            ? "IRT"
            : currency.Trim().ToUpperInvariant();

        return normalized is "IRT" or "IRR"
            ? normalized
            : throw new InvalidOperationException(
                "واحد پول زرین‌پال فقط می‌تواند IRT یا IRR باشد.");
    }

    private static ParsedResponse ParseResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ParsedResponse(
                0,
                null,
                new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase));
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var values = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var code = 0;
        string? message = null;

        if (root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in data.EnumerateObject())
                values[property.Name] = property.Value.Clone();

            code = ReadInt(data, "code");
            message = ReadString(data, "message");
        }

        if ((code == 0 || string.IsNullOrWhiteSpace(message)) &&
            root.TryGetProperty("errors", out var errors))
        {
            var error = errors.ValueKind switch
            {
                JsonValueKind.Array when errors.GetArrayLength() > 0 => errors[0],
                JsonValueKind.Object => errors,
                _ => default
            };

            if (error.ValueKind == JsonValueKind.Object)
            {
                if (code == 0)
                    code = ReadInt(error, "code");

                message ??= ReadString(error, "message");
            }
        }

        return new ParsedResponse(code, message, values);
    }

    private static int ReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String &&
               int.TryParse(value.GetString(), out number)
            ? number
            : 0;
    }

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed record ParsedResponse(
        int Code,
        string? GatewayMessage,
        IReadOnlyDictionary<string, JsonElement> Values)
    {
        public string Message(string fallback)
            => string.IsNullOrWhiteSpace(GatewayMessage)
                ? fallback
                : GatewayMessage!;

        public string? StringValue(string name)
            => Values.TryGetValue(name, out var value)
                ? value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : value.ToString()
                : null;

        public long? LongValue(string name)
        {
            if (!Values.TryGetValue(name, out var value))
                return null;

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out var number))
            {
                return number;
            }

            return value.ValueKind == JsonValueKind.String &&
                   long.TryParse(value.GetString(), out number)
                ? number
                : null;
        }
    }
}
