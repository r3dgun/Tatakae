using Tatakae.Application.Contracts.Common;

namespace Tatakae.Web.ApiClients.Results;

public sealed class ApiClientException : Exception
{
    public ApiClientException(ResultDto result)
        : base(string.IsNullOrWhiteSpace(result.Message) ? "درخواست به سرویس ناموفق بود." : result.Message)
    {
        Result = result;
    }

    public ResultDto Result { get; }
}
