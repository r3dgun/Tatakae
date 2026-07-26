using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("Notifications")]
[Index(nameof(CustomerId), nameof(IsRead))]
[Index(nameof(Status), nameof(Channel), nameof(Type))]
[Index(nameof(RelatedOrderId))]
public sealed class NotificationDbRecord : BaseEntity<Guid>
{
    public Guid? CustomerId { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    [Required]
    public NotificationType Type { get; set; } = NotificationType.Manual;

    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;

    [Required, MaxLength(200)]
    public string Recipient { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public Guid? RelatedOrderId { get; set; }

    [MaxLength(60)]
    public string? RelatedOrderNumber { get; set; }

    public Guid? RelatedProductId { get; set; }

    [MaxLength(600)]
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [ForeignKey(nameof(RelatedOrderId))]
    public OrderDbRecord? RelatedOrder { get; set; }

    [ForeignKey(nameof(RelatedProductId))]
    public ProductDbRecord? RelatedProduct { get; set; }
}
