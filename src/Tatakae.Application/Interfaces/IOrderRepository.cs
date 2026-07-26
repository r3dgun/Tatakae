using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Interfaces;

public interface IOrderRepository
{
    Task<ResultDto<IReadOnlyCollection<Order>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResultDto<Order>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultDto<Order>> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<ResultDto<Order>> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<ResultDto<Order>> UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ResultDto<OrderStatusHistoryDto>> AddStatusHistoryAsync(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string title, string? note, string? trackingCode, string changedBy, CancellationToken cancellationToken = default);
}
