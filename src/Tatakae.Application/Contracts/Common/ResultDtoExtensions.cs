namespace Tatakae.Application.Contracts.Common;

/// <summary>
/// Bridges ResultDto-based repositories with application use cases while
/// preserving the original Persian error message, semantic status, error code,
/// and field-level validation errors.
/// </summary>
public static class ResultDtoExtensions
{
    public static void EnsureSuccess(this ResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
            throw ResultDtoException.From(result);
    }

    public static T RequireData<T>(this ResultDto<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
            throw ResultDtoException.From(result);

        if (result.Data is null)
        {
            throw new ResultDtoException(
                string.IsNullOrWhiteSpace(result.Message)
                    ? "داده موردنظر دریافت نشد."
                    : result.Message,
                ResultStatus.NotFound,
                result.ErrorCode,
                result.Errors);
        }

        return result.Data;
    }

    public static T? DataOrDefault<T>(this ResultDto<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result.Data;

        if (result.Status == ResultStatus.NotFound)
            return default;

        throw ResultDtoException.From(result);
    }

    public static ResultDto<TOut> ForwardFailure<TOut>(this ResultDto result)
        => new()
        {
            IsSuccess = false,
            Status = result.Status,
            Message = result.Message,
            ErrorCode = result.ErrorCode,
            Errors = result.Errors,
            Data = default
        };

    public static ResultDto ForwardFailure(this ResultDto result)
        => new()
        {
            IsSuccess = false,
            Status = result.Status,
            Message = result.Message,
            ErrorCode = result.ErrorCode,
            Errors = result.Errors
        };
}

public sealed class ResultDtoException(
    string message,
    ResultStatus status = ResultStatus.Failure,
    string? errorCode = null,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(message)
{
    public ResultStatus Status { get; } = status;

    public string? ErrorCode { get; } = errorCode;

    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;

    public static ResultDtoException From(ResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ResultDtoException(
            result.Message,
            result.Status,
            result.ErrorCode,
            result.Errors);
    }
}
