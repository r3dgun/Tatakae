using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Infrastructure.Repositories;

public sealed class InMemoryOrderRepository(ILogger<InMemoryOrderRepository>? logger = null) : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _data = new(StoreSeed.CreateOrders().ToDictionary(x => x.Id));
    private readonly ConcurrentDictionary<Guid, List<OrderStatusHistoryDto>> _history = new();
    private readonly ILogger<InMemoryOrderRepository> _logger = logger ?? NullLogger<InMemoryOrderRepository>.Instance;

    public Task<ResultDto<IReadOnlyCollection<Order>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<Order>>();
        try { return Task.FromResult(result.Success("سفارش‌ها دریافت شدند.", _data.Values.ToArray())); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت سفارش‌ها"); return Task.FromResult(result.Failed("خطایی در دریافت سفارش‌ها رخ داده است.")); }
    }

    public Task<ResultDto<Order>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Order>();
        if (id == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه سفارش معتبر نیست."));
        try { return Task.FromResult(_data.TryGetValue(id, out var order) ? result.Success("سفارش دریافت شد.", order) : result.NotFound("سفارش پیدا نشد.")); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت سفارش {OrderId}", id); return Task.FromResult(result.Failed("خطایی در دریافت سفارش رخ داده است.")); }
    }

    public Task<ResultDto<Order>> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Order>();
        if (string.IsNullOrWhiteSpace(orderNumber)) return Task.FromResult(result.ValidationFailed("شماره سفارش معتبر نیست."));
        try
        {
            var order = _data.Values.SingleOrDefault(x => string.Equals(x.OrderNumber, orderNumber.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(order is null ? result.NotFound("سفارش پیدا نشد.") : result.Success("سفارش دریافت شد.", order));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت سفارش {OrderNumber}", orderNumber); return Task.FromResult(result.Failed("خطایی در دریافت سفارش رخ داده است.")); }
    }

    public Task<ResultDto<Order>> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Order>();
        if (order is null) return Task.FromResult(result.ValidationFailed("اطلاعات سفارش ارسال نشده است."));
        try { _data[order.Id] = order; return Task.FromResult(result.Success("سفارش ثبت شد.", order)); }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ثبت سفارش {OrderId}", order.Id); return Task.FromResult(result.Failed("خطایی در ثبت سفارش رخ داده است.")); }
    }

    public Task<ResultDto<Order>> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Order>();
        if (order is null) return Task.FromResult(result.ValidationFailed("اطلاعات سفارش ارسال نشده است."));
        try
        {
            if (!_data.ContainsKey(order.Id)) return Task.FromResult(result.NotFound("سفارش پیدا نشد."));
            _data[order.Id] = order;
            return Task.FromResult(result.Success("سفارش به‌روزرسانی شد.", order));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در به‌روزرسانی سفارش {OrderId}", order.Id); return Task.FromResult(result.Failed("خطایی در به‌روزرسانی سفارش رخ داده است.")); }
    }

    public Task<ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>();
        if (orderId == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه سفارش معتبر نیست."));
        try
        {
            IReadOnlyCollection<OrderStatusHistoryDto> rows = (_history.TryGetValue(orderId, out var items) ? items : []).OrderBy(x => x.HappenedAt).ToArray();
            return Task.FromResult(result.Success("تاریخچه سفارش دریافت شد.", rows));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در دریافت تاریخچه سفارش {OrderId}", orderId); return Task.FromResult(result.Failed("خطایی در دریافت تاریخچه سفارش رخ داده است.")); }
    }

    public Task<ResultDto<OrderStatusHistoryDto>> AddStatusHistoryAsync(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string title, string? note, string? trackingCode, string changedBy, CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<OrderStatusHistoryDto>();
        if (orderId == Guid.Empty) return Task.FromResult(result.ValidationFailed("شناسه سفارش معتبر نیست."));
        try
        {
            var row = new OrderStatusHistoryDto(Guid.NewGuid(), orderId, fromStatus?.ToString(), fromStatus is null ? null : Tatakae.Application.Services.OrderService.StatusLabel(fromStatus.Value), toStatus.ToString(), Tatakae.Application.Services.OrderService.StatusLabel(toStatus), title, note, trackingCode, changedBy, DateTimeOffset.UtcNow);
            _history.GetOrAdd(orderId, _ => []).Add(row);
            return Task.FromResult(result.Success("تاریخچه سفارش ثبت شد.", row));
        }
        catch (Exception ex) { _logger.LogError(ex, "خطا در ثبت تاریخچه سفارش {OrderId}", orderId); return Task.FromResult(result.Failed("خطایی در ثبت تاریخچه سفارش رخ داده است.")); }
    }
}
