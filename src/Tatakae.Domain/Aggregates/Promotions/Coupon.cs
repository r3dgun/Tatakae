using Tatakae.Domain.Common;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Entities;

/// <summary>Coupon aggregate. Validity and redemption rules belong to the domain.</summary>
public sealed class Coupon
{
    public Coupon(
        Guid id,
        string code,
        DiscountType type,
        decimal value,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        int? usageLimit,
        decimal? minimumOrderAmount,
        bool isActive = true)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه کد تخفیف معتبر نیست.");
        Code = DomainGuard.Required(code, nameof(code), "کد تخفیف الزامی است.").ToUpperInvariant();
        Type = type;
        Value = DomainGuard.Positive(value, nameof(value), "مقدار تخفیف باید بیشتر از صفر باشد.");
        if (type == DiscountType.Percentage && value > 100m)
            throw new ArgumentOutOfRangeException(nameof(value), value, "درصد تخفیف نمی‌تواند بیشتر از ۱۰۰ باشد.");
        if (endsAt is not null && endsAt < startsAt)
            throw new ArgumentException("زمان پایان تخفیف نمی‌تواند قبل از زمان شروع باشد.", nameof(endsAt));
        if (usageLimit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(usageLimit), usageLimit, "سقف استفاده باید بیشتر از صفر باشد.");
        if (minimumOrderAmount is < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumOrderAmount), minimumOrderAmount, "حداقل مبلغ سفارش نمی‌تواند منفی باشد.");

        StartsAt = startsAt;
        EndsAt = endsAt;
        UsageLimit = usageLimit;
        MinimumOrderAmount = minimumOrderAmount;
        IsActive = isActive;
    }

    public Guid Id { get; }
    public string Code { get; }
    public DiscountType Type { get; }
    public decimal Value { get; }
    public DateTimeOffset StartsAt { get; }
    public DateTimeOffset? EndsAt { get; }
    public int? UsageLimit { get; }
    public int UsageCount { get; private set; }
    public decimal? MinimumOrderAmount { get; }
    public bool IsActive { get; private set; }

    public static Coupon Rehydrate(
        Guid id,
        string code,
        DiscountType type,
        decimal value,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        int? usageLimit,
        int usageCount,
        decimal? minimumOrderAmount,
        bool isActive)
    {
        var coupon = new Coupon(id, code, type, value, startsAt, endsAt, usageLimit, minimumOrderAmount, isActive);
        coupon.UsageCount = DomainGuard.NonNegative(usageCount, nameof(usageCount), "تعداد استفاده نمی‌تواند منفی باشد.");
        if (usageLimit is not null && coupon.UsageCount > usageLimit)
            throw new ArgumentException("تعداد استفاده از سقف مجاز بیشتر است.", nameof(usageCount));
        return coupon;
    }

    public bool IsUsable(DateTimeOffset now, decimal subtotal)
    {
        DomainGuard.NonNegative(subtotal, nameof(subtotal), "مبلغ سبد نمی‌تواند منفی باشد.");
        return IsActive
               && now >= StartsAt
               && (EndsAt is null || now <= EndsAt)
               && (UsageLimit is null || UsageCount < UsageLimit)
               && (MinimumOrderAmount is null || subtotal >= MinimumOrderAmount);
    }

    public decimal CalculateDiscount(decimal subtotal)
    {
        DomainGuard.NonNegative(subtotal, nameof(subtotal), "مبلغ سبد نمی‌تواند منفی باشد.");
        return Type == DiscountType.Percentage
            ? Math.Min(subtotal, Math.Round(subtotal * Value / 100m, 0))
            : Math.Min(Value, subtotal);
    }

    public decimal Redeem(DateTimeOffset now, decimal subtotal)
    {
        if (!IsUsable(now, subtotal))
            throw new InvalidOperationException("کد تخفیف معتبر نیست یا شرایط استفاده از آن فراهم نیست.");

        UsageCount++;
        return CalculateDiscount(subtotal);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
