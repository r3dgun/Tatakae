namespace Tatakae.Domain.Enums;

public enum NotificationChannel
{
    InApp = 0,
    Sms = 1,
    Email = 2,
    Admin = 3
}

public enum NotificationStatus
{
    Queued = 0,
    Sent = 1,
    Failed = 2,
    Cancelled = 3
}

public enum NotificationType
{
    OrderCreated = 0,
    PaymentSucceeded = 1,
    PaymentFailed = 2,
    OrderStatusChanged = 3,
    ShipmentTrackingAdded = 4,
    ArtworkApproved = 5,
    ArtworkNeedsRevision = 6,
    ArtworkRejected = 7,
    ReviewPublished = 8,
    QuestionAnswered = 9,
    AdminTask = 10,
    Manual = 11
}
