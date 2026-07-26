using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Admin;

public sealed record AdminDashboardDto(
    int ProductCount,
    int PublishedProductCount,
    int CategoryCount,
    int OrderCount,
    int OrdersAwaitingArtworkReview,
    int LowStockVariantCount,
    decimal GrossSales,
    decimal TodaySales,
    IReadOnlyCollection<DashboardRecentOrderDto> RecentOrders)
{
    public int PendingPaymentOrderCount { get; init; }
    public int PaidOrderCount { get; init; }
    public int InEmbroideryOrderCount { get; init; }
    public int QualityControlOrderCount { get; init; }
    public int PackedOrderCount { get; init; }
    public int ShippedOrderCount { get; init; }
    public int DeliveredOrderCount { get; init; }
    public int CancelledOrderCount { get; init; }
    public int RefundedOrderCount { get; init; }

    public int PendingPaymentCount { get; init; }
    public decimal PendingPaymentAmount { get; init; }
    public decimal CurrentMonthSales { get; init; }
    public decimal AverageOrderValue { get; init; }

    public int ReadyMadeProductCount { get; init; }
    public int StudioProductCount { get; init; }
    public int OutOfStockVariantCount { get; init; }
    public int TotalAvailableStock { get; init; }

    public int PendingReviewCount { get; init; }
    public int UnansweredQuestionCount { get; init; }
    public int PendingArtworkCount { get; init; }
    public int NeedsRevisionArtworkCount { get; init; }

    public IReadOnlyCollection<DashboardStatusMetricDto> OrderPipeline { get; init; } = Array.Empty<DashboardStatusMetricDto>();
    public IReadOnlyCollection<DashboardTopProductDto> TopProducts { get; init; } = Array.Empty<DashboardTopProductDto>();
    public IReadOnlyCollection<DashboardActionItemDto> ActionItems { get; init; } = Array.Empty<DashboardActionItemDto>();
}

public sealed record DashboardRecentOrderDto(string OrderNumber, string CustomerName, string Status, string StatusLabel, decimal Total, DateTimeOffset CreatedAt);

public sealed record DashboardStatusMetricDto(string Status, string Label, int Count, decimal Amount);

public sealed record DashboardTopProductDto(Guid ProductId, string ProductName, int QuantitySold, decimal Revenue);

public sealed record DashboardActionItemDto(string Title, string Description, string Severity, string Link, int Count);

public sealed class AdminOrderStatusRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^(PendingPayment|Paid|ArtworkReview|InEmbroidery|QualityControl|Packed|Shipped|Delivered|Cancelled|Refunded)$")]
    public string Status { get; set; } = "Paid";

    [StringLength(100)]
    public string? TrackingCode { get; set; }

    [StringLength(1000)]
    public string? AdminNote { get; set; }

    /// <summary>فقط برای ادمین ارشد؛ اجازه عبور از ترتیب معمول وضعیت‌ها.</summary>
    public bool Force { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((Status == "Shipped" || Status == "Delivered") && string.IsNullOrWhiteSpace(TrackingCode))
        {
            yield return new ValidationResult("برای سفارش ارسال‌شده، کد رهگیری را وارد کنید.", [nameof(TrackingCode)]);
        }
    }
}

public sealed class AdminGridQueryDto
{
    [StringLength(120)]
    public string? Search { get; set; }

    [Range(1, 2000)]
    public int Page { get; set; } = 1;

    [Range(5, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(60)]
    public string SortBy { get; set; } = "createdAt";

    public bool Desc { get; set; } = true;
}

public sealed record AdminProductRowDto(
    Guid Id,
    string Name,
    string Slug,
    string CategoryName,
    string ApparelCategory,
    string PrimaryImageUrl,
    decimal StartingPrice,
    int VariantCount,
    int TotalStock,
    bool IsPublished,
    bool IsFeatured,
    bool SupportsEmbroidery,
    DateTimeOffset UpdatedAt);

public sealed record AdminOrderRowDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string CustomerMobile,
    string Status,
    string StatusLabel,
    string PaymentStatus,
    int ItemCount,
    decimal Total,
    DateTimeOffset CreatedAt,
    string? TrackingCode);

public sealed record AdminCustomerRowDto(
    Guid Id,
    string FullName,
    string Mobile,
    string? Email,
    int OrderCount,
    decimal LifetimeValue,
    DateTimeOffset CreatedAt);

public sealed record AdminCouponRowDto(
    Guid Id,
    string Code,
    string Type,
    decimal Value,
    int UsageCount,
    int? UsageLimit,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    bool IsActive);
