# Phase 14 — Seed Tests

تست‌های این فاز در پروژه `tests/Tatakae.Api.Tests` قرار دارند.

## DevelopmentSeedCatalogTests

- وجود محصول آماده
- وجود محصول قابل شخصی‌سازی
- وجود محصول تخفیف‌خورده
- وجود محصول با موجودی صفر
- یکتایی slug، SKU و شناسه‌ها
- ثابت ماندن شناسه‌ها بین دو بار ساخت catalog
- وجود آدرس پیش‌فرض معتبر
- ثابت بودن شماره و شناسه سفارش
- وجود حساب‌های مشتری و ادمین

## StoreDataSeederTests

- idempotent بودن اجرای دوباره Seed
- ثبت سفارش، آدرس و دو وضعیت پرسش‌وپاسخ
- تعمیر محصول Seed شده با variant حذف‌شده
- حفظ دسته‌ها و داده‌های ساخته‌شده خارج از Seed
- بررسی مستقیم وضعیت موجودی و تخفیف در EF InMemory

## DevelopmentIdentitySeederTests

- ساخت حساب‌های مشتری و ادمین
- تخصیص Role صحیح
- اتصال حساب مشتری به Customer تستی
- جلوگیری از تکرار کاربران در اجرای دوم
- بازیابی رمز Development هنگام فعال بودن reset

## اجرا

```powershell
dotnet test .\Tatakae.sln
```

یا:

```powershell
.\scripts\tests\run-tests.ps1
```

## SeedConfigurationContractTests

- پیش‌فرض امن `SeedDataOptions`
- غیرفعال بودن fixtureهای دمو در `appsettings.json`
- فعال بودن fixtureها و reset رمز فقط در `appsettings.Development.json`
