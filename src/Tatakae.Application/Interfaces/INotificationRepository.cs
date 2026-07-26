using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Interfaces;

public interface INotificationRepository
{
    Task<ResultDto<NotificationDto>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<NotificationDto>>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<NotificationDto>>> GetForAdminAsync(AdminNotificationFilter filter, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CountUnreadAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> MarkReadAsync(Guid customerId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<ResultDto> MarkAllReadAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ResultDto<NotificationDto>> UpdateStatusAsync(Guid id, NotificationStatus status, string? failureReason, CancellationToken cancellationToken = default);
}
