using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("Orders")]
[Index(nameof(OrderNumber), IsUnique = true)]
[Index(nameof(CustomerId))]
[Index(nameof(Status), nameof(PaymentStatus))]
[Index(nameof(CreatedAt))]
public sealed class OrderDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(40)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public Guid CustomerId { get; set; }

    [Required, MaxLength(180)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(20), Phone]
    public string CustomerMobile { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string ShippingRecipientName { get; set; } = string.Empty;

    [Required, MaxLength(20), Phone]
    public string ShippingMobile { get; set; } = string.Empty;

    [Required, MaxLength(90)]
    public string ShippingProvince { get; set; } = string.Empty;

    [Required, MaxLength(90)]
    public string ShippingCity { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ShippingPostalCode { get; set; } = string.Empty;

    [Required, MaxLength(900)]
    public string ShippingAddressLine { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ShippingPlaque { get; set; }

    [MaxLength(30)]
    public string? ShippingUnit { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [Required, MaxLength(60)]
    public OrderStatus Status { get; set; }

    [Required, MaxLength(60)]
    public PaymentStatus PaymentStatus { get; set; }

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal ShippingAmount { get; set; }

    [Required, MaxLength(60)]
    public string ShippingMethodCode { get; set; } = "manual";

    [Required, MaxLength(160)]
    public string ShippingMethodTitle { get; set; } = "ارسال دستی";

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    [MaxLength(120)]
    public string? TrackingCode { get; set; }

    [MaxLength(1200)]
    public string? AdminNote { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [InverseProperty(nameof(OrderLineDbRecord.Order))]
    public List<OrderLineDbRecord> Lines { get; set; } = [];

    [InverseProperty(nameof(OrderStatusHistoryDbRecord.Order))]
    public List<OrderStatusHistoryDbRecord> StatusHistory { get; set; } = [];
}

[Table("OrderLines")]
[Index(nameof(OrderId))]
[Index(nameof(ProductId))]
[Index(nameof(VariantId))]
public sealed class OrderLineDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid VariantId { get; set; }

    [Required, MaxLength(240)]
    public string ProductName { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string ProductSlug { get; set; } = string.Empty;

    [Required, MaxLength(1000), Url]
    public string ProductImageUrl { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Size { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string ColorName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ColorHex { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Precision(18, 2)]
    public decimal UnitGarmentPrice { get; set; }

    [Required]
    public Guid EmbroideryId { get; set; }

    [Required, MaxLength(80)]
    public EmbroideryPlacement EmbroideryPlacement { get; set; }

    [Precision(9, 2)]
    public decimal EmbroideryWidthCm { get; set; }

    [Precision(9, 2)]
    public decimal EmbroideryHeightCm { get; set; }

    [Range(1, 24)]
    public int EmbroideryThreadColorCount { get; set; }

    [Required, MaxLength(400)]
    public string EmbroideryThreadColorHexesCsv { get; set; } = string.Empty;

    [MaxLength(1000), Url]
    public string? EmbroideryArtworkFileUrl { get; set; }

    [MaxLength(260)]
    public string? EmbroideryArtworkFileName { get; set; }

    [MaxLength(400)]
    public string? EmbroideryText { get; set; }

    [MaxLength(120)]
    public string? EmbroideryFontName { get; set; }

    [MaxLength(1200)]
    public string? EmbroideryNote { get; set; }

    [Precision(18, 2)]
    public decimal EmbroideryCalculatedPrice { get; set; }

    [Required, MaxLength(80)]
    public string EmbroideryGarmentType { get; set; } = "TShirt";

    [Required, MaxLength(30)]
    public string EmbroideryGarmentSize { get; set; } = "L";

    [Required, MaxLength(20)]
    public string EmbroideryGarmentColorHex { get; set; } = "#111827";

    [Required, MaxLength(60)]
    public string EmbroideryDesignSource { get; set; } = "Motif";

    [MaxLength(80)]
    public string? EmbroideryMotifKey { get; set; } = "dragon";

    public int EmbroideryPositionX { get; set; }
    public int EmbroideryPositionY { get; set; }

    [Range(10, 400)]
    public int EmbroideryScalePercent { get; set; } = 100;

    [Range(-360, 360)]
    public int EmbroideryRotationDegrees { get; set; }

    [Range(0, 100)]
    public int EmbroideryOpacityPercent { get; set; } = 100;

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }
}


[Table("OrderStatusHistory")]
[Index(nameof(OrderId), nameof(HappenedAt))]
[Index(nameof(ToStatus))]
public sealed class OrderStatusHistoryDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid OrderId { get; set; }

    [MaxLength(60)]
    public OrderStatus? FromStatus { get; set; }

    [Required, MaxLength(60)]
    public OrderStatus ToStatus { get; set; }

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string? Note { get; set; }

    [MaxLength(120)]
    public string? TrackingCode { get; set; }

    [Required, MaxLength(180)]
    public string ChangedBy { get; set; } = "system";

    public DateTimeOffset HappenedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }
}
