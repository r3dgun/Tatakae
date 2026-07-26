using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Controllers;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Api.Tests;

public sealed class CouponsControllerTests
{
    [Fact]
    public async Task Quote_ReturnsResultDtoPayload()
    {
        var coupon = new Coupon(Guid.NewGuid(), "WELCOME10", DiscountType.Percentage, 10m, DateTimeOffset.UtcNow.AddDays(-1), null, usageLimit: null, minimumOrderAmount: null);
        var controller = new CouponsController(new CouponService(new FakeCouponRepository(coupon)));

        ActionResult<ResultDto<CouponQuoteDto>> response = await controller.Quote(new CouponQuoteRequest
        {
            Code = "WELCOME10",
            CartSubtotal = 1_000_000m
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<ResultDto<CouponQuoteDto>>(ok.Value);
        Assert.True(result.IsSuccess);
        var quote = Assert.IsType<CouponQuoteDto>(result.Data);
        Assert.Equal(100_000m, quote.DiscountAmount);
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
            => Task.FromResult(new ResultDto<Coupon>().Success("ذخیره شد.", coupon));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("حذف شد."));
    }
}
