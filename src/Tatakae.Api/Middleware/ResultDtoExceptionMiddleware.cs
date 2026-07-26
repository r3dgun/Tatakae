using Tatakae.Application.Contracts.Common;

namespace Tatakae.Api.Middleware;

/// <summary>
/// Converts application/persistence failures into the single ResultDto API envelope.
/// Expected failures keep their Persian message and semantic status; unexpected
/// exceptions are logged and return a safe generic message.
/// </summary>
public sealed class ResultDtoExceptionMiddleware(
    RequestDelegate next,
    ILogger<ResultDtoExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (ResultDtoException exception)
        {
            var result = new ResultDto
            {
                IsSuccess = false,
                Status = exception.Status,
                Message = exception.Message,
                ErrorCode = exception.ErrorCode,
                Errors = exception.Errors
            };

            await WriteAsync(context, result);
        }
        catch (KeyNotFoundException exception)
        {
            await WriteAsync(
                context,
                new ResultDto().NotFound(exception.Message, "not_found"));
        }
        catch (ArgumentException exception)
        {
            await WriteAsync(
                context,
                new ResultDto().ValidationFailed(exception.Message, "validation_error"));
        }
        catch (UnauthorizedAccessException exception)
        {
            await WriteAsync(
                context,
                new ResultDto().Failed(exception.Message, ResultStatus.Unauthorized, "unauthorized"));
        }
        catch (InvalidOperationException exception)
        {
            await WriteAsync(
                context,
                new ResultDto().Conflict(exception.Message, "conflict"));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "خطای مدیریت‌نشده در درخواست {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteAsync(
                context,
                new ResultDto().Failed(
                    "خطایی در پردازش درخواست رخ داده است.",
                    ResultStatus.Failure,
                    "unhandled_error"));
        }
    }

    private static async Task WriteAsync(HttpContext context, ResultDto result)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException("پاسخ HTTP شروع شده و امکان نوشتن ResultDto وجود ندارد.");

        context.Response.Clear();
        context.Response.StatusCode = ToStatusCode(result.Status);
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    public static int ToStatusCode(ResultStatus status)
        => status switch
        {
            ResultStatus.Success => StatusCodes.Status200OK,
            ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
}
