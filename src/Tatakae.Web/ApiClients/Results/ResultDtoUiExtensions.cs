using Tatakae.Application.Contracts.Common;

namespace Tatakae.Web.ApiClients.Results;

/// <summary>
/// Presentation helpers used at Razor call sites. API clients themselves always
/// expose ResultDto; these helpers only adapt a successful result to existing UI
/// state while preserving failures inside ApiClientException for the global error boundary.
/// </summary>
public static class ResultDtoUiExtensions
{
    public static T RequireUiData<T>(this ResultDto<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
            throw new ApiClientException(result);

        if (result.Data is null)
        {
            throw new ApiClientException(new ResultDto().Failed(
                "پاسخ سرویس فاقد داده معتبر است.",
                ResultStatus.Failure,
                "empty_response_data"));
        }

        return result.Data;
    }

    public static T? OptionalData<T>(this ResultDto<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
            return result.Data;

        if (result.Status == ResultStatus.NotFound)
            return default;

        throw new ApiClientException(result);
    }

    public static void EnsureUiSuccess(this ResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            throw new ApiClientException(result);
    }
}
