using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Common;

public sealed record ApiResultDto<T>(bool Succeeded, T? Data, IReadOnlyCollection<ApiErrorDto> Errors)
{
    public static ApiResultDto<T> Success(T data) => new(true, data, Array.Empty<ApiErrorDto>());
    public static ApiResultDto<T> Failure(params ApiErrorDto[] errors) => new(false, default, errors);
}

public sealed record ApiErrorDto(string Code, string Message, string? Field = null);

public sealed record MoneyDto(decimal Amount, string CurrencyCode, string Formatted);

public sealed record SelectOptionDto(string Value, string Label, bool IsDisabled = false, string? Hint = null);

public sealed record BreadcrumbItemDto(string Label, string Url, int Position);

public sealed class PaginationQueryDto
{
    [Range(1, 2000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class DateRangeQueryDto
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
