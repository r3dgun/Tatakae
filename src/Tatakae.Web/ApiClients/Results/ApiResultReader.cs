using System.Net;
using System.Text.Json;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Web.ApiClients.Results;

public sealed class ApiResultReader : IApiResultReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ResultDto<T>> ReadAsync<T>(
        HttpResponseMessage response,
        string fallbackMessage,
        CancellationToken cancellationToken = default)
    {
        var body = await ReadBodyAsync(response, cancellationToken);

        if (LooksLikeResultDto(body))
        {
            var wrapped = TryDeserialize<ResultDto<T>>(body);
            if (wrapped is not null)
            {
                NormalizeHttpFailure(wrapped, response.StatusCode, fallbackMessage);
                return wrapped;
            }
        }

        if (response.IsSuccessStatusCode)
        {
            var data = TryDeserialize<T>(body);
            if (data is not null)
                return new ResultDto<T>().Success("اطلاعات با موفقیت دریافت شد.", data);

            return new ResultDto<T>().Failed(
                "پاسخ سرویس خالی یا نامعتبر است.",
                ResultStatus.Failure,
                "empty_or_invalid_response");
        }

        return BuildFailure<T>(response.StatusCode, body, fallbackMessage);
    }

    public async Task<ResultDto> ReadAsync(
        HttpResponseMessage response,
        string fallbackMessage,
        CancellationToken cancellationToken = default)
    {
        var body = await ReadBodyAsync(response, cancellationToken);

        if (LooksLikeResultDto(body))
        {
            var wrapped = TryDeserialize<ResultDto>(body);
            if (wrapped is not null)
            {
                NormalizeHttpFailure(wrapped, response.StatusCode, fallbackMessage);
                return wrapped;
            }
        }

        if (response.IsSuccessStatusCode)
            return new ResultDto().Success("عملیات با موفقیت انجام شد.");

        return BuildFailure(response.StatusCode, body, fallbackMessage);
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content is null) return string.Empty;
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static bool LooksLikeResultDto(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            return HasProperty(document.RootElement, "isSuccess")
                || HasProperty(document.RootElement, "status")
                || HasProperty(document.RootElement, "errorCode");
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static T? TryDeserialize<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return default;
        try { return JsonSerializer.Deserialize<T>(body, JsonOptions); }
        catch (JsonException) { return default; }
        catch (NotSupportedException) { return default; }
    }

    private static void NormalizeHttpFailure(ResultDto result, HttpStatusCode statusCode, string fallbackMessage)
    {
        if (result.IsSuccess && (int)statusCode < 400) return;
        result.IsSuccess = false;
        if (result.Status == ResultStatus.Success) result.Status = MapStatus(statusCode);
        if (string.IsNullOrWhiteSpace(result.Message)) result.Message = fallbackMessage;
        result.ErrorCode ??= $"http_{(int)statusCode}";
    }

    private static ResultDto<T> BuildFailure<T>(HttpStatusCode statusCode, string body, string fallbackMessage)
    {
        var failure = BuildFailure(statusCode, body, fallbackMessage);
        return new ResultDto<T>
        {
            IsSuccess = false,
            Status = failure.Status,
            Message = failure.Message,
            ErrorCode = failure.ErrorCode,
            Errors = failure.Errors,
            Data = default
        };
    }

    private static ResultDto BuildFailure(HttpStatusCode statusCode, string body, string fallbackMessage)
    {
        var message = ExtractMessage(body) ?? fallbackMessage;
        var errors = ExtractErrors(body);
        return new ResultDto
        {
            IsSuccess = false,
            Status = MapStatus(statusCode),
            Message = message,
            ErrorCode = $"http_{(int)statusCode}",
            Errors = errors
        };
    }

    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            foreach (var name in new[] { "message", "detail", "title" })
            {
                if (TryGetProperty(root, name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
        }
        catch (JsonException)
        {
            return body.Length <= 500 ? body : null;
        }
        return null;
    }

    private static IReadOnlyDictionary<string, string[]>? ExtractErrors(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (!TryGetProperty(document.RootElement, "errors", out var errorsElement)
                || errorsElement.ValueKind != JsonValueKind.Object)
                return null;

            var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in errorsElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    errors[property.Name] = property.Value.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
                }
            }
            return errors.Count == 0 ? null : errors;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool HasProperty(JsonElement element, string name)
        => TryGetProperty(element, name, out _);

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }
        if (element.TryGetProperty(name, out value)) return true;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static ResultStatus MapStatus(HttpStatusCode statusCode)
        => statusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ResultStatus.ValidationError,
            HttpStatusCode.Unauthorized => ResultStatus.Unauthorized,
            HttpStatusCode.Forbidden => ResultStatus.Forbidden,
            HttpStatusCode.NotFound => ResultStatus.NotFound,
            HttpStatusCode.Conflict => ResultStatus.Conflict,
            _ => ResultStatus.Failure
        };
}
