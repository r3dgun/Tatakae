using Tatakae.Domain.Common;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Entities;

/// <summary>
/// Order aggregate root. It owns monetary totals, payment state and valid workflow transitions.
/// Persistence, notifications and inventory orchestration belong to Application/Infrastructure.
/// </summary>
public sealed class Order
{
    private readonly IReadOnlyCollection<OrderLine> _lines;

    private Order(
        Guid id,
        string orderNumber,
        Guid customerId,
        string customerName,
        string customerMobile,
        Address shippingAddress,
        IReadOnlyCollection<OrderLine> lines,
        decimal shippingAmount,
        decimal discountAmount,
        string shippingMethodCode,
        string shippingMethodTitle,
        DateTimeOffset createdAt,
        OrderStatus status,
        PaymentStatus paymentStatus,
        decimal subtotal,
        decimal total,
        string? trackingCode,
        string? adminNote)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه سفارش معتبر نیست.");
        OrderNumber = DomainGuard.Required(orderNumber, nameof(orderNumber), "شماره سفارش الزامی است.").ToUpperInvariant();
        CustomerId = DomainGuard.NotEmpty(customerId, nameof(customerId), "شناسه مشتری معتبر نیست.");
        CustomerName = DomainGuard.Required(customerName, nameof(customerName), "نام مشتری الزامی است.");
        CustomerMobile = DomainGuard.Required(customerMobile, nameof(customerMobile), "شماره موبایل مشتری الزامی است.");
        ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
        _lines = DomainGuard.NotEmpty(lines, nameof(lines), "سفارش باید حداقل یک آیتم داشته باشد.").ToArray();
        ShippingAmount = DomainGuard.NonNegative(shippingAmount, nameof(shippingAmount), "هزینه ارسال نمی‌تواند منفی باشد.");
        DiscountAmount = DomainGuard.NonNegative(discountAmount, nameof(discountAmount), "مبلغ تخفیف نمی‌تواند منفی باشد.");
        ShippingMethodCode = string.IsNullOrWhiteSpace(shippingMethodCode) ? "manual" : shippingMethodCode.Trim();
        ShippingMethodTitle = string.IsNullOrWhiteSpace(shippingMethodTitle) ? "ارسال دستی" : shippingMethodTitle.Trim();
        CreatedAt = createdAt;
        Status = status;
        PaymentStatus = paymentStatus;
        Subtotal = DomainGuard.NonNegative(subtotal, nameof(subtotal), "جمع مبلغ سفارش نمی‌تواند منفی باشد.");
        Total = DomainGuard.NonNegative(total, nameof(total), "مبلغ نهایی سفارش نمی‌تواند منفی باشد.");
        TrackingCode = DomainGuard.Optional(trackingCode);
        AdminNote = DomainGuard.Optional(adminNote);

