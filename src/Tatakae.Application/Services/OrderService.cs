using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Interfaces.Gateways;

namespace Tatakae.Application.Services;

public sealed partial class OrderService(
    IProductRepository products,
    IOrderRepository orders,
    ICustomerRepository customers,
    ICouponRepository coupons,
    IEmbroideryPricingService embroidery,
    IShippingService shippingService,
    ILogger<OrderService>? logger = null,
    IInventoryReservationGateway? inventoryReservations = null) : IOrderService
{
    private readonly ILogger<OrderService> _logger = logger ?? NullLogger<OrderService>.Instance;
    public async Task<EmbroideryQuoteDto> QuoteEmbroideryAsync(EmbroideryCustomizationRequest request, CancellationToken cancellationToken = default)
    {
        var product = (await products.GetByIdAsync(request.ProductId, cancellationToken)).RequireData();
        if (!product.SupportsEmbroidery) throw new InvalidOperationException("این محصول آماده و از قبل گلدوزی‌شده است و وارد استودیو نمی‌شود.");
        if (!product.Variants.Any(x => x.Id == request.VariantId && x.IsActive)) throw new ArgumentException("تنوع انتخابی معتبر نیست.");
        return embroidery.Quote(product, request).RequireData();
    }

    public async Task<OrderDto> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var lines = new List<OrderLine>();
        foreach (var item in request.Items)
        {
            var product = (await products.GetByIdAsync(item.ProductId, cancellationToken)).RequireData();
            if (!product.IsPublished) throw new ArgumentException("این محصول در حال حاضر قابل سفارش نیست.");

            var variant = product.Variants.SingleOrDefault(x => x.Id == item.VariantId && x.IsActive)
                          ?? throw new ArgumentException("تنوع انتخاب‌شده برای محصول معتبر نیست.");
            if (variant.AvailableQuantity < item.Quantity) throw new ArgumentException($"موجودی {product.Name} کافی نیست.");

            item.Embroidery.ProductId = product.Id;
            item.Embroidery.VariantId = variant.Id;
            var config = product.SupportsEmbroidery
                ? embroidery.CreateConfiguration(product, item.Embroidery).RequireData()
                : CreateReadyMadeConfiguration(product, variant);
            var primaryImage = product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).First();
            lines.Add(new OrderLine(product.Id, variant.Id, product.Name, product.Slug, primaryImage.Url, variant.Sku, variant.Size, variant.ColorName, variant.ColorHex, item.Quantity, variant.EffectivePrice, config));
        }

        var customer = (await customers.GetByMobileAsync(request.Mobile, cancellationToken)).DataOrDefault()
                       ?? Customer.Create(Guid.NewGuid(), request.CustomerName, request.Mobile, request.Email, DateTimeOffset.UtcNow);
        (await customers.UpsertAsync(customer, cancellationToken)).EnsureSuccess();

        var subtotal = lines.Sum(x => x.LineTotal);
        var discount = 0m;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = (await coupons.GetByCodeAsync(request.CouponCode, cancellationToken)).RequireData();
            discount = coupon.Redeem(DateTimeOffset.UtcNow, subtotal);

            (await coupons.UpsertAsync(coupon, cancellationToken)).EnsureSuccess();
        }

        var shippingMethod = (await shippingService.ResolveCheckoutMethodAsync(request.ShippingMethodCode, subtotal, cancellationToken)).RequireData();
        var shipping = shippingMethod.Price;
        var address = new Address(Guid.NewGuid(), request.ShippingAddress.RecipientName, request.ShippingAddress.Mobile, request.ShippingAddress.Province, request.ShippingAddress.City, request.ShippingAddress.PostalCode, request.ShippingAddress.AddressLine, request.ShippingAddress.Plaque, request.ShippingAddress.Unit, true);
        var createdAt = DateTimeOffset.UtcNow;
        var orderNumber = $"EMB-{createdAt:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 10000)}";
        var order = Order.Create(Guid.NewGuid(), orderNumber, customer.Id, request.CustomerName, request.Mobile, address, lines, shipping, discount, shippingMethod.Code, shippingMethod.Title, createdAt);
        var reservationGateway = inventoryReservations
            ?? throw new InvalidOperationException("سرویس رزرو موجودی پیکربندی نشده است.");
        var reservation = await reservationGateway.CreateReservedOrderAsync(order, cancellationToken);
        return Map(order, reservation);
    }

    private static EmbroideryConfiguration CreateReadyMadeConfiguration(Product product, ProductVariant variant) => new(
        Guid.NewGuid(),
        EmbroideryPlacement.CenterChest,
        0m,
        0m,
        0,
        Array.Empty<string>(),
        null,
        null,
        null,
        null,
        "محصول آماده و از قبل گلدوزی‌شده؛ نیاز به استودیو ندارد.",
        0m,
        product.ApparelCategory.ToString(),
        variant.Size,
        variant.ColorHex,
        "ReadyMade",
        null,
        0,
        0,
        100,
        0,
        100);

    public async Task<IReadOnlyCollection<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orderData = (await orders.GetAllAsync(cancellationToken)).RequireData()
            .OrderByDescending(x => x.CreatedAt)
            .ToArray();
        var reservationMap = inventoryReservations is null
            ? new Dictionary<Guid, InventoryReservationSnapshot>()
            : await inventoryReservations.GetForOrdersAsync(
                orderData.Select(x => x.Id).ToArray(),
                cancellationToken);

        return orderData
            .Select(order => Map(order, reservationMap.GetValueOrDefault(order.Id)))
            .ToArray();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = (await orders.GetByIdAsync(id, cancellationToken)).DataOrDefault();
        if (order is null) return null;
        var reservation = inventoryReservations is null
            ? null
            : await inventoryReservations.GetForOrderAsync(order.Id, cancellationToken);
        return Map(order, reservation);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid id, OrderStatus status, string? trackingCode, string? adminNote, CancellationToken cancellationToken = default, bool force = false, string changedBy = "admin")
    {
        var order = (await orders.GetByIdAsync(id, cancellationToken)).RequireData();
        var from = order.Status;

        if (from == status)
        {
            order.ChangeStatus(status, trackingCode, adminNote, force);
            (await orders.UpdateAsync(order, cancellationToken)).EnsureSuccess();
            return Map(order);
        }

        if (!force && !order.CanMoveTo(status))
            throw new InvalidOperationException($"تغییر وضعیت از «{StatusLabel(from)}» به «{StatusLabel(status)}» مجاز نیست.");

        await ApplyInventoryForStatusChangeAsync(order, from, status, cancellationToken);

        order.ChangeStatus(status, trackingCode, adminNote, force);
        (await orders.UpdateAsync(order, cancellationToken)).EnsureSuccess();
        (await orders.AddStatusHistoryAsync(order.Id, from, status, StatusChangeTitle(from, status), adminNote, trackingCode, changedBy, cancellationToken)).EnsureSuccess();
        return Map(order);
    }

    public async Task<AdminOrderWorkflowDto?> GetWorkflowAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = (await orders.GetByIdAsync(id, cancellationToken)).DataOrDefault();
        if (order is null) return null;
        var history = (await orders.GetStatusHistoryAsync(id, cancellationToken)).RequireData();
        return new AdminOrderWorkflowDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            StatusLabel(order.Status),
            order.PaymentStatus.ToString(),
            order.TrackingCode,
            StatusOptions(),
            GetNextStatusOptions(order.Status),
            history,
            BuildTimeline(order, history));
    }

    public IReadOnlyCollection<OrderStatusOptionDto> GetStatusOptions() => StatusOptions();

    public static OrderTrackingDto MapTracking(Order order, IReadOnlyCollection<OrderStatusHistoryDto> history) => new(
        order.Id,
        order.OrderNumber,
        order.Status.ToString(),
        StatusLabel(order.Status),
        order.TrackingCode,
        BuildTimeline(order, history));

    private async Task ApplyInventoryForStatusChangeAsync(Order order, OrderStatus from, OrderStatus to, CancellationToken cancellationToken)
    {
        if (from == OrderStatus.PendingPayment && to == OrderStatus.Paid)
        {
            var reservationGateway = inventoryReservations
                ?? throw new InvalidOperationException("سرویس رزرو موجودی پیکربندی نشده است.");
            var consumed = await reservationGateway.ConsumePendingAsync(
                order.Id,
                "مصرف رزرو به‌دلیل تأیید دستی پرداخت سفارش.",
                cancellationToken);
            if (!consumed)
                throw new InvalidOperationException("رزرو فعال موجودی برای پرداخت سفارش وجود ندارد.");
            return;
        }

        var fromClosed = IsInventoryReleasedStatus(from);
        var toClosed = IsInventoryReleasedStatus(to);
        if (fromClosed == toClosed) return;

        if (toClosed && from == OrderStatus.PendingPayment)
        {
            var reservationGateway = inventoryReservations
                ?? throw new InvalidOperationException("سرویس رزرو موجودی پیکربندی نشده است.");
            await reservationGateway.ReleasePendingAsync(
                order.Id,
                "آزادسازی موجودی به‌دلیل لغو سفارش در انتظار پرداخت.",
                cancellationToken);
            return;
        }

        if (fromClosed && to == OrderStatus.PendingPayment && order.PaymentStatus != PaymentStatus.Paid)
        {
            var reservationGateway = inventoryReservations
                ?? throw new InvalidOperationException("سرویس رزرو موجودی پیکربندی نشده است.");
            await reservationGateway.ReserveExistingOrderAsync(order, cancellationToken);
            return;
        }

        foreach (var line in order.Lines)
        {
            var product = (await products.GetByIdAsync(line.ProductId, cancellationToken)).RequireData();
            var variant = product.Variants.SingleOrDefault(x => x.Id == line.VariantId)
                          ?? throw new KeyNotFoundException($"SKU سفارش پیدا نشد: {line.Sku}");

            if (toClosed)
            {
                // سفارش پرداخت‌شده قبلاً رزرو را مصرف کرده است؛ در لغو/مرجوعی موجودی فیزیکی برمی‌گردد.
                variant.AdjustStock(line.Quantity);
            }
            else
            {
                if (variant.AvailableQuantity < line.Quantity)
                    throw new InvalidOperationException($"موجودی SKU {line.Sku} برای فعال‌سازی دوباره سفارش کافی نیست.");
                variant.AdjustStock(-line.Quantity);
            }

            (await products.UpsertAsync(product, cancellationToken)).EnsureSuccess();
        }
    }

    private static bool IsInventoryReleasedStatus(OrderStatus status)
        => status is OrderStatus.Cancelled or OrderStatus.Refunded;

    public static bool CanMove(OrderStatus from, OrderStatus to)
        => Order.CanTransition(from, to);

    public static IReadOnlyCollection<OrderStatus> AllowedNextStatuses(OrderStatus status)
        => Order.AllowedNextStatuses(status);

    private static IReadOnlyCollection<OrderStatusOptionDto> GetNextStatusOptions(OrderStatus status)
        => AllowedNextStatuses(status).Select(StatusOption).ToArray();

    private static IReadOnlyCollection<OrderStatusOptionDto> StatusOptions()
        => Enum.GetValues<OrderStatus>().OrderBy(x => (int)x).Select(StatusOption).ToArray();

    private static OrderStatusOptionDto StatusOption(OrderStatus status) => new(
        status.ToString(),
        StatusLabel(status),
        StatusDescription(status),
        (int)status,
        status is OrderStatus.Cancelled or OrderStatus.Refunded or OrderStatus.Delivered,
        status is OrderStatus.Shipped or OrderStatus.Delivered,
        StatusTone(status));

    private static string StatusDescription(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "سفارش ثبت شده ولی پرداخت نهایی نشده است.",
        OrderStatus.Paid => "پرداخت ثبت شده و سفارش آماده شروع عملیات است.",
        OrderStatus.ArtworkReview => "فایل، متن یا جزئیات گلدوزی باید بررسی شود.",
        OrderStatus.InEmbroidery => "سفارش وارد مرحله تولید و گلدوزی شده است.",
        OrderStatus.QualityControl => "کیفیت دوخت، رنگ نخ و جایگذاری طرح کنترل می‌شود.",
        OrderStatus.Packed => "سفارش بسته‌بندی شده و آماده ارسال است.",
        OrderStatus.Shipped => "سفارش تحویل پست/پیک شده است.",
        OrderStatus.Delivered => "سفارش به مشتری تحویل شده است.",
        OrderStatus.Cancelled => "سفارش لغو شده و موجودی به انبار برگشته است.",
        OrderStatus.Refunded => "وجه سفارش بازگشت داده شده یا مرجوعی نهایی شده است.",
        _ => status.ToString()
    };

    private static string StatusTone(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "muted",
        OrderStatus.Paid => "info",
        OrderStatus.ArtworkReview => "warning",
        OrderStatus.InEmbroidery => "primary",
        OrderStatus.QualityControl => "violet",
        OrderStatus.Packed => "neutral",
        OrderStatus.Shipped => "success",
        OrderStatus.Delivered => "done",
        OrderStatus.Cancelled => "danger",
        OrderStatus.Refunded => "danger",
        _ => "neutral"
    };

    private static string StatusChangeTitle(OrderStatus from, OrderStatus to)
        => $"تغییر وضعیت از {StatusLabel(from)} به {StatusLabel(to)}";

    private static IReadOnlyCollection<OrderTimelineItemDto> BuildTimeline(Order order, IReadOnlyCollection<OrderStatusHistoryDto> history)
    {
        var orderStatuses = Enum.GetValues<OrderStatus>().OrderBy(x => (int)x).ToArray();
        return orderStatuses.Select(status =>
        {
            var row = history.LastOrDefault(x => string.Equals(x.ToStatus, status.ToString(), StringComparison.OrdinalIgnoreCase));
            var completed = (int)status <= (int)order.Status && order.Status is not OrderStatus.Cancelled and not OrderStatus.Refunded;
            if (status is OrderStatus.Cancelled or OrderStatus.Refunded)
                completed = order.Status == status;
            return new OrderTimelineItemDto(status.ToString(), StatusLabel(status), StatusDescription(status), row?.HappenedAt, completed, order.Status == status);
        }).ToArray();
    }

    public static OrderDto Map(Order order, InventoryReservationSnapshot? reservation = null) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerName,
        order.CustomerMobile,
        order.CreatedAt,
        order.Status.ToString(),
        StatusLabel(order.Status),
        order.PaymentStatus.ToString(),
        order.Subtotal,
        order.ShippingAmount,
        order.DiscountAmount,
        order.Total,
        order.TrackingCode,
        order.AdminNote,
        order.ShippingMethodCode,
        order.ShippingMethodTitle,
        new OrderAddressDto(order.ShippingAddress.RecipientName, order.ShippingAddress.Mobile, order.ShippingAddress.Province, order.ShippingAddress.City, order.ShippingAddress.PostalCode, order.ShippingAddress.AddressLine, order.ShippingAddress.Plaque, order.ShippingAddress.Unit),
        order.Lines.Select(line => new OrderLineDto(line.ProductId, line.VariantId, line.ProductName, line.ProductSlug, line.ProductImageUrl, line.Sku, line.Size, line.ColorName, line.ColorHex, line.Quantity, line.UnitGarmentPrice, line.Embroidery.CalculatedPrice, line.UnitPrice, line.LineTotal,
            new EmbroideryConfigurationDto(line.Embroidery.Id, line.Embroidery.Placement.ToString(), EmbroideryPricingService.EmbroideryLabel(line.Embroidery.Placement), line.Embroidery.WidthCm, line.Embroidery.HeightCm, line.Embroidery.ThreadColorCount, line.Embroidery.ThreadColorHexes, line.Embroidery.ArtworkFileUrl, line.Embroidery.ArtworkFileName, line.Embroidery.Text, line.Embroidery.FontName, line.Embroidery.Note, line.Embroidery.CalculatedPrice, line.Embroidery.GarmentType, line.Embroidery.GarmentSize, line.Embroidery.GarmentColorHex, line.Embroidery.DesignSource, line.Embroidery.MotifKey, line.Embroidery.PositionX, line.Embroidery.PositionY, line.Embroidery.ScalePercent, line.Embroidery.RotationDegrees, line.Embroidery.OpacityPercent))).ToArray(),
        reservation?.ExpiresAt,
        reservation?.Status);

    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "در انتظار پرداخت",
        OrderStatus.Paid => "پرداخت شده",
        OrderStatus.ArtworkReview => "بررسی سفارش",
        OrderStatus.InEmbroidery => "در حال گلدوزی",
        OrderStatus.QualityControl => "کنترل کیفیت",
        OrderStatus.Packed => "بسته‌بندی",
        OrderStatus.Shipped => "ارسال شده",
        OrderStatus.Delivered => "تحویل شده",
        OrderStatus.Cancelled => "لغو شده",
        OrderStatus.Refunded => "بازگشت وجه",
        _ => status.ToString()
    };
}
