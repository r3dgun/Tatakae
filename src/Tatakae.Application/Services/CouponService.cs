using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Application.Services;

public sealed class CouponService(
    ICouponRepository coupons,
    ILogger<CouponService>? logger = null) : ICouponService
{
    private readonly ILogger<CouponService> _logger = logger ?? NullLogger<CouponService>.Instance;

    public async Task<ResultDto<CouponQuoteDto>> QuoteAsync(
        CouponQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<CouponQuoteDto>();

        try
        {
            if (request is null)
                return result.ValidationFailed("اطلاعات کد تخفیف ارسال نشده است.");

            var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
                return result.ValidationFailed("کد تخفیف را وارد کن.");

            if (request.CartSubtotal <= 0)
                return result.ValidationFailed("برای اعمال کد تخفیف، سبد خرید باید مبلغ داشته باشد.");

            var repositoryResult = await coupons.GetByCodeAsync(code, cancellationToken);
            if (!repositoryResult.IsSuccess || repositoryResult.Data is null)
                return result.Failed(repositoryResult.Message, repositoryResult.Status, repositoryResult.ErrorCode);

            var coupon = repositoryResult.Data;
            var now = DateTimeOffset.UtcNow;

            if (!coupon.IsActive)
                return result.Failed("این کد تخفیف غیرفعال است.");

            if (now < coupon.StartsAt)
                return result.Failed("زمان شروع این کد تخفیف هنوز نرسیده است.");

            if (coupon.EndsAt is not null && now > coupon.EndsAt)
                return result.Failed("مهلت استفاده از این کد تخفیف تمام شده است.");

            if (coupon.UsageLimit is not null && coupon.UsageCount >= coupon.UsageLimit)
                return result.Failed("ظرفیت استفاده از این کد تخفیف تکمیل شده است.");

            if (coupon.MinimumOrderAmount is not null && request.CartSubtotal < coupon.MinimumOrderAmount)
                return result.Failed($"حداقل مبلغ سفارش برای این کد {coupon.MinimumOrderAmount:N0} تومان است.");

            var discount = Math.Min(coupon.CalculateDiscount(request.CartSubtotal), request.CartSubtotal);
            var quote = new CouponQuoteDto(
                coupon.Code,
                true,
                "کد تخفیف با موفقیت اعمال شد.",
                request.CartSubtotal,
                discount,
                Math.Max(0, request.CartSubtotal - discount),
                coupon.Type.ToString(),
                coupon.Value,
                coupon.MinimumOrderAmount,
                coupon.UsageLimit,
                coupon.UsageCount,
                coupon.EndsAt);

            return result.Success("کد تخفیف با موفقیت اعمال شد.", quote);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بررسی کد تخفیف {Code}", request?.Code);
            return result.Failed("خطایی در بررسی کد تخفیف رخ داده است.");
        }
    }
}
