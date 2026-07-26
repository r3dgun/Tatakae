# Phase 04 - Iran locations seed

این نسخه Seed Data استان‌ها و شهرهای ایران را به دیتابیس اضافه می‌کند تا Checkout و دفترچه آدرس از داده واقعی استفاده کنند.

## فایل‌های اصلی

- `src/Tatakae.Infrastructure/Seeding/IranLocationSeed.cs`
- `src/Tatakae.Infrastructure/Persistence/DatabaseInitializer.cs`
- `src/Tatakae.Api/Controllers/LocationsController.cs`
- `src/Tatakae.Application/Contracts/Lookups/LocationContracts.cs`
- `src/Tatakae.Web/Pages/Checkout.razor`
- `src/Tatakae.Web/Pages/Account/Addresses.razor`

## API

```http
GET /api/locations
GET /api/locations/provinces
GET /api/locations/cities?province=تهران
```

## رفتار Seed

- ۳۱ استان ایران seed می‌شوند.
- برای هر استان شهرهای اصلی و پرکاربرد seed می‌شوند.
- Seed idempotent است؛ اجرای دوباره برنامه رکورد تکراری نمی‌سازد.
- اگر استان/شهر قبلاً وجود داشته باشد فقط فعال و همگام‌سازی می‌شود.

## استفاده در UI

- در `/account/addresses` فیلد استان/شهر از input متنی به select تبدیل شده است.
- در `/checkout` فیلد استان/شهر از input متنی به select وابسته تبدیل شده است.
- با تغییر استان، لیست شهرها خودکار عوض می‌شود.
- تغییر استان/شهر، روش‌های ارسال را دوباره محاسبه می‌کند.