        EnsureTrackingCodeForShipping(Status, TrackingCode);
    }

    public Guid Id { get; }
    public string OrderNumber { get; }
    public Guid CustomerId { get; }
    public string CustomerName { get; }
    public string CustomerMobile { get; }
    public Address ShippingAddress { get; }
    public DateTimeOffset CreatedAt { get; }
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines;
    public decimal Subtotal { get; }
    public decimal ShippingAmount { get; }
    public decimal DiscountAmount { get; }
    public string ShippingMethodCode { get; }
    public string ShippingMethodTitle { get; }
    public decimal Total { get; }
    public string? TrackingCode { get; private set; }
    public string? AdminNote { get; private set; }

    public static Order Create(
        Guid id,
        string orderNumber,
        Guid customerId,
        string customerName,
        string customerMobile,
        Address shippingAddress,
        IReadOnlyCollection<OrderLine> lines,
        decimal shippingAmount,
        decimal discountAmount,
        string shippingMethodCode,
        string shippingMethodTitle,
        DateTimeOffset createdAt)
    {
        var subtotal = (lines ?? []).Sum(x => x.LineTotal);
        var total = Math.Max(0m, subtotal + shippingAmount - discountAmount);

        // New orders are always created from their lines, so their monetary values
        // remain internally consistent. Rehydrate intentionally preserves persisted
        // totals to keep historical orders readable while legacy rows are repaired.
        return new Order(
            id,
            orderNumber,
            customerId,
            customerName,
            customerMobile,
            shippingAddress,
            lines,
            shippingAmount,
            discountAmount,
            shippingMethodCode,
            shippingMethodTitle,
            createdAt,
            OrderStatus.PendingPayment,
            PaymentStatus.Pending,
            subtotal,
            total,
            null,
            null);
    }

    public static Order Rehydrate(
        Guid id,
        string orderNumber,
        Guid customerId,
        string customerName,
        string customerMobile,
        Address shippingAddress,
        IReadOnlyCollection<OrderLine> lines,
        decimal shippingAmount,
        decimal discountAmount,
        string shippingMethodCode,
        string shippingMethodTitle,
        DateTimeOffset createdAt,
        OrderStatus status,
        PaymentStatus paymentStatus,
        decimal subtotal,
        decimal total,
        string? trackingCode,
        string? adminNote)
        => new(
            id,
            orderNumber,
            customerId,
            customerName,
            customerMobile,
            shippingAddress,
            lines,
            shippingAmount,
            discountAmount,
            shippingMethodCode,
            shippingMethodTitle,
            createdAt,
            status,
            paymentStatus,
            subtotal,
            total,
            trackingCode,
            adminNote);

    public void MarkPaid()
    {
        if (PaymentStatus == PaymentStatus.Paid && Status == OrderStatus.Paid)
            return;

        if (PaymentStatus == PaymentStatus.Refunded || Status == OrderStatus.Refunded)
            throw new InvalidOperationException("سفارش بازپرداخت‌شده قابل پرداخت مجدد نیست.");

        ChangeStatus(OrderStatus.Paid);
        PaymentStatus = PaymentStatus.Paid;
    }

    public void MarkPaymentFailed()
    {
        if (PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("پرداخت موفق را نمی‌توان ناموفق ثبت کرد.");
        if (PaymentStatus == PaymentStatus.Refunded)
            throw new InvalidOperationException("پرداخت بازپرداخت‌شده را نمی‌توان ناموفق ثبت کرد.");

        PaymentStatus = PaymentStatus.Failed;
    }

    public void MarkRefunded(string? adminNote = null)
    {
        if (PaymentStatus == PaymentStatus.Refunded && Status == OrderStatus.Refunded)
        {
            AdminNote = DomainGuard.Optional(adminNote) ?? AdminNote;
            return;
        }

        if (PaymentStatus != PaymentStatus.Paid)
            throw new InvalidOperationException("فقط سفارش پرداخت‌شده قابل بازپرداخت است.");

        Status = OrderStatus.Refunded;
        PaymentStatus = PaymentStatus.Refunded;
        AdminNote = DomainGuard.Optional(adminNote) ?? AdminNote;
    }

    public void ChangeStatus(
        OrderStatus newStatus,
        string? trackingCode = null,
        string? adminNote = null,
        bool force = false)
    {
        var normalizedTrackingCode = DomainGuard.Optional(trackingCode);
        if (newStatus == Status && normalizedTrackingCode is null)
            normalizedTrackingCode = TrackingCode;
        var normalizedAdminNote = DomainGuard.Optional(adminNote);
        EnsureTrackingCodeForShipping(newStatus, normalizedTrackingCode);

        if (newStatus != Status && !force && !CanTransition(Status, newStatus))
            throw new InvalidOperationException($"تغییر وضعیت سفارش از «{Status}» به «{newStatus}» مجاز نیست.");

        Status = newStatus;
        TrackingCode = normalizedTrackingCode;
        AdminNote = normalizedAdminNote;

        if (newStatus == OrderStatus.Paid)
            PaymentStatus = PaymentStatus.Paid;
        else if (newStatus == OrderStatus.Refunded)
            PaymentStatus = PaymentStatus.Refunded;
        else if (newStatus == OrderStatus.PendingPayment && PaymentStatus != PaymentStatus.Paid)
            PaymentStatus = PaymentStatus.Pending;
    }

    public bool CanMoveTo(OrderStatus newStatus)
        => Status == newStatus || CanTransition(Status, newStatus);

    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => AllowedNextStatuses(from).Contains(to);

    public static IReadOnlyCollection<OrderStatus> AllowedNextStatuses(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => [OrderStatus.Paid, OrderStatus.Cancelled],
        OrderStatus.Paid => [OrderStatus.ArtworkReview, OrderStatus.Cancelled, OrderStatus.Refunded],
        OrderStatus.ArtworkReview => [OrderStatus.InEmbroidery, OrderStatus.Cancelled, OrderStatus.Refunded],
        OrderStatus.InEmbroidery => [OrderStatus.QualityControl, OrderStatus.Cancelled],
        OrderStatus.QualityControl => [OrderStatus.Packed, OrderStatus.InEmbroidery],
        OrderStatus.Packed => [OrderStatus.Shipped, OrderStatus.QualityControl],
        OrderStatus.Shipped => [OrderStatus.Delivered, OrderStatus.Refunded],
        OrderStatus.Delivered => [OrderStatus.Refunded],
        OrderStatus.Cancelled => [OrderStatus.Paid, OrderStatus.ArtworkReview],
        OrderStatus.Refunded => [],
        _ => []
    };

    private static void EnsureTrackingCodeForShipping(OrderStatus status, string? trackingCode)
    {
        if ((status is OrderStatus.Shipped or OrderStatus.Delivered) && string.IsNullOrWhiteSpace(trackingCode))
            throw new InvalidOperationException("برای سفارش ارسال‌شده ثبت کد رهگیری الزامی است.");
    }
}

