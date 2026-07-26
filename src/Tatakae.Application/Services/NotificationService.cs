using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Enums;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class NotificationService(
    INotificationRepository notifications, ICustomerRepository customers,
    ILogger<NotificationService>? logger = null) : INotificationService
{
    private readonly ILogger<NotificationService> _logger = logger ?? NullLogger<NotificationService>.Instance;
    public async Task<NotificationSummaryDto> GetMineAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).RequireData();
        var items = (await notifications.GetForCustomerAsync(customer.Id, cancellationToken)).RequireData();
        return new NotificationSummaryDto(
            items.Count,
            items.Count(x => !x.IsRead),
            items.Count(x => x.Status == NotificationStatus.Queued.ToString()),
            items.Count(x => x.Status == NotificationStatus.Sent.ToString()),
            items.Count(x => x.Status == NotificationStatus.Failed.ToString()),
            items);
    }

    public async Task<int> CountUnreadAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        return customer is null ? 0 : (await notifications.CountUnreadAsync(customer.Id, cancellationToken)).RequireData();
    }

    public async Task<NotificationDto?> MarkReadAsync(string mobile, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).RequireData();
        return (await notifications.MarkReadAsync(customer.Id, notificationId, cancellationToken)).DataOrDefault();
    }

    public async Task MarkAllReadAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).RequireData();
        (await notifications.MarkAllReadAsync(customer.Id, cancellationToken)).EnsureSuccess();
    }

    public async Task<IReadOnlyCollection<NotificationDto>> AdminListAsync(AdminNotificationFilter filter, CancellationToken cancellationToken = default)
        => (await notifications.GetForAdminAsync(filter, cancellationToken)).RequireData();

    public async Task<NotificationDto> AdminCreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        return (await notifications.CreateAsync(request, cancellationToken)).RequireData();
    }

    public async Task<NotificationDto?> AdminUpdateStatusAsync(Guid id, UpdateNotificationStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationStatus>(request.Status, true, out var status))
            throw new ArgumentException("وضعیت اعلان معتبر نیست.");
        return (await notifications.UpdateStatusAsync(id, status, request.FailureReason, cancellationToken)).DataOrDefault();
    }

    public async Task<NotificationDto> QueueOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default)
        => await QueueForOrderAsync(order, NotificationType.OrderCreated, "سفارش شما ثبت شد", $"سفارش {order.OrderNumber} ثبت شد و در انتظار پرداخت است.", cancellationToken);

    public async Task<NotificationDto> QueueOrderStatusChangedAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        var type = string.IsNullOrWhiteSpace(order.TrackingCode) ? NotificationType.OrderStatusChanged : NotificationType.ShipmentTrackingAdded;
        var body = string.IsNullOrWhiteSpace(order.TrackingCode)
            ? $"وضعیت سفارش {order.OrderNumber} به «{order.StatusLabel}» تغییر کرد."
            : $"وضعیت سفارش {order.OrderNumber} به «{order.StatusLabel}» تغییر کرد. کد رهگیری: {order.TrackingCode}";
        return await QueueForOrderAsync(order, type, "تغییر وضعیت سفارش", body, cancellationToken);
    }

    public async Task<NotificationDto> QueuePaymentResultAsync(PaymentReceiptDto receipt, string customerMobile, CancellationToken cancellationToken = default)
    {
        var success = receipt.Status is "Succeeded" or "Verified";
        var request = new CreateNotificationRequest
        {
            CustomerMobile = customerMobile,
            Channel = "InApp",
            Type = success ? NotificationType.PaymentSucceeded.ToString() : NotificationType.PaymentFailed.ToString(),
            Recipient = customerMobile,
            Subject = success ? "پرداخت سفارش تأیید شد" : "پرداخت سفارش ناموفق بود",
            Body = success
                ? $"پرداخت سفارش {receipt.OrderNumber} با مبلغ {receipt.Amount:N0} تومان ثبت شد. شماره پیگیری: {receipt.TraceNumber ?? receipt.RefId ?? "-"}"
                : $"پرداخت سفارش {receipt.OrderNumber} ناموفق بود. دوباره پرداخت را انجام بده یا با پشتیبانی تماس بگیر.",
            RelatedOrderId = receipt.OrderId,
            RelatedOrderNumber = receipt.OrderNumber,
            ActionUrl = $"/account/orders",
            MarkAsSent = true
        };
        Validate(request);
        return (await notifications.CreateAsync(request, cancellationToken)).RequireData();
    }

    private async Task<NotificationDto> QueueForOrderAsync(OrderDto order, NotificationType type, string subject, string body, CancellationToken cancellationToken)
    {
        var request = new CreateNotificationRequest
        {
            CustomerMobile = order.CustomerMobile,
            Channel = "InApp",
            Type = type.ToString(),
            Recipient = order.CustomerMobile,
            Subject = subject,
            Body = body,
            RelatedOrderId = order.Id,
            RelatedOrderNumber = order.OrderNumber,
            ActionUrl = "/account/orders",
            MarkAsSent = true
        };
        Validate(request);
        return (await notifications.CreateAsync(request, cancellationToken)).RequireData();
    }

    private static void Validate(CreateNotificationRequest request)
    {
        if (request.CustomerId is null && string.IsNullOrWhiteSpace(request.CustomerMobile) && request.Channel != "Admin")
            throw new ArgumentException("برای اعلان مشتری باید شناسه مشتری یا شماره موبایل ثبت شود.");
        if (string.IsNullOrWhiteSpace(request.Subject)) throw new ArgumentException("عنوان اعلان لازم است.");
        if (string.IsNullOrWhiteSpace(request.Body)) throw new ArgumentException("متن اعلان لازم است.");
    }

    public static string ChannelLabel(NotificationChannel channel) => channel switch
    {
        NotificationChannel.InApp => "داخل حساب کاربری",
        NotificationChannel.Sms => "پیامک",
        NotificationChannel.Email => "ایمیل",
        NotificationChannel.Admin => "اعلان ادمین",
        _ => channel.ToString()
    };

    public static string StatusLabel(NotificationStatus status) => status switch
    {
        NotificationStatus.Queued => "در صف ارسال",
        NotificationStatus.Sent => "ارسال شده",
        NotificationStatus.Failed => "ناموفق",
        NotificationStatus.Cancelled => "لغو شده",
        _ => status.ToString()
    };

    public static string TypeLabel(NotificationType type) => type switch
    {
        NotificationType.OrderCreated => "ثبت سفارش",
        NotificationType.PaymentSucceeded => "پرداخت موفق",
        NotificationType.PaymentFailed => "پرداخت ناموفق",
        NotificationType.OrderStatusChanged => "تغییر وضعیت سفارش",
        NotificationType.ShipmentTrackingAdded => "ثبت کد رهگیری",
        NotificationType.ArtworkApproved => "تأیید طرح گلدوزی",
        NotificationType.ArtworkNeedsRevision => "نیاز به اصلاح طرح",
        NotificationType.ArtworkRejected => "رد طرح گلدوزی",
        NotificationType.ReviewPublished => "انتشار نظر",
        NotificationType.QuestionAnswered => "پاسخ پرسش",
        NotificationType.AdminTask => "کار ادمین",
        NotificationType.Manual => "دستی",
        _ => type.ToString()
    };
}
