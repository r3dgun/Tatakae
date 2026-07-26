using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Admin;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class AdminDashboardService(
    IProductRepository products,
    IOrderRepository orders,
    ICategoryRepository categories,
    IProductEngagementRepository engagement,
    IEmbroideryArtworkRepository artworks,
    ILogger<AdminDashboardService>? logger = null) : IAdminDashboardService
{
    private readonly ILogger<AdminDashboardService> _logger = logger ?? NullLogger<AdminDashboardService>.Instance;
    public async Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var allProducts = (await products.GetAllAsync(cancellationToken)).RequireData();
        var allOrders = (await orders.GetAllAsync(cancellationToken)).RequireData();
        var allCategories = (await categories.GetAllAsync(cancellationToken)).RequireData();
        var pendingReviews = (await engagement.GetReviewsForAdminAsync("Pending", cancellationToken)).RequireData();
        var pendingQuestions = (await engagement.GetQuestionsForAdminAsync("Pending", cancellationToken)).RequireData();
        var pendingArtworks = (await artworks.GetForAdminAsync("PendingReview", cancellationToken)).RequireData();
        var revisionArtworks = (await artworks.GetForAdminAsync("NeedsRevision", cancellationToken)).RequireData();

        var now = DateTimeOffset.UtcNow;
        var today = now.Date;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var paidOrders = allOrders.Where(x => x.PaymentStatus == PaymentStatus.Paid).ToArray();
        var pendingPaymentOrders = allOrders.Where(x => x.PaymentStatus == PaymentStatus.Pending || x.Status == OrderStatus.PendingPayment).ToArray();
        var variants = allProducts.SelectMany(x => x.Variants).ToArray();
        var topProducts = BuildTopProducts(paidOrders);
        var pipeline = BuildPipeline(allOrders);

        return new AdminDashboardDto(
            allProducts.Count,
            allProducts.Count(x => x.IsPublished),
            allCategories.Count,
            allOrders.Count,
            allOrders.Count(x => x.Status == OrderStatus.ArtworkReview),
            variants.Count(x => x.IsLowStock),
            paidOrders.Sum(x => x.Total),
            paidOrders.Where(x => x.CreatedAt.Date == today).Sum(x => x.Total),
            allOrders.OrderByDescending(x => x.CreatedAt).Take(8).Select(x => new DashboardRecentOrderDto(x.OrderNumber, x.CustomerName, x.Status.ToString(), OrderService.StatusLabel(x.Status), x.Total, x.CreatedAt)).ToArray())
        {
            PendingPaymentOrderCount = allOrders.Count(x => x.Status == OrderStatus.PendingPayment),
            PaidOrderCount = allOrders.Count(x => x.Status == OrderStatus.Paid),
            InEmbroideryOrderCount = allOrders.Count(x => x.Status == OrderStatus.InEmbroidery),
            QualityControlOrderCount = allOrders.Count(x => x.Status == OrderStatus.QualityControl),
            PackedOrderCount = allOrders.Count(x => x.Status == OrderStatus.Packed),
            ShippedOrderCount = allOrders.Count(x => x.Status == OrderStatus.Shipped),
            DeliveredOrderCount = allOrders.Count(x => x.Status == OrderStatus.Delivered),
            CancelledOrderCount = allOrders.Count(x => x.Status == OrderStatus.Cancelled),
            RefundedOrderCount = allOrders.Count(x => x.Status == OrderStatus.Refunded),
            PendingPaymentCount = pendingPaymentOrders.Length,
            PendingPaymentAmount = pendingPaymentOrders.Sum(x => x.Total),
            CurrentMonthSales = paidOrders.Where(x => x.CreatedAt >= currentMonthStart).Sum(x => x.Total),
            AverageOrderValue = paidOrders.Length == 0 ? 0m : Math.Round(paidOrders.Average(x => x.Total), 0),
            ReadyMadeProductCount = allProducts.Count(x => x.IsReadyMade),
            StudioProductCount = allProducts.Count(x => x.SupportsEmbroidery),
            OutOfStockVariantCount = variants.Count(x => x.IsActive && x.AvailableQuantity == 0),
            TotalAvailableStock = variants.Where(x => x.IsActive).Sum(x => x.AvailableQuantity),
            PendingReviewCount = pendingReviews.Count,
            UnansweredQuestionCount = pendingQuestions.Count,
            PendingArtworkCount = pendingArtworks.Count,
            NeedsRevisionArtworkCount = revisionArtworks.Count,
            OrderPipeline = pipeline,
            TopProducts = topProducts,
            ActionItems = BuildActionItems(pendingPaymentOrders, pendingReviews.Count, pendingQuestions.Count, pendingArtworks.Count, revisionArtworks.Count, variants)
        };
    }

    private static IReadOnlyCollection<DashboardStatusMetricDto> BuildPipeline(IReadOnlyCollection<Order> orders)
    {
        var steps = new[]
        {
            OrderStatus.PendingPayment,
            OrderStatus.Paid,
            OrderStatus.ArtworkReview,
            OrderStatus.InEmbroidery,
            OrderStatus.QualityControl,
            OrderStatus.Packed,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Cancelled,
            OrderStatus.Refunded
        };

        return steps.Select(status => new DashboardStatusMetricDto(
            status.ToString(),
            OrderService.StatusLabel(status),
            orders.Count(x => x.Status == status),
            orders.Where(x => x.Status == status).Sum(x => x.Total))).ToArray();
    }

    private static IReadOnlyCollection<DashboardTopProductDto> BuildTopProducts(IReadOnlyCollection<Order> paidOrders)
        => paidOrders
            .SelectMany(x => x.Lines)
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(x => new DashboardTopProductDto(x.Key.ProductId, x.Key.ProductName, x.Sum(l => l.Quantity), x.Sum(l => l.LineTotal)))
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.QuantitySold)
            .Take(5)
            .ToArray();

    private static IReadOnlyCollection<DashboardActionItemDto> BuildActionItems(
        IReadOnlyCollection<Order> pendingPaymentOrders,
        int pendingReviews,
        int pendingQuestions,
        int pendingArtworks,
        int revisionArtworks,
        IReadOnlyCollection<ProductVariant> variants)
    {
        var items = new List<DashboardActionItemDto>();
        var lowStock = variants.Count(x => x.IsLowStock);
        var outOfStock = variants.Count(x => x.IsActive && x.AvailableQuantity == 0);

        AddIf(items, pendingPaymentOrders.Count, "پرداخت‌های در انتظار", "سفارش‌هایی که هنوز پرداختشان نهایی نشده است.", "warning", "/admin/payments");
        AddIf(items, pendingArtworks, "طرح‌های در انتظار بررسی", "فایل‌های گلدوزی مشتری باید تأیید یا رد شوند.", "danger", "/admin/artworks");
        AddIf(items, revisionArtworks, "طرح‌های نیازمند اصلاح", "طرح‌هایی که باید دوباره توسط مشتری اصلاح شوند.", "warning", "/admin/artworks");
        AddIf(items, pendingQuestions, "پرسش‌های بی‌پاسخ", "پرسش‌های محصول هنوز پاسخ ادمین ندارند.", "warning", "/admin/questions");
        AddIf(items, pendingReviews, "نظرهای در انتظار تأیید", "نظرهای مشتری باید قبل از انتشار بررسی شوند.", "info", "/admin/reviews");
        AddIf(items, outOfStock, "SKUهای ناموجود", "تنوع‌هایی که موجودی قابل فروش ندارند.", "danger", "/admin/inventory");
        AddIf(items, lowStock, "SKUهای کم‌موجودی", "تنوع‌هایی که به آستانه هشدار موجودی رسیده‌اند.", "warning", "/admin/inventory");

        return items;
    }

    private static void AddIf(List<DashboardActionItemDto> items, int count, string title, string description, string severity, string link)
    {
        if (count > 0)
            items.Add(new DashboardActionItemDto(title, description, severity, link, count));
    }
}
