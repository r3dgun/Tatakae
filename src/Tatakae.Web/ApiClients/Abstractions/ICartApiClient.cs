using Tatakae.Application.Contracts.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tatakae.Application.Contracts.Cart;
using Tatakae.Web.Models;

namespace Tatakae.Web.ApiClients.Abstractions;

public interface ICartApiClient
{
    Task<ResultDto> MergeAsync(IReadOnlyCollection<CartLine> lines, CancellationToken cancellationToken = default);
    Task<ResultDto> ClearAsync(CancellationToken cancellationToken = default);
}
