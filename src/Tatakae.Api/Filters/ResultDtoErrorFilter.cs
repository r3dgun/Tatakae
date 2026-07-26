using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Api.Filters;

/// <summary>
/// Normalizes legacy controller errors (ProblemDetails, anonymous message objects,
/// NotFound(), BadRequest(), ...) to ResultDto without changing successful API payloads.
/// </summary>
public sealed class ResultDtoErrorFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        var statusCode = ResolveStatusCode(context.Result);
        if (statusCode is null || statusCode < StatusCodes.Status400BadRequest)
        {
            await next();
            return;
        }

        if (context.Result is ObjectResult { Value: ResultDto })
        {
            await next();
            return;
        }

        var value = (context.Result as ObjectResult)?.Value;
        var message = ResolveMessage(value, statusCode.Value);
        var status = ToResultStatus(statusCode.Value);
        var errorCode = ResolveErrorCode(status, statusCode.Value);

        ResultDto result = value is ValidationProblemDetails validationDetails
            ? new ResultDto().ValidationFailed(
                message,
                validationDetails.Errors.ToDictionary(
                    item => item.Key,
                    item => item.Value),
                errorCode)
            : new ResultDto().Failed(message, status, errorCode);

        context.Result = new ObjectResult(result)
        {
            StatusCode = statusCode
        };

        await next();
    }

    private static int? ResolveStatusCode(IActionResult result)
        => result switch
        {
            ObjectResult objectResult => objectResult.StatusCode,
            StatusCodeResult statusResult => statusResult.StatusCode,
            _ => null
        };

    private static string ResolveMessage(object? value, int statusCode)
    {
        if (value is ProblemDetails details)
        {
            if (!string.IsNullOrWhiteSpace(details.Detail))
                return details.Detail;

            if (!string.IsNullOrWhiteSpace(details.Title))
                return details.Title;
        }

        if (value is string text && !string.IsNullOrWhiteSpace(text))
            return text;

        if (value is not null)
        {
            var property = value.GetType().GetProperty("Message")
                           ?? value.GetType().GetProperty("message");
            if (property?.GetValue(value) is string message && !string.IsNullOrWhiteSpace(message))
                return message;
        }

        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "اطلاعات درخواست معتبر نیست.",
            StatusCodes.Status401Unauthorized => "برای انجام این عملیات باید وارد حساب کاربری شوید.",
            StatusCodes.Status403Forbidden => "اجازه انجام این عملیات را ندارید.",
            StatusCodes.Status404NotFound => "اطلاعات موردنظر پیدا نشد.",
            StatusCodes.Status409Conflict => "عملیات با وضعیت فعلی اطلاعات تداخل دارد.",
            _ => "خطایی در پردازش درخواست رخ داده است."
        };
    }

    internal static ResultStatus ToResultStatus(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity => ResultStatus.ValidationError,
            StatusCodes.Status401Unauthorized => ResultStatus.Unauthorized,
            StatusCodes.Status403Forbidden => ResultStatus.Forbidden,
            StatusCodes.Status404NotFound => ResultStatus.NotFound,
            StatusCodes.Status409Conflict => ResultStatus.Conflict,
            _ => ResultStatus.Failure
        };

    private static string ResolveErrorCode(ResultStatus status, int statusCode)
        => status switch
        {
            ResultStatus.ValidationError => "validation_error",
            ResultStatus.Unauthorized => "unauthorized",
            ResultStatus.Forbidden => "forbidden",
            ResultStatus.NotFound => "not_found",
            ResultStatus.Conflict => "conflict",
            _ => $"http_{statusCode}"
        };
}
