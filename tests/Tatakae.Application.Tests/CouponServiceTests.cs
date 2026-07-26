using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class CouponServiceTests
{
    [Fact]
    public async Task QuoteAsync_WhenCouponIsValid_ReturnsDiscountAndPayableSubtotal()
    {
        var coupon = new Coupon(Guid.NewGuid(), "WELCOME10", DiscountType.Percentage, 10m, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), usageLimit: 10, minimumOrderAmount: 500_000m);
        var service = new CouponService(new FakeCouponRepository(coupon));

        var result = await service.QuoteAsync(new CouponQuoteRequest { Code = "welcome10", CartSubtotal = 1_200_000m });

        Assert.True(result.IsSuccess);
        var quote = Assert.IsType<CouponQuoteDto>(result.Data);
        Assert.True(quote.IsValid);
        Assert.Equal("WELCOME10", quote.Code);
        Assert.Equal(120_000m, quote.DiscountAmount);
        Assert.Equal(1_080_000m, quote.PayableSubtotal);
        Assert.Equal("کد تخفیف با موفقیت اعمال شد.", result.Message);
    }

    [Fact]
    public async Task QuoteAsync_WhenMinimumOrderIsNotReached_ReturnsFailedResult()
    {
        var coupon = new Coupon(Guid.NewGuid(), "BIGSALE", DiscountType.FixedAmount, 200_000m, DateTimeOffset.UtcNow.AddDays(-1), null, usageLimit: null, minimumOrderAmount: 1_000_000m);
        var service = new CouponService(new FakeCouponRepository(coupon));

        var result = await service.QuoteAsync(new CouponQuoteRequest { Code = "BIGSALE", CartSubtotal = 800_000m });

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Contains("حداقل مبلغ سفارش", result.Message);
    }

    [Fact]
    public async Task QuoteAsync_WhenCouponDoesNotExist_ReturnsFailedResult()
    {
        var service = new CouponService(new FakeCouponRepository());

        var result = await service.QuoteAsync(new CouponQuoteRequest { Code = "NOTFOUND", CartSubtotal = 900_000m });

        Assert.False(result.IsSuccess);
        Assert.Equal("کد تخفیف پیدا نشد.", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task QuoteAsync_WhenFixedAmountIsMoreThanSubtotal_CapsDiscount()
    {
        var coupon = new Coupon(Guid.NewGuid(), "SAVEALL", DiscountType.FixedAmount, 500_000m, DateTimeOffset.UtcNow.AddDays(-1), null, usageLimit: null, minimumOrderAmount: null);
        var service = new CouponService(new FakeCouponRepository(coupon));

        var result = await service.QuoteAsync(new CouponQuoteRequest { Code = "SAVEALL", CartSubtotal = 320_000m });

        Assert.True(result.IsSuccess);
        var quote = Assert.IsType<CouponQuoteDto>(result.Data);
        Assert.Equal(320_000m, quote.DiscountAmount);
        Assert.Equal(0m, quote.PayableSubtotal);
    }

    private sealed class FakeCouponRepository(params Coupon[] coupons) : ICouponRepository
    {
        private readonly List<Coupon> _coupons = coupons.ToList();

        public Task<ResultDto<IReadOnlyCollection<Coupon>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Coupon> data = _coupons;
            return Task.FromResult(new ResultDto<IReadOnlyCollection<Coupon>>().Success("دریافت شد.", data));
        }

        public Task<ResultDto<Coupon>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var coupon = _coupons.SingleOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
            var result = new ResultDto<Coupon>();
            return Task.FromResult(coupon is null ? result.Failed("کد تخفیف پیدا نشد.") : result.Success("دریافت شد.", coupon));
        }

        public Task<ResultDto<Coupon>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var coupon = _coupons.SingleOrDefault(x => x.Id == id);
            var result = new ResultDto<Coupon>();
            return Task.FromResult(coupon is null ? result.Failed("کد تخفیف پیدا نشد.") : result.Success("دریافت شد.", coupon));
        }

        public Task<ResultDto<Coupon>> UpsertAsync(Coupon coupon, CancellationToken cancellationToken = default)
        {
            _coupons.RemoveAll(x => x.Id == coupon.Id);
            _coupons.Add(coupon);
            return Task.FromResult(new ResultDto<Coupon>().Success("ذخیره شد.", coupon));
        }

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var removed = _coupons.RemoveAll(x => x.Id == id) > 0;
            var result = new ResultDto();
            return Task.FromResult(removed ? result.Success("حذف شد.") : result.Failed("کد تخفیف پیدا نشد."));
        }
    }
}
