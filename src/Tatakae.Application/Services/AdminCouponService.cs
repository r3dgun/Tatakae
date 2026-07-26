using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Coupons;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Services;

public sealed class AdminCouponService(
    ICouponRepository coupons,
    ILogger<AdminCouponService>? logger = null) : IAdminCouponService
{
    private readonly ILogger<AdminCouponService> _logger = logger ?? NullLogger<AdminCouponService>.Instance;

    public async Task<ResultDto<IReadOnlyCollection<CouponDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<IReadOnlyCollection<CouponDto>>();

        try
        {
            var repositoryResult = await coupons.GetAllAsync(cancellationToken);
            if (!repositoryResult.IsSuccess || repositoryResult.Data is null)
                return result.Failed(repositoryResult.Message, repositoryResult.Status, repositoryResult.ErrorCode);

            var data = repositoryResult.Data
                .OrderByDescending(x => x.StartsAt)
                .Select(Map)
                .ToArray();

            return result.Success("کدهای تخفیف با موفقیت دریافت شدند.", data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت کدهای تخفیف");
            return result.Failed("خطایی در دریافت کدهای تخفیف رخ داده است.");
        }
    }

    public async Task<ResultDto<CouponDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<CouponDto>();

        try
        {
            if (id == Guid.Empty)
                return result.ValidationFailed("شناسه کد تخفیف معتبر نیست.");

            var repositoryResult = await coupons.GetByIdAsync(id, cancellationToken);
            if (!repositoryResult.IsSuccess || repositoryResult.Data is null)
                return result.Failed(repositoryResult.Message, repositoryResult.Status, repositoryResult.ErrorCode);

            return result.Success("کد تخفیف با موفقیت دریافت شد.", Map(repositoryResult.Data));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت کد تخفیف {CouponId}", id);
            return result.Failed("خطایی در دریافت کد تخفیف رخ داده است.");
        }
    }

    public async Task<ResultDto<CouponDto>> CreateAsync(
        AdminCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<CouponDto>();

        try
        {
            if (request is null)
                return result.ValidationFailed("اطلاعات کد تخفیف ارسال نشده است.");

            var validationMessage = ValidateRequest(request);
            if (validationMessage is not null)
                return result.ValidationFailed(validationMessage);

            var uniqueResult = await EnsureUniqueCodeAsync(request.Code, null, cancellationToken);
            if (!uniqueResult.IsSuccess)
                return result.Failed(uniqueResult.Message, uniqueResult.Status, uniqueResult.ErrorCode);

            var coupon = Build(Guid.NewGuid(), request);
            var repositoryResult = await coupons.UpsertAsync(coupon, cancellationToken);
            if (!repositoryResult.IsSuccess || repositoryResult.Data is null)
                return result.Failed(repositoryResult.Message, repositoryResult.Status, repositoryResult.ErrorCode);

            return result.Success("کد تخفیف با موفقیت ایجاد شد.", Map(repositoryResult.Data));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد کد تخفیف {Code}", request?.Code);
            return result.Failed("خطایی در ایجاد کد تخفیف رخ داده است.");
        }
    }

    public async Task<ResultDto<CouponDto>> UpdateAsync(
        Guid id,
        AdminCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<CouponDto>();

        try
        {
            if (id == Guid.Empty)
                return result.ValidationFailed("شناسه کد تخفیف معتبر نیست.");

            if (request is null)
                return result.ValidationFailed("اطلاعات کد تخفیف برای به‌روزرسانی ارسال نشده است.");

            var validationMessage = ValidateRequest(request);
            if (validationMessage is not null)
                return result.ValidationFailed(validationMessage);

            var existingResult = await coupons.GetByIdAsync(id, cancellationToken);
            if (!existingResult.IsSuccess || existingResult.Data is null)
                return result.Failed(existingResult.Message, existingResult.Status, existingResult.ErrorCode);

            var uniqueResult = await EnsureUniqueCodeAsync(request.Code, id, cancellationToken);
            if (!uniqueResult.IsSuccess)
                return result.Failed(uniqueResult.Message, uniqueResult.Status, uniqueResult.ErrorCode);

            var coupon = Build(id, request);
            var repositoryResult = await coupons.UpsertAsync(coupon, cancellationToken);
            if (!repositoryResult.IsSuccess || repositoryResult.Data is null)
                return result.Failed(repositoryResult.Message, repositoryResult.Status, repositoryResult.ErrorCode);

            return result.Success("کد تخفیف با موفقیت به‌روزرسانی شد.", Map(repositoryResult.Data));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در به‌روزرسانی کد تخفیف {CouponId}", id);
            return result.Failed("خطایی در به‌روزرسانی کد تخفیف رخ داده است.");
        }
    }

    public async Task<ResultDto> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto();

        try
        {
            if (id == Guid.Empty)
                return result.ValidationFailed("شناسه کد تخفیف معتبر نیست.");

            var repositoryResult = await coupons.DeleteAsync(id, cancellationToken);
            if (!repositoryResult.IsSuccess)
                return result.Failed(repositoryResult.Message, repositoryResult.Status, repositoryResult.ErrorCode);

            return result.Success("کد تخفیف با موفقیت حذف شد.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف کد تخفیف {CouponId}", id);
            return result.Failed("خطایی در حذف کد تخفیف رخ داده است.");
        }
    }

    private async Task<ResultDto> EnsureUniqueCodeAsync(
        string code,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        var result = new ResultDto();
        var repositoryResult = await coupons.GetAllAsync(cancellationToken);

        if (!repositoryResult.IsSuccess || repositoryResult.Data is null)
            return result.Failed(repositoryResult.Message, repositoryResult.Status, repositoryResult.ErrorCode);

        var duplicate = repositoryResult.Data.Any(x =>
            x.Id != currentId
            && string.Equals(x.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        return duplicate
            ? result.Conflict("کد تخفیف تکراری است.")
            : result.Success("کد تخفیف یکتا است.");
    }

    private static string? ValidateRequest(AdminCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return "کد تخفیف الزامی است.";

        if (request.Value <= 0)
            return "مقدار تخفیف باید بیشتر از صفر باشد.";

        if (!Enum.TryParse<DiscountType>(request.Type, true, out var type))
            return "نوع تخفیف معتبر نیست.";

        if (type == DiscountType.Percentage && request.Value > 100)
            return "درصد تخفیف نمی‌تواند بیشتر از ۱۰۰ باشد.";

        if (request.EndsAt is not null && request.EndsAt <= request.StartsAt)
            return "زمان پایان باید بعد از زمان شروع باشد.";

        if (request.UsageLimit is <= 0)
            return "محدودیت استفاده باید بیشتر از صفر باشد.";

        if (request.MinimumOrderAmount is < 0)
            return "حداقل مبلغ سفارش نمی‌تواند منفی باشد.";

        return null;
    }

    private static Coupon Build(Guid id, AdminCouponRequest request)
    {
        _ = Enum.TryParse<DiscountType>(request.Type, true, out var type);
        return new Coupon(
            id,
            request.Code.Trim().ToUpperInvariant(),
            type,
            request.Value,
            request.StartsAt,
            request.EndsAt,
            request.UsageLimit,
            request.MinimumOrderAmount,
            request.IsActive);
    }

    private static CouponDto Map(Coupon coupon)
        => new(
            coupon.Id,
            coupon.Code,
            coupon.Type.ToString(),
            coupon.Value,
            coupon.StartsAt,
            coupon.EndsAt,
            coupon.UsageLimit,
            coupon.UsageCount,
            coupon.MinimumOrderAmount,
            coupon.IsActive);
}
