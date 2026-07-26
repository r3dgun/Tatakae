using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Tests;

public sealed class CouponTests
{
    [Fact]
    public void CalculateDiscount_ForPercentage_ReturnsRoundedDiscount()
    {
        var coupon = Coupon(DiscountType.Percentage, 10m);
        Assert.Equal(87_550m, coupon.CalculateDiscount(875_500m));
    }

    [Fact]
    public void CalculateDiscount_ForFixedAmount_CapsAtSubtotal()
    {
        var coupon = Coupon(DiscountType.FixedAmount, 500_000m);
        Assert.Equal(300_000m, coupon.CalculateDiscount(300_000m));
    }

    [Fact]
    public void Constructor_WhenPercentageExceedsOneHundred_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Coupon(DiscountType.Percentage, 101m));

    [Fact]
    public void IsUsable_WhenUsageLimitReached_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var coupon = Tatakae.Domain.Entities.Coupon.Rehydrate(Guid.NewGuid(), "USED", DiscountType.Percentage, 15m, now.AddDays(-1), now.AddDays(3), 2, 2, null, true);
        Assert.False(coupon.IsUsable(now, 1_000_000m));
    }

    [Fact]
    public void Redeem_WhenUsable_ReturnsDiscountAndIncreasesUsageCount()
    {
        var now = DateTimeOffset.UtcNow;
        var coupon = Coupon(DiscountType.Percentage, 10m, now);

        var discount = coupon.Redeem(now, 1_000_000m);

        Assert.Equal(100_000m, discount);
        Assert.Equal(1, coupon.UsageCount);
    }

    [Fact]
    public void Redeem_WhenNotUsable_DoesNotIncreaseUsageCount()
    {
        var now = DateTimeOffset.UtcNow;
        var coupon = new Coupon(Guid.NewGuid(), "FUTURE", DiscountType.Percentage, 10m, now.AddDays(1), null, null, null);

        Assert.Throws<InvalidOperationException>(() => coupon.Redeem(now, 1_000_000m));
        Assert.Equal(0, coupon.UsageCount);
    }

    private static Coupon Coupon(DiscountType type, decimal value, DateTimeOffset? now = null)
    {
        var point = now ?? DateTimeOffset.UtcNow;
        return new Coupon(Guid.NewGuid(), "WELCOME", type, value, point.AddDays(-1), point.AddDays(1), null, null);
    }
}
