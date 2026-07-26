# Phase 05 - Coupon Tests

در این مرحله تست‌های مربوط به کد تخفیف اضافه شد.

## Domain Tests

فایل:

```text
tests/Tatakae.Domain.Tests/CouponTests.cs
```

پوشش تست:

- محاسبه تخفیف درصدی
- سقف تخفیف مبلغ ثابت تا مبلغ سبد
- غیرقابل استفاده بودن کد قبل از زمان شروع
- غیرقابل استفاده بودن کد بعد از پر شدن ظرفیت استفاده
- افزایش UsageCount بعد از مصرف کد

## Application Tests

فایل:

```text
tests/Tatakae.Application.Tests/CouponServiceTests.cs
```

پوشش تست:

- quote موفق برای کد معتبر
- پیام خطا برای حداقل مبلغ سفارش ناکافی
- پیام خطا برای کد ناموجود
- سقف‌گذاری تخفیف ثابت وقتی از جمع سبد بیشتر است

## API Tests

فایل:

```text
tests/Tatakae.Api.Tests/CouponsControllerTests.cs
```

پوشش تست:

- خروجی درست API `POST /api/coupons/quote`

## اجرای تست‌ها

```powershell
dotnet test .\Tatakae.sln
```
