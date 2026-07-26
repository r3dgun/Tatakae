using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Notifications;

public sealed record NotificationDto(
    Guid Id,
    Guid? CustomerId,
    string Channel,
    string ChannelLabel,
    string Type,
    string TypeLabel,
    string Status,
    string StatusLabel,
    string Recipient,
    string Subject,
    string Body,
    Guid? RelatedOrderId,
    string? RelatedOrderNumber,
    Guid? RelatedProductId,
    string? ActionUrl,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? ReadAt,
    string? FailureReason);

public sealed record NotificationSummaryDto(
    int Total,
    int Unread,
    int Queued,
    int Sent,
    int Failed,
    IReadOnlyCollection<NotificationDto> Items);

public sealed class CreateNotificationRequest : IValidatableObject
{
    public Guid? CustomerId { get; set; }

    [StringLength(30, ErrorMessage = "شماره موبایل حداکثر ۳۰ کاراکتر است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string? CustomerMobile { get; set; }

    [Required]
    [RegularExpression("^(InApp|Sms|Email|Admin)$")]
    public string Channel { get; set; } = "InApp";

    [Required]
    [RegularExpression("^(OrderCreated|PaymentSucceeded|PaymentFailed|OrderStatusChanged|ShipmentTrackingAdded|ArtworkApproved|ArtworkNeedsRevision|ArtworkRejected|ReviewPublished|QuestionAnswered|AdminTask|Manual)$")]
    public string Type { get; set; } = "Manual";

    [Required]
    [StringLength(180)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Recipient { get; set; }

    public Guid? RelatedOrderId { get; set; }

    [StringLength(60)]
    public string? RelatedOrderNumber { get; set; }

    public Guid? RelatedProductId { get; set; }

    [StringLength(600)]
    public string? ActionUrl { get; set; }

    public bool MarkAsSent { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId is null && string.IsNullOrWhiteSpace(CustomerMobile) && string.IsNullOrWhiteSpace(Recipient))
        {
            yield return new ValidationResult("گیرنده اعلان را با مشتری، موبایل یا آدرس گیرنده مشخص کنید.", [nameof(CustomerMobile), nameof(Recipient)]);
        }

        if (!string.IsNullOrWhiteSpace(ActionUrl)
            && !ActionUrl.StartsWith('/')
            && !Uri.TryCreate(ActionUrl, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("لینک اقدام باید مسیر داخلی مانند /account/orders یا URL کامل باشد.", [nameof(ActionUrl)]);
        }
    }
}

public sealed class AdminNotificationFilter
{
    public string? Status { get; set; }
    public string? Channel { get; set; }
    public string? Type { get; set; }
    public string? Search { get; set; }
    public int Take { get; set; } = 100;
}

public sealed class UpdateNotificationStatusRequest
{
    [Required]
    [RegularExpression("^(Queued|Sent|Failed|Cancelled)$")]
    public string Status { get; set; } = "Sent";

    [StringLength(500)]
    public string? FailureReason { get; set; }
}
