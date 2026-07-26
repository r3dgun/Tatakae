using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Seo;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Persistence.Mappers;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlCategoryRepository(
    TatakaeDbContext db,
    ILogger<SqlCategoryRepository>? logger = null) : ICategoryRepository
{
    private readonly ILogger<SqlCategoryRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlCategoryRepository>.Instance;

    private async Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await db.Categories.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken)).Select(x => x.ToDomain()).ToArray();

    private async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => (await db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken))?.ToDomain();

    private async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = SeoSlug.Normalize(slug);
        return (await db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == normalizedSlug, cancellationToken))?.ToDomain();
    }

    private async Task UpsertAsync(Category category, CancellationToken cancellationToken = default)
    {
        var incoming = category.ToRecord();
        var existing = await db.Categories
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == category.Id, cancellationToken);

        if (existing is null)
        {
            db.Categories.Add(incoming);
        }
        else
        {
            existing.Name = incoming.Name;
            existing.Slug = incoming.Slug;
            existing.Description = incoming.Description;
            existing.CoverImageUrl = incoming.CoverImageUrl;
            existing.SeoMetaTitle = incoming.SeoMetaTitle;
            existing.SeoMetaDescription = incoming.SeoMetaDescription;
            existing.SeoCanonicalPath = incoming.SeoCanonicalPath;
            existing.SeoOpenGraphImageUrl = incoming.SeoOpenGraphImageUrl;
            existing.SeoAllowIndex = incoming.SeoAllowIndex;
            existing.SeoAllowFollow = incoming.SeoAllowFollow;
            existing.ParentId = incoming.ParentId;
            existing.SortOrder = incoming.SortOrder;
            existing.IsActive = incoming.IsActive;
            db.Restore(existing);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record is null) throw new KeyNotFoundException("دسته‌بندی پیدا نشد.");

        db.SoftDelete(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
