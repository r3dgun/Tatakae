using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Shipping;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlShippingMethodRepository(
    TatakaeDbContext db,
    ILogger<SqlShippingMethodRepository>? logger = null) : IShippingMethodRepository
{
    private readonly ILogger<SqlShippingMethodRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlShippingMethodRepository>.Instance;

    private async Task<IReadOnlyCollection<ShippingMethodDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.ShippingMethods.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Title).Select(ToDtoExpression).ToArrayAsync(cancellationToken);

    private async Task<IReadOnlyCollection<ShippingMethodDto>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await db.ShippingMethods.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.IsDefault).ThenBy(x => x.SortOrder).ThenBy(x => x.BasePrice).Select(ToDtoExpression).ToArrayAsync(cancellationToken);

    private async Task<ShippingMethodDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await db.ShippingMethods.AsNoTracking().Where(x => x.Code == code.Trim()).Select(ToDtoExpression).FirstOrDefaultAsync(cancellationToken);

    private async Task<ShippingMethodDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.ShippingMethods.AsNoTracking().Where(x => x.Id == id).Select(ToDtoExpression).FirstOrDefaultAsync(cancellationToken);

    private async Task<ShippingMethodDto> UpsertAsync(Guid? id, UpsertManualShippingMethodRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        var duplicate = await db.ShippingMethods
            .IgnoreQueryFilters()
            .AnyAsync(x => !x.IsRemoved && x.Code == code && (!id.HasValue || x.Id != id.Value), cancellationToken);
        if (duplicate) throw new InvalidOperationException("کد روش ارسال تکراری است.");

        ShippingMethodDbRecord entity;
        if (id.HasValue)
        {
            entity = await db.ShippingMethods
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("روش ارسال پیدا نشد.");
            db.Restore(entity);
        }
        else
        {
            entity = new ShippingMethodDbRecord { Id = Guid.NewGuid(), Carrier = ShippingCarrier.OwnCourier };
            db.ShippingMethods.Add(entity);
        }

        entity.Code = code;
        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.BasePrice = request.BasePrice;
        entity.FreeShippingThreshold = request.FreeShippingThreshold;
        entity.MinDeliveryDays = request.EstimatedMinDays;
        entity.MaxDeliveryDays = request.EstimatedMaxDays;
        entity.SupportsCashOnDelivery = request.SupportsCashOnDelivery;
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        if (entity.IsDefault)
        {
            await db.ShippingMethods.Where(x => x.Id != entity.Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.ShippingMethods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("روش ارسال پیدا نشد.");
        db.SoftDelete(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<ShippingMethodDbRecord, ShippingMethodDto>> ToDtoExpression = x => new ShippingMethodDto(
        x.Id, x.Code, x.Title, x.Description, x.BasePrice, x.BasePrice, x.FreeShippingThreshold, x.MinDeliveryDays, x.MaxDeliveryDays, x.SupportsCashOnDelivery, x.IsDefault, x.IsActive, x.IsActive);

    private static ShippingMethodDto ToDto(ShippingMethodDbRecord x) => new(
        x.Id, x.Code, x.Title, x.Description, x.BasePrice, x.BasePrice, x.FreeShippingThreshold, x.MinDeliveryDays, x.MaxDeliveryDays, x.SupportsCashOnDelivery, x.IsDefault, x.IsActive, x.IsActive);
}
