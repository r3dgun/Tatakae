using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Persistence.Mappers;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlCouponRepository(
    TatakaeDbContext db,
    ILogger<SqlCouponRepository>? logger = null) : ICouponRepository
{
    private readonly ILogger<SqlCouponRepository> _resultLogger =
        logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlCouponRepository>.Instance;

    private async Task<IReadOnlyCollection<Coupon>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => (await db.Coupons
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .ToListAsync(cancellationToken))
            .Select(x => x.ToDomain())
            .ToArray();

    private async Task<Coupon?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var record = await db.Coupons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);

        return record?.ToDomain();
    }

    private async Task<Coupon?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.Coupons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    private async Task UpsertAsync(
        Coupon coupon,
        CancellationToken cancellationToken = default)
    {
        var incoming = coupon.ToRecord();
        var existing = await db.Coupons
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == coupon.Id, cancellationToken);

        if (existing is null)
        {
            db.Coupons.Add(incoming);
        }
        else
        {
            existing.Code = incoming.Code;
            existing.Type = incoming.Type;
            existing.Value = incoming.Value;
            existing.StartsAt = incoming.StartsAt;
            existing.EndsAt = incoming.EndsAt;
            existing.UsageLimit = incoming.UsageLimit;
            existing.UsageCount = incoming.UsageCount;
            existing.MinimumOrderAmount = incoming.MinimumOrderAmount;
            existing.IsActive = incoming.IsActive;
            db.Restore(existing);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.Coupons
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("کد تخفیف پیدا نشد.");

        db.SoftDelete(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
