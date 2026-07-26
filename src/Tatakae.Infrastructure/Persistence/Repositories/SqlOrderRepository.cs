using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Persistence.Mappers;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlOrderRepository(
    TatakaeDbContext db,
    ILogger<SqlOrderRepository>? logger = null) : IOrderRepository
{
    private readonly ILogger<SqlOrderRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlOrderRepository>.Instance;

    private async Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await Query().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken)).Select(x => x.ToDomain()).ToArray();

    private async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => (await Query().SingleOrDefaultAsync(x => x.Id == id, cancellationToken))?.ToDomain();

    private async Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        => (await Query().SingleOrDefaultAsync(x => x.OrderNumber == orderNumber.Trim().ToUpper(), cancellationToken))?.ToDomain();

    private async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        db.Orders.Add(order.ToRecord());
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var existing = await db.Orders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == order.Id, cancellationToken);

        if (existing is null)
        {
            db.Orders.Add(order.ToRecord());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.OrderNumber = order.OrderNumber;
        existing.CustomerId = order.CustomerId;
        existing.CustomerName = order.CustomerName;
        existing.CustomerMobile = order.CustomerMobile;
        existing.ShippingRecipientName = order.ShippingAddress.RecipientName;
        existing.ShippingMobile = order.ShippingAddress.Mobile;
        existing.ShippingProvince = order.ShippingAddress.Province;
        existing.ShippingCity = order.ShippingAddress.City;
        existing.ShippingPostalCode = order.ShippingAddress.PostalCode;
        existing.ShippingAddressLine = order.ShippingAddress.AddressLine;
        existing.ShippingPlaque = order.ShippingAddress.Plaque;
        existing.ShippingUnit = order.ShippingAddress.Unit;
        existing.Status = order.Status;
        existing.PaymentStatus = order.PaymentStatus;
        existing.Subtotal = order.Subtotal;
        existing.ShippingAmount = order.ShippingAmount;
        existing.ShippingMethodCode = order.ShippingMethodCode;
        existing.ShippingMethodTitle = order.ShippingMethodTitle;
        existing.DiscountAmount = order.DiscountAmount;
        existing.Total = order.Total;
        existing.TrackingCode = order.TrackingCode;
        existing.AdminNote = order.AdminNote;

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var rows = await db.OrderStatusHistory.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.HappenedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new OrderStatusHistoryDto(
            x.Id,
            x.OrderId,
            x.FromStatus?.ToString(),
            x.FromStatus is null ? null : Tatakae.Application.Services.OrderService.StatusLabel(x.FromStatus.Value),
            x.ToStatus.ToString(),
            Tatakae.Application.Services.OrderService.StatusLabel(x.ToStatus),
            x.Title,
            x.Note,
            x.TrackingCode,
            x.ChangedBy,
            x.HappenedAt)).ToArray();
    }

    private async Task AddStatusHistoryAsync(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string title, string? note, string? trackingCode, string changedBy, CancellationToken cancellationToken = default)
    {
        db.OrderStatusHistory.Add(new OrderStatusHistoryDbRecord
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Title = title,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            TrackingCode = string.IsNullOrWhiteSpace(trackingCode) ? null : trackingCode.Trim(),
            ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "system" : changedBy.Trim(),
            HappenedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<OrderDbRecord> Query() => db.Orders.AsNoTracking().Include(x => x.Lines).Include(x => x.StatusHistory);
}
