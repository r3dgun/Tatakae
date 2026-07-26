using Tatakae.Application.Contracts.Legal;

namespace Tatakae.Application.Interfaces;

public interface IStorePolicyPageReader
{
    Task<IReadOnlyCollection<StorePolicyPageDto>> GetPublishedAsync(CancellationToken cancellationToken = default);
}
