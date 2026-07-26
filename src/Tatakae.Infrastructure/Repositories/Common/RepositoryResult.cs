using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Infrastructure.Repositories.Common;

/// <summary>
/// Single persistence result policy used by every SQL repository.
/// Expected domain/persistence failures preserve their Persian message and
/// semantic status; unexpected failures are logged and return a safe message.
/// </summary>
internal static class RepositoryResult
{
    public static async Task<ResultDto<T>> QueryAsync<T>(
        Func<Task<T>> action,
        ILogger logger,
        string successMessage,
        string failureMessage,
        string operation)
    {
        var result = new ResultDto<T>();
        try
        {
            return result.Success(successMessage, await action());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "تداخل همزمانی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "اطلاعات هم‌زمان توسط درخواست دیگری تغییر کرده است. دوباره تلاش کنید.",
                "database_concurrency_conflict");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "تداخل ذخیره‌سازی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "ذخیره اطلاعات به‌دلیل تداخل داده‌ها انجام نشد.",
                "database_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Repository هنگام {Operation}", operation);
            return result.Failed(failureMessage, ResultStatus.Failure, "repository_failure");
        }
    }

    public static async Task<ResultDto<T>> FindAsync<T>(
        Func<Task<T?>> action,
        ILogger logger,
        string successMessage,
        string notFoundMessage,
        string failureMessage,
        string operation)
        where T : class
    {
        var result = new ResultDto<T>();
        try
        {
            var data = await action();
            return data is null
                ? result.NotFound(notFoundMessage, "not_found")
                : result.Success(successMessage, data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "تداخل همزمانی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "اطلاعات هم‌زمان توسط درخواست دیگری تغییر کرده است. دوباره تلاش کنید.",
                "database_concurrency_conflict");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "تداخل ذخیره‌سازی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "ذخیره اطلاعات به‌دلیل تداخل داده‌ها انجام نشد.",
                "database_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Repository هنگام {Operation}", operation);
            return result.Failed(failureMessage, ResultStatus.Failure, "repository_failure");
        }
    }

    public static async Task<ResultDto> CommandAsync(
        Func<Task<bool>> action,
        ILogger logger,
        string successMessage,
        string notFoundMessage,
        string failureMessage,
        string operation)
    {
        var result = new ResultDto();
        try
        {
            return await action()
                ? result.Success(successMessage)
                : result.NotFound(notFoundMessage, "not_found");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "تداخل همزمانی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "اطلاعات هم‌زمان توسط درخواست دیگری تغییر کرده است. دوباره تلاش کنید.",
                "database_concurrency_conflict");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "تداخل ذخیره‌سازی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "ذخیره اطلاعات به‌دلیل تداخل داده‌ها انجام نشد.",
                "database_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Repository هنگام {Operation}", operation);
            return result.Failed(failureMessage, ResultStatus.Failure, "repository_failure");
        }
    }

    public static async Task<ResultDto<T>> MutationAsync<T>(
        Func<Task<T>> action,
        ILogger logger,
        string successMessage,
        string failureMessage,
        string operation)
    {
        var result = new ResultDto<T>();
        try
        {
            return result.Success(successMessage, await action());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "تداخل همزمانی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "اطلاعات هم‌زمان توسط درخواست دیگری تغییر کرده است. دوباره تلاش کنید.",
                "database_concurrency_conflict");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "تداخل ذخیره‌سازی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "ذخیره اطلاعات به‌دلیل تداخل داده‌ها انجام نشد.",
                "database_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Repository هنگام {Operation}", operation);
            return result.Failed(failureMessage, ResultStatus.Failure, "repository_failure");
        }
    }

    public static async Task<ResultDto> CommandAsync(
        Func<Task> action,
        ILogger logger,
        string successMessage,
        string failureMessage,
        string operation)
    {
        var result = new ResultDto();
        try
        {
            await action();
            return result.Success(successMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "تداخل همزمانی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "اطلاعات هم‌زمان توسط درخواست دیگری تغییر کرده است. دوباره تلاش کنید.",
                "database_concurrency_conflict");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "تداخل ذخیره‌سازی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "ذخیره اطلاعات به‌دلیل تداخل داده‌ها انجام نشد.",
                "database_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Repository هنگام {Operation}", operation);
            return result.Failed(failureMessage, ResultStatus.Failure, "repository_failure");
        }
    }

    public static async Task<ResultDto<T>> MutationAsync<T>(
        Func<Task> action,
        T data,
        ILogger logger,
        string successMessage,
        string failureMessage,
        string operation)
    {
        var result = new ResultDto<T>();
        try
        {
            await action();
            return result.Success(successMessage, data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            return result.NotFound(ex.Message, "not_found");
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "validation_error");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "conflict");
        }
        catch (UnauthorizedAccessException ex)
        {
            return result.Unauthorized(ex.Message, "unauthorized");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "تداخل همزمانی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "اطلاعات هم‌زمان توسط درخواست دیگری تغییر کرده است. دوباره تلاش کنید.",
                "database_concurrency_conflict");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "تداخل ذخیره‌سازی در Repository هنگام {Operation}", operation);
            return result.Conflict(
                "ذخیره اطلاعات به‌دلیل تداخل داده‌ها انجام نشد.",
                "database_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Repository هنگام {Operation}", operation);
            return result.Failed(failureMessage, ResultStatus.Failure, "repository_failure");
        }
    }
}
