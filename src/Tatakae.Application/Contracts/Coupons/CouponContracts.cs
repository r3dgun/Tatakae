using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Coupons;

public sealed record CouponDto(Guid Id, string Code, string Type, decimal Value, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, int? UsageLimit, int UsageCount, decimal? MinimumOrderAmount, bool IsActive);

public sealed class AdminCouponRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^[A-Za-z0-9-]{3,30}$")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Percentage|FixedAmount)$")]
    public string Type { get; set; } = "Percentage";

    [Range(typeof(decimal), "1", "999999999")]
    public decimal Value { get; set; }

    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndsAt { get; set; }

    [Range(1, 9999999)]
    public int? UsageLimit { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? MinimumOrderAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == "Percentage" && Value > 100)
        {
            yield return new ValidationResult("درصد تخفیف نمی‌تواند بیشتر از ۱۰۰ باشد.", [nameof(Value)]);
        }

        if (EndsAt.HasValue && EndsAt.Value <= StartsAt)
        {
            yield return new ValidationResult("زمان پایان باید بعد از زمان شروع باشد.", [nameof(EndsAt), nameof(StartsAt)]);
        }
    }
}

public sealed class CouponQuoteRequest
{
    [Required]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal CartSubtotal { get; set; }
}

public sealed record CouponQuoteDto(
    string Code,
    bool IsValid,
    string Message,
    decimal CartSubtotal,
    decimal DiscountAmount,
    decimal PayableSubtotal,
    string? Type,
    decimal? Value,
    decimal? MinimumOrderAmount,
    int? UsageLimit,
    int UsageCount,
    DateTimeOffset? EndsAt);
