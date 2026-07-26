using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Legal;
using Tatakae.Application.Interfaces;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed class SqlStorePolicyPageReader(TatakaeDbContext db) : IStorePolicyPageReader
{
    public async Task<IReadOnlyCollection<StorePolicyPageDto>> GetPublishedAsync(CancellationToken cancellationToken = default)
        => (await db.StorePolicyPages.AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken))
            .Select(x => new StorePolicyPageDto(x.Id, x.Slug, x.Title, x.Summary, x.Body, x.SeoTitle, x.SeoDescription, x.IsPublished, x.SortOrder, x.UpdatedAt))
            .ToArray();
}
