using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Tests;

public sealed class OrderTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 2, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_UsesSuppliedIdentityAndTimeAndCalculatesTotals()
    {
        var id = Guid.NewGuid();
        var order = Create(id: id, orderNumber: "emb-test-1001", lines: [Line(quantity: 2, garmentPrice: 800_000m, embroideryPrice: 100_000m)], shippingAmount: 80_000m, discountAmount: 50_000m);

        Assert.Equal(id, order.Id);
        Assert.Equal("EMB-TEST-1001", order.OrderNumber);
        Assert.Equal(CreatedAt, order.CreatedAt);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(1_800_000m, order.Subtotal);
        Assert.Equal(1_830_000m, order.Total);
    }

    [Fact]
    public void MarkPaid_UpdatesPaymentAndOrderStatus()
    {
        var order = Create();

        order.MarkPaid();

        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void ChangeStatus_RejectsInvalidTransition()
    {
        var order = Create();

        var error = Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(OrderStatus.Shipped, "TRK-1"));

        Assert.Contains("مجاز نیست", error.Message);
    }

    [Fact]
    public void ChangeStatus_WhenShipped_RequiresTrackingCode()
    {
        var order = Create();
        order.MarkPaid();
        order.ChangeStatus(OrderStatus.ArtworkReview);
        order.ChangeStatus(OrderStatus.InEmbroidery);
        order.ChangeStatus(OrderStatus.QualityControl);
        order.ChangeStatus(OrderStatus.Packed);

        Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(OrderStatus.Shipped));
    }

    [Fact]
    public void ChangeStatus_TrimsTrackingCodeAndAdminNote()
    {
        var order = Create();
        order.MarkPaid();
        order.ChangeStatus(OrderStatus.ArtworkReview);
        order.ChangeStatus(OrderStatus.InEmbroidery);
        order.ChangeStatus(OrderStatus.QualityControl);
        order.ChangeStatus(OrderStatus.Packed);

        order.ChangeStatus(OrderStatus.Shipped, "  TRK-123  ", "  ارسال شد  ");

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal("TRK-123", order.TrackingCode);
        Assert.Equal("ارسال شد", order.AdminNote);
    }

    [Fact]
    public void MarkRefunded_RequiresPaidOrderAndUpdatesBothStatuses()
    {
        var unpaidOrder = Create();
        Assert.Throws<InvalidOperationException>(() => unpaidOrder.MarkRefunded());

        var paidOrder = Create();
        paidOrder.MarkPaid();
        paidOrder.ChangeStatus(OrderStatus.ArtworkReview);
        paidOrder.ChangeStatus(OrderStatus.InEmbroidery);

        paidOrder.MarkRefunded("Refund completed by Zarinpal");

        Assert.Equal(OrderStatus.Refunded, paidOrder.Status);
        Assert.Equal(PaymentStatus.Refunded, paidOrder.PaymentStatus);
        Assert.Equal("Refund completed by Zarinpal", paidOrder.AdminNote);
    }

    [Fact]
    public void Create_WithoutLines_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(lines: []));
    }


    [Fact]
    public void Rehydrate_PreservesLegacyStoredTotals_WhenLineBreakdownDiffers()
    {
        var line = Line(garmentPrice: 800_000m, embroideryPrice: 100_000m);

        var order = Order.Rehydrate(
            Guid.NewGuid(),
            "EMB-LEGACY-0001",
            Guid.NewGuid(),
            "کاربر قدیمی",
            "09123456789",
            Address(),
            [line],
            shippingAmount: 50_000m,
            discountAmount: 0m,
            shippingMethodCode: "post",
            shippingMethodTitle: "پست پیشتاز",
            createdAt: CreatedAt,
            status: OrderStatus.PendingPayment,
            paymentStatus: PaymentStatus.Pending,
            subtotal: 800_000m,
            total: 850_000m,
            trackingCode: null,
            adminNote: null);

        Assert.Equal(800_000m, order.Subtotal);
        Assert.Equal(850_000m, order.Total);
        Assert.Equal(900_000m, order.Lines.Single().LineTotal);
    }

    [Fact]
    public void Create_DoesNotReadSystemClockOrGenerateOrderNumber()
    {
        var order = Create(orderNumber: "EMB-FIXED-0001");

        Assert.Equal("EMB-FIXED-0001", order.OrderNumber);
        Assert.Equal(CreatedAt, order.CreatedAt);
    }

    private static Order Create(
        Guid? id = null,
        string orderNumber = "EMB-TEST-0001",
        IReadOnlyCollection<OrderLine>? lines = null,
        decimal shippingAmount = 0m,
        decimal discountAmount = 0m)
        => Order.Create(
            id ?? Guid.NewGuid(),
            orderNumber,
            Guid.NewGuid(),
            "کاربر تست",
            "09123456789",
            Address(),
            lines ?? [Line()],
            shippingAmount,
            discountAmount,
            "post",
            "پست پیشتاز",
            CreatedAt);

    private static Address Address() => new(
        Guid.NewGuid(),
        "کاربر تست",
        "09123456789",
        "تهران",
        "تهران",
        "1234567890",
        "خیابان تست، پلاک ۱",
        "1",
        "2",
        true);

    private static OrderLine Line(int quantity = 1, decimal garmentPrice = 800_000m, decimal embroideryPrice = 0m) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "تی‌شرت گلدوزی",
        "embroidered-tshirt",
        "https://example.com/product.jpg",
        "TT-TEE-BLK-M",
        "M",
        "مشکی",
        "#111111",
        quantity,
        garmentPrice,
        new EmbroideryConfiguration(
            Guid.NewGuid(),
            EmbroideryPlacement.LeftChest,
            WidthCm: 8m,
            HeightCm: 8m,
            ThreadColorCount: 2,
            ThreadColorHexes: ["#111111", "#FFFFFF"],
            ArtworkFileUrl: null,
            ArtworkFileName: null,
            Text: null,
            FontName: null,
            Note: null,
            CalculatedPrice: embroideryPrice));
}
