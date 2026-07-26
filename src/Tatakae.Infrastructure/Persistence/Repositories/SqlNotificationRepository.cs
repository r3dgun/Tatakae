using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlNotificationRepository(
    TatakaeDbContext db,
    ILogger<SqlNotificationRepository>? logger = null) : INotificationRepository
{
    private readonly ILogger<SqlNotificationRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlNotificationRepository>.Instance;

    private async Task<NotificationDto> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var channel = ParseEnum<NotificationChannel>(request.Channel, NotificationChannel.InApp);
        var type = ParseEnum<NotificationType>(request.Type, NotificationType.Manual);
        var customerId = request.CustomerId;
        if (customerId is null && !string.IsNullOrWhiteSpace(request.CustomerMobile))
        {
            var normalized = NormalizeMobile(request.CustomerMobile);
            customerId = await db.Customers
                .Where(x => x.Mobile == normalized || x.Mobile == request.CustomerMobile)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var record = new NotificationDbRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Channel = channel,
            Type = type,
            Status = request.MarkAsSent ? NotificationStatus.Sent : NotificationStatus.Queued,
            Recipient = string.IsNullOrWhiteSpace(request.Recipient) ? request.CustomerMobile?.Trim() ?? "admin" : request.Recipient.Trim(),
            Subject = request.Subject.Trim(),
            Body = request.Body.Trim(),
            RelatedOrderId = request.RelatedOrderId,
            RelatedOrderNumber = request.RelatedOrderNumber,
            RelatedProductId = request.RelatedProductId,
            ActionUrl = request.ActionUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            SentAt = request.MarkAsSent ? DateTimeOffset.UtcNow : null,
            IsRead = false
        };

        db.Notifications.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return Map(record);
    }

    private async Task<IReadOnlyCollection<NotificationDto>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        => (await db.Notifications.AsNoTracking()
            .Where(x => x.CustomerId == customerId && !x.IsRemoved)
            .OrderByDescending(x => x.CreatedAt)
            .Take(80)
            .ToListAsync(cancellationToken))
            .Select(Map)
            .ToArray();

    private async Task<IReadOnlyCollection<NotificationDto>> GetForAdminAsync(AdminNotificationFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Notifications.AsNoTracking().Where(x => !x.IsRemoved);

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<NotificationStatus>(filter.Status, true, out var status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Channel) && Enum.TryParse<NotificationChannel>(filter.Channel, true, out var channel))
            query = query.Where(x => x.Channel == channel);
        if (!string.IsNullOrWhiteSpace(filter.Type) && Enum.TryParse<NotificationType>(filter.Type, true, out var type))
            query = query.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Recipient.Contains(search) || x.Subject.Contains(search) || x.Body.Contains(search) || (x.RelatedOrderNumber != null && x.RelatedOrderNumber.Contains(search)));
        }

        var take = Math.Clamp(filter.Take, 1, 300);
        return (await query.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync(cancellationToken)).Select(Map).ToArray();
    }

    private async Task<int> CountUnreadAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await db.Notifications.CountAsync(x => x.CustomerId == customerId && !x.IsRead && !x.IsRemoved, cancellationToken);

    private async Task<NotificationDto?> MarkReadAsync(Guid customerId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var item = await db.Notifications.SingleOrDefaultAsync(x => x.Id == notificationId && x.CustomerId == customerId, cancellationToken);
        if (item is null) return null;
        item.IsRead = true;
        item.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private async Task MarkAllReadAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var items = await db.Notifications.Where(x => x.CustomerId == customerId && !x.IsRead).ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt ??= DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<NotificationDto?> UpdateStatusAsync(Guid id, NotificationStatus status, string? failureReason, CancellationToken cancellationToken = default)
    {
        var item = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return null;
        item.Status = status;
        item.FailureReason = failureReason;
        if (status == NotificationStatus.Sent) item.SentAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private static NotificationDto Map(NotificationDbRecord x) => new(
        x.Id,
        x.CustomerId,
        x.Channel.ToString(),
        NotificationService.ChannelLabel(x.Channel),
        x.Type.ToString(),
        NotificationService.TypeLabel(x.Type),
        x.Status.ToString(),
        NotificationService.StatusLabel(x.Status),
        x.Recipient,
        x.Subject,
        x.Body,
        x.RelatedOrderId,
        x.RelatedOrderNumber,
        x.RelatedProductId,
        x.ActionUrl,
        x.IsRead,
        x.CreatedAt,
        x.SentAt,
        x.ReadAt,
        x.FailureReason);

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;

    private static string NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return string.Empty;
        var value = mobile.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
        if (value.StartsWith("+98", StringComparison.Ordinal)) value = "0" + value[3..];
        if (value.StartsWith("98", StringComparison.Ordinal)) value = "0" + value[2..];
        return value;
    }
}