public sealed record OrderLine
{
    public OrderLine(
        Guid productId,
        Guid variantId,
        string productName,
        string productSlug,
        string productImageUrl,
        string sku,
        string size,
        string colorName,
        string colorHex,
        int quantity,
        decimal unitGarmentPrice,
        EmbroideryConfiguration embroidery)
    {
        ProductId = DomainGuard.NotEmpty(productId, nameof(productId), "شناسه محصول معتبر نیست.");
        VariantId = DomainGuard.NotEmpty(variantId, nameof(variantId), "شناسه تنوع محصول معتبر نیست.");
        ProductName = DomainGuard.Required(productName, nameof(productName), "نام محصول الزامی است.");
        ProductSlug = DomainGuard.Required(productSlug, nameof(productSlug), "اسلاگ محصول الزامی است.");
        ProductImageUrl = DomainGuard.Required(productImageUrl, nameof(productImageUrl), "تصویر محصول الزامی است.");
        Sku = DomainGuard.Required(sku, nameof(sku), "SKU الزامی است.").ToUpperInvariant();
        Size = DomainGuard.Required(size, nameof(size), "سایز محصول الزامی است.");
        ColorName = DomainGuard.Required(colorName, nameof(colorName), "نام رنگ الزامی است.");
        ColorHex = DomainGuard.Required(colorHex, nameof(colorHex), "رنگ محصول الزامی است.");
        Quantity = DomainGuard.Positive(quantity, nameof(quantity), "تعداد سفارش باید بیشتر از صفر باشد.");
        UnitGarmentPrice = DomainGuard.NonNegative(unitGarmentPrice, nameof(unitGarmentPrice), "قیمت واحد نمی‌تواند منفی باشد.");
        Embroidery = embroidery ?? throw new ArgumentNullException(nameof(embroidery));
    }

    public Guid ProductId { get; }
    public Guid VariantId { get; }
    public string ProductName { get; }
    public string ProductSlug { get; }
    public string ProductImageUrl { get; }
    public string Sku { get; }
    public string Size { get; }
    public string ColorName { get; }
    public string ColorHex { get; }
    public int Quantity { get; }
    public decimal UnitGarmentPrice { get; }
    public EmbroideryConfiguration Embroidery { get; }
    public decimal UnitPrice => UnitGarmentPrice + Embroidery.CalculatedPrice;
    public decimal LineTotal => UnitPrice * Quantity;
}
