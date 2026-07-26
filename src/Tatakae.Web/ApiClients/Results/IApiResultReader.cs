using Tatakae.Application.Contracts.Common;

namespace Tatakae.Web.ApiClients.Results;

public interface IApiResultReader
{
    Task<ResultDto<T>> ReadAsync<T>(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken = default);
    Task<ResultDto> ReadAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken = default);
}
