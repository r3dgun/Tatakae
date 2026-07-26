using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Infrastructure.Repositories;

public sealed class InMemoryCouponRepository(
    ILogger<InMemoryCouponRepository>? logger = null) : ICouponRepository
{
    private readonly ConcurrentDictionary<Guid, Coupon> _data = new(
        StoreSeed.CreateCoupons().ToDictionary(x => x.Id));

    private readonly ILogger<InMemoryCouponRepository> _logger =
        logger ?? NullLogger<InMemoryCouponRepository>.Instance;

    public Task<ResultDto<IReadOnlyCollection<Coupon>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<Coupon>>();

        try
        {
            IReadOnlyCollection<Coupon> data = _data.Values.ToArray();
            return Task.FromResult(result.Success("کدهای تخفیف دریافت شدند.", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت کدهای تخفیف از حافظه");
            return Task.FromResult(result.Failed("خطایی در دریافت کدهای تخفیف رخ داده است.", ResultStatus.Failure, "repository_failure"));
        }
    }

    public Task<ResultDto<Coupon>> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Coupon>();

        try
        {
            if (string.IsNullOrWhiteSpace(code))
                return Task.FromResult(result.ValidationFailed("کد تخفیف معتبر نیست.", "invalid_coupon_code"));

            var coupon = _data.Values.SingleOrDefault(x =>
                string.Equals(x.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(coupon is null
                ? result.NotFound("کد تخفیف پیدا نشد.", "coupon_not_found")
                : result.Success("کد تخفیف دریافت شد.", coupon));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت کد تخفیف {Code} از حافظه", code);
            return Task.FromResult(result.Failed("خطایی در دریافت کد تخفیف رخ داده است.", ResultStatus.Failure, "repository_failure"));
        }
    }

    public Task<ResultDto<Coupon>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Coupon>();

        try
        {
            if (id == Guid.Empty)
                return Task.FromResult(result.ValidationFailed("شناسه کد تخفیف معتبر نیست.", "invalid_coupon_id"));

            return Task.FromResult(_data.TryGetValue(id, out var coupon)
                ? result.Success("کد تخفیف دریافت شد.", coupon)
                : result.NotFound("کد تخفیف پیدا نشد.", "coupon_not_found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت کد تخفیف {CouponId} از حافظه", id);
            return Task.FromResult(result.Failed("خطایی در دریافت کد تخفیف رخ داده است.", ResultStatus.Failure, "repository_failure"));
        }
    }

    public Task<ResultDto<Coupon>> UpsertAsync(
        Coupon coupon,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<Coupon>();

        try
        {
            if (coupon is null)
                return Task.FromResult(result.ValidationFailed("اطلاعات کد تخفیف ارسال نشده است.", "coupon_required"));

            _data[coupon.Id] = coupon;
            return Task.FromResult(result.Success("کد تخفیف ذخیره شد.", coupon));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ذخیره کد تخفیف {CouponId} در حافظه", coupon?.Id);
            return Task.FromResult(result.Failed("خطایی در ذخیره کد تخفیف رخ داده است.", ResultStatus.Failure, "repository_failure"));
        }
    }

    public Task<ResultDto> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto();

        try
        {
            if (id == Guid.Empty)
                return Task.FromResult(result.ValidationFailed("شناسه کد تخفیف معتبر نیست.", "invalid_coupon_id"));

            return Task.FromResult(_data.TryRemove(id, out _)
                ? result.Success("کد تخفیف حذف شد.")
                : result.NotFound("کد تخفیف پیدا نشد.", "coupon_not_found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف کد تخفیف {CouponId} از حافظه", id);
            return Task.FromResult(result.Failed("خطایی در حذف کد تخفیف رخ داده است.", ResultStatus.Failure, "repository_failure"));
        }
    }
}
