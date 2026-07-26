using Tatakae.Application.Contracts.Common;

namespace Tatakae.Web.ApiClients.Results;

public interface IApiClientTransport
{
    Task<ResultDto<T>> GetResultAsync<T>(
        string url,
        string fallbackMessage,
        CancellationToken cancellationToken = default);

    Task<ResultDto<T>> SendResultAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        string fallbackMessage,
        CancellationToken cancellationToken = default);

    Task<ResultDto> SendResultAsync(
        HttpMethod method,
        string url,
        object? body,
        string fallbackMessage,
        CancellationToken cancellationToken = default);
}
