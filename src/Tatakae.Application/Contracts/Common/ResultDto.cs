namespace Tatakae.Application.Contracts.Common;

/// <summary>
/// Describes the semantic outcome of an application or persistence operation.
/// API layers can map this value to the appropriate HTTP status code without
/// parsing the Persian message text.
/// </summary>
public enum ResultStatus
{
    Success = 0,
    ValidationError = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Failure = 6
}

/// <summary>
/// Standard result for commands that do not return data.
/// </summary>
public class ResultDto
{
    public bool IsSuccess { get; set; }

    public ResultStatus Status { get; set; } = ResultStatus.Failure;

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }

    public ResultDto Failed(
        string message,
        ResultStatus status = ResultStatus.Failure,
        string? errorCode = null)
        => new()
        {
            IsSuccess = false,
            Status = status,
            Message = message,
            ErrorCode = errorCode
        };

    public ResultDto Failed(ResultDtoException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ResultDto
        {
            IsSuccess = false,
            Status = exception.Status,
            Message = exception.Message,
            ErrorCode = exception.ErrorCode,
            Errors = exception.Errors
        };
    }

    public ResultDto ValidationFailed(string message, string? errorCode = null)
        => Failed(message, ResultStatus.ValidationError, errorCode);

    public ResultDto ValidationFailed(
        string message,
        IReadOnlyDictionary<string, string[]> errors,
        string? errorCode = null)
        => new()
        {
            IsSuccess = false,
            Status = ResultStatus.ValidationError,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors
        };

    public ResultDto NotFound(string message, string? errorCode = null)
        => Failed(message, ResultStatus.NotFound, errorCode);

    public ResultDto Conflict(string message, string? errorCode = null)
        => Failed(message, ResultStatus.Conflict, errorCode);

    public ResultDto Unauthorized(string message, string? errorCode = null)
        => Failed(message, ResultStatus.Unauthorized, errorCode);

    public ResultDto Forbidden(string message, string? errorCode = null)
        => Failed(message, ResultStatus.Forbidden, errorCode);

    public ResultDto Success(string message)
        => new()
        {
            IsSuccess = true,
            Status = ResultStatus.Success,
            Message = message
        };
}

/// <summary>
/// Standard result for queries and commands that return data.
/// </summary>
public class ResultDto<T> : ResultDto
{
    public T? Data { get; set; }

    public new ResultDto<T> Failed(
        string message,
        ResultStatus status = ResultStatus.Failure,
        string? errorCode = null)
        => new()
        {
            IsSuccess = false,
            Status = status,
            Message = message,
            ErrorCode = errorCode,
            Data = default
        };

    public new ResultDto<T> Failed(ResultDtoException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ResultDto<T>
        {
            IsSuccess = false,
            Status = exception.Status,
            Message = exception.Message,
            ErrorCode = exception.ErrorCode,
            Errors = exception.Errors,
            Data = default
        };
    }

    public new ResultDto<T> ValidationFailed(string message, string? errorCode = null)
        => Failed(message, ResultStatus.ValidationError, errorCode);

    public new ResultDto<T> ValidationFailed(
        string message,
        IReadOnlyDictionary<string, string[]> errors,
        string? errorCode = null)
        => new()
        {
            IsSuccess = false,
            Status = ResultStatus.ValidationError,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
            Data = default
        };

    public new ResultDto<T> NotFound(string message, string? errorCode = null)
        => Failed(message, ResultStatus.NotFound, errorCode);

    public new ResultDto<T> Conflict(string message, string? errorCode = null)
        => Failed(message, ResultStatus.Conflict, errorCode);

    public new ResultDto<T> Unauthorized(string message, string? errorCode = null)
        => Failed(message, ResultStatus.Unauthorized, errorCode);

    public new ResultDto<T> Forbidden(string message, string? errorCode = null)
        => Failed(message, ResultStatus.Forbidden, errorCode);

    public ResultDto<T> Success(string message, T data)
        => new()
        {
            IsSuccess = true,
            Status = ResultStatus.Success,
            Message = message,
            Data = data
        };
}
