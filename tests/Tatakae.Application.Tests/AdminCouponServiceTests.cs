using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class AdminCouponServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ReturnsSuccessfulResult()
    {
        var repository = new FakeCouponRepository();
        var service = new AdminCouponService(repository);

        var result = await service.CreateAsync(new AdminCouponRequest
        {
            Code = "WELCOME10",
            Type = nameof(DiscountType.Percentage),
            Value = 10,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddDays(5),
            IsActive = true
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("WELCOME10", result.Data.Code);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task CreateAsync_WhenCodeIsDuplicate_ReturnsFailedResult()
    {
        var existing = new Coupon(
            Guid.NewGuid(),
            "WELCOME10",
            DiscountType.Percentage,
            10,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            null,
            null);

        var service = new AdminCouponService(new FakeCouponRepository(existing));

        var result = await service.CreateAsync(new AdminCouponRequest
        {
            Code = "welcome10",
            Type = nameof(DiscountType.Percentage),
            Value = 15,
            StartsAt = DateTimeOffset.UtcNow
        });

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Contains("تکراری", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenCouponDoesNotExist_ReturnsRepositoryMessage()
    {
        var service = new AdminCouponService(new FakeCouponRepository());

        var result = await service.UpdateAsync(Guid.NewGuid(), new AdminCouponRequest
        {
            Code = "NEWCODE",
            Type = nameof(DiscountType.FixedAmount),
            Value = 100_000,
            StartsAt = DateTimeOffset.UtcNow
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("کد تخفیف پیدا نشد.", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryFails_PropagatesFailure()
    {
        var service = new AdminCouponService(new FailingDeleteCouponRepository());

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("حذف کد تخفیف ممکن نیست.", result.Message);
    }

    private sealed class FakeCouponRepository(params Coupon[] coupons) : ICouponRepository
    {
        public List<Coupon> Items { get; } = coupons.ToList();

        public Task<ResultDto<IReadOnlyCollection<Coupon>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Coupon> data = Items;
            return Task.FromResult(new ResultDto<IReadOnlyCollection<Coupon>>().Success("دریافت شد.", data));
        }

        public Task<ResultDto<Coupon>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var coupon = Items.SingleOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
            var result = new ResultDto<Coupon>();
            return Task.FromResult(coupon is null ? result.Failed("کد تخفیف پیدا نشد.") : result.Success("دریافت شد.", coupon));
        }

        public Task<ResultDto<Coupon>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var coupon = Items.SingleOrDefault(x => x.Id == id);
            var result = new ResultDto<Coupon>();
            return Task.FromResult(coupon is null ? result.Failed("کد تخفیف پیدا نشد.") : result.Success("دریافت شد.", coupon));
        }

        public Task<ResultDto<Coupon>> UpsertAsync(Coupon coupon, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Id == coupon.Id);
            Items.Add(coupon);
            return Task.FromResult(new ResultDto<Coupon>().Success("ذخیره شد.", coupon));
        }

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var removed = Items.RemoveAll(x => x.Id == id) > 0;
            var result = new ResultDto();
            return Task.FromResult(removed ? result.Success("حذف شد.") : result.Failed("کد تخفیف پیدا نشد."));
        }
    }

    private sealed class FailingDeleteCouponRepository : ICouponRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Coupon>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Coupon>>().Success("دریافت شد.", Array.Empty<Coupon>()));

        public Task<ResultDto<Coupon>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Coupon>().Failed("کد تخفیف پیدا نشد."));

        public Task<ResultDto<Coupon>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Coupon>().Failed("کد تخفیف پیدا نشد."));

        public Task<ResultDto<Coupon>> UpsertAsync(Coupon coupon, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Coupon>().Failed("ذخیره انجام نشد."));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Failed("حذف کد تخفیف ممکن نیست."));
    }
}
