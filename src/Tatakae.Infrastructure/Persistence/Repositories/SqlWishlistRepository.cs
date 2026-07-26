using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Interfaces;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlWishlistRepository(
    TatakaeDbContext db,
    ILogger<SqlWishlistRepository>? logger = null) : IWishlistRepository
{
    private readonly ILogger<SqlWishlistRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlWishlistRepository>.Instance;

    private async Task<IReadOnlyCollection<WishlistEntry>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await db.Wishlists.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new WishlistEntry(x.Id, x.CustomerId, x.ProductId, x.CreatedAt))
            .ToArrayAsync(cancellationToken);

    private Task<bool> ExistsAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
        => db.Wishlists.AnyAsync(x => x.CustomerId == customerId && x.ProductId == productId, cancellationToken);

    private async Task AddAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
    {
        var existing = await db.Wishlists
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.CustomerId == customerId && x.ProductId == productId, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsRemoved)
            {
                db.Restore(existing);
                existing.CreatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        db.Wishlists.Add(new WishlistDbRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ProductId = productId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
    {
        var items = await db.Wishlists.Where(x => x.CustomerId == customerId && x.ProductId == productId).ToListAsync(cancellationToken);
        if (items.Count == 0) throw new KeyNotFoundException("علاقه‌مندی پیدا نشد.");
        db.SoftDeleteRange(items);
        await db.SaveChangesAsync(cancellationToken);
    }
}
