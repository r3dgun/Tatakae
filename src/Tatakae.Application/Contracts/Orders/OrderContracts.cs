using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Contracts.Embroidery;

namespace Tatakae.Application.Contracts.Orders;

public sealed class CheckoutRequest
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "نام و نام خانوادگی باید بین ۳ تا ۱۰۰ کاراکتر باشد.")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [StringLength(150, ErrorMessage = "ایمیل حداکثر ۱۵۰ کاراکتر است.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "آدرس ارسال الزامی است.")]
    public CheckoutAddressRequest ShippingAddress { get; set; } = new();

    [Required]
    [MinLength(1, ErrorMessage = "سبد خرید خالی است.")]
    public List<CheckoutItemRequest> Items { get; set; } = [];

    [StringLength(30, ErrorMessage = "کد تخفیف حداکثر ۳۰ کاراکتر است.")]
    [RegularExpression("^[A-Za-z0-9-]*$", ErrorMessage = "کد تخفیف فقط شامل حروف انگلیسی، عدد و خط تیره است.")]
    public string? CouponCode { get; set; }

    [Required(ErrorMessage = "روش ارسال را انتخاب کنید.")]
    [StringLength(60)]
    public string ShippingMethodCode { get; set; } = "post-standard";
}

public sealed class CheckoutAddressRequest
{
    [Required(ErrorMessage = "نام گیرنده الزامی است.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "نام گیرنده باید بین ۲ تا ۱۰۰ کاراکتر باشد.")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل گیرنده الزامی است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "استان الزامی است.")]
    [StringLength(60, ErrorMessage = "نام استان حداکثر ۶۰ کاراکتر است.")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "شهر الزامی است.")]
    [StringLength(60, ErrorMessage = "نام شهر حداکثر ۶۰ کاراکتر است.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد پستی الزامی است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.PostalCode, ErrorMessage = "کد پستی باید ۱۰ رقم باشد.")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "آدرس کامل الزامی است.")]
    [StringLength(400, MinimumLength = 10, ErrorMessage = "آدرس باید بین ۱۰ تا ۴۰۰ کاراکتر باشد.")]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Plaque { get; set; }

    [StringLength(20)]
    public string? Unit { get; set; }
}

public sealed class CheckoutItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid VariantId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;

    [Required]
    public EmbroideryCustomizationRequest Embroidery { get; set; } = new();
}

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string CustomerMobile,
    DateTimeOffset CreatedAt,
    string Status,
    string StatusLabel,
    string PaymentStatus,
    decimal Subtotal,
    decimal ShippingAmount,
    decimal DiscountAmount,
    decimal Total,
    string? TrackingCode,
    string? AdminNote,
    string ShippingMethodCode,
    string ShippingMethodTitle,
    OrderAddressDto ShippingAddress,
    IReadOnlyCollection<OrderLineDto> Lines,
    DateTimeOffset? ReservationExpiresAt = null,
    string? ReservationStatus = null);

public sealed record OrderAddressDto(string RecipientName, string Mobile, string Province, string City, string PostalCode, string AddressLine, string? Plaque, string? Unit);

public sealed record OrderLineDto(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string ProductSlug,
    string ProductImageUrl,
    string Sku,
    string Size,
    string ColorName,
    string ColorHex,
    int Quantity,
    decimal UnitGarmentPrice,
    decimal EmbroideryPrice,
    decimal UnitPrice,
    decimal LineTotal,
    EmbroideryConfigurationDto Embroidery);

public sealed class OrderLookupQuery
{
    [Required, StringLength(30)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;
}

public sealed class CustomerOrdersQuery
{
    [Required]
    public Guid CustomerId { get; set; }

    [Range(1, 2000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string StatusLabel,
    string PaymentStatus,
    decimal Total,
    int ItemCount,
    DateTimeOffset CreatedAt,
    string? TrackingCode);

public sealed record OrderTimelineItemDto(string Status, string Title, string Description, DateTimeOffset? HappenedAt, bool IsCompleted, bool IsCurrent);

public sealed record OrderTrackingDto(Guid OrderId, string OrderNumber, string Status, string StatusLabel, string? TrackingCode, IReadOnlyCollection<OrderTimelineItemDto> Timeline);


public sealed record OrderStatusOptionDto(
    string Status,
    string Label,
    string Description,
    int SortOrder,
    bool IsTerminal,
    bool RequiresTrackingCode,
    string UiTone);

public sealed record OrderStatusHistoryDto(
    Guid Id,
    Guid OrderId,
    string? FromStatus,
    string? FromStatusLabel,
    string ToStatus,
    string ToStatusLabel,
    string Title,
    string? Note,
    string? TrackingCode,
    string ChangedBy,
    DateTimeOffset HappenedAt);

public sealed record AdminOrderWorkflowDto(
    Guid OrderId,
    string OrderNumber,
    string CurrentStatus,
    string CurrentStatusLabel,
    string PaymentStatus,
    string? TrackingCode,
    IReadOnlyCollection<OrderStatusOptionDto> AllStatuses,
    IReadOnlyCollection<OrderStatusOptionDto> NextStatuses,
    IReadOnlyCollection<OrderStatusHistoryDto> History,
    IReadOnlyCollection<OrderTimelineItemDto> Timeline);
