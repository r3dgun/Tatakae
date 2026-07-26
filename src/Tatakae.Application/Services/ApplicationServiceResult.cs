using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

internal static class ApplicationServiceResult
{
    public static async Task<ResultDto<T>> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string successMessage,
        string failureMessage,
        string errorCode,
        ILogger logger,
        ResultStatus nullStatus = ResultStatus.NotFound,
        string? nullMessage = null,
        string? nullErrorCode = null)
    {
        try
        {
            var data = await operation();
            if (data is null)
            {
                return new ResultDto<T>().Failed(
                    nullMessage ?? "داده موردنظر پیدا نشد.",
                    nullStatus,
                    nullErrorCode ?? errorCode);
            }

            return new ResultDto<T>().Success(successMessage, data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ResultDtoException ex)
        {
            return new ResultDto<T>().Failed(ex);
        }
        catch (ArgumentException ex)
        {
            return new ResultDto<T>().ValidationFailed(ex.Message, errorCode);
        }
        catch (KeyNotFoundException ex)
        {
            return new ResultDto<T>().NotFound(ex.Message, errorCode);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ResultDto<T>().Unauthorized(ex.Message, errorCode);
        }
        catch (InvalidOperationException ex)
        {
            return new ResultDto<T>().Conflict(ex.Message, errorCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{FailureMessage} ErrorCode={ErrorCode}", failureMessage, errorCode);
            return new ResultDto<T>().Failed(failureMessage, ResultStatus.Failure, errorCode);
        }
    }

    public static async Task<ResultDto<T>> ExecuteNullableAsync<T>(
        Func<Task<T?>> operation,
        string successMessage,
        string failureMessage,
        string errorCode,
        ILogger logger,
        ResultStatus nullStatus = ResultStatus.NotFound,
        string? nullMessage = null,
        string? nullErrorCode = null)
        where T : class
    {
        try
        {
            var data = await operation();
            if (data is null)
            {
                return new ResultDto<T>().Failed(
                    nullMessage ?? "داده موردنظر پیدا نشد.",
                    nullStatus,
                    nullErrorCode ?? errorCode);
            }

            return new ResultDto<T>().Success(successMessage, data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ResultDtoException ex)
        {
            return new ResultDto<T>().Failed(ex);
        }
        catch (ArgumentException ex)
        {
            return new ResultDto<T>().ValidationFailed(ex.Message, errorCode);
        }
        catch (KeyNotFoundException ex)
        {
            return new ResultDto<T>().NotFound(ex.Message, errorCode);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ResultDto<T>().Unauthorized(ex.Message, errorCode);
        }
        catch (InvalidOperationException ex)
        {
            return new ResultDto<T>().Conflict(ex.Message, errorCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{FailureMessage} ErrorCode={ErrorCode}", failureMessage, errorCode);
            return new ResultDto<T>().Failed(failureMessage, ResultStatus.Failure, errorCode);
        }
    }

    public static async Task<ResultDto> ExecuteAsync(
        Func<Task> operation,
        string successMessage,
        string failureMessage,
        string errorCode,
        ILogger logger)
    {
        try
        {
            await operation();
            return new ResultDto().Success(successMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ResultDtoException ex)
        {
            return new ResultDto().Failed(ex);
        }
        catch (ArgumentException ex)
        {
            return new ResultDto().ValidationFailed(ex.Message, errorCode);
        }
        catch (KeyNotFoundException ex)
        {
            return new ResultDto().NotFound(ex.Message, errorCode);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ResultDto().Unauthorized(ex.Message, errorCode);
        }
        catch (InvalidOperationException ex)
        {
            return new ResultDto().Conflict(ex.Message, errorCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{FailureMessage} ErrorCode={ErrorCode}", failureMessage, errorCode);
            return new ResultDto().Failed(failureMessage, ResultStatus.Failure, errorCode);
        }
    }
}
