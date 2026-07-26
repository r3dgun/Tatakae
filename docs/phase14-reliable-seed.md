# Phase 14 — Reliable Development Seed

این فاز داده‌های لازم برای توسعه، تست دستی و تست خودکار را به‌صورت پایدار و تکرارپذیر فراهم می‌کند.

## سناریوهای آماده

| سناریو | رکورد Seed |
|---|---|
| محصول قابل شخصی‌سازی | `premium-cotton-embroidered-tshirt` |
| محصول آماده | `ready-dragon-mark-embroidered-tshirt` |
| محصول تخفیف‌خورده | `essential-fleece-embroidered-hoodie` |
| محصول ناموجود | `out-of-stock-embroidered-tshirt` |
| سفارش تستی | `EMB-TEST-0001` |
| آدرس تستی | آدرس پیش‌فرض مشتری تست در تهران |
| پرسش پاسخ‌داده‌شده | سؤال درباره لوگوی دو رنگ روی محصول قابل شخصی‌سازی |
| پرسش در انتظار | سؤال موجودی مجدد محصول آماده |
| کاربر مشتری | `09121234567` / `Customer@123456` |
| کاربر ادمین | `09120000000` / `Admin@123456` |

حساب ادمین دوم برای سازگاری با نسخه‌های قبلی نیز باقی مانده است:

```text
09123456789 / Admin@123456
```

## تنظیمات محیط

Seed اصلی با بخش زیر کنترل می‌شود:

```json
"SeedData": {
  "Enabled": true,
  "IncludeDevelopmentFixtures": false,
  "ResetDevelopmentPasswords": false
}
```

در `appsettings.Development.json` داده‌های Development فعال‌اند:

```json
"SeedData": {
  "Enabled": true,
  "IncludeDevelopmentFixtures": true,
  "ResetDevelopmentPasswords": true
}
```

بنابراین حساب‌های دمو، سفارش، آدرس و پرسش‌های تستی در Production به‌طور پیش‌فرض ساخته نمی‌شوند.

## ویژگی‌های فنی

- شناسه محصولات، SKUها، تصاویر، سفارش، آدرس و پرسش‌ها ثابت است.
- تاریخ‌های Seed ثابت‌اند و به زمان اجرای برنامه وابسته نیستند.
- شماره سفارش تصادفی نیست.
- اجرای چندباره Seed رکورد تکراری ایجاد نمی‌کند.
- اگر یک محصول Seed شده ساختار ناقص داشته باشد، aggregate آن تعمیر می‌شود.
- داده‌های غیر Seed شده حذف یا reset نمی‌شوند.
- حساب مشتری Identity به رکورد Customer تستی متصل است.
- با فعال بودن `ResetDevelopmentPasswords` رمزهای مستندشده در هر startup توسعه قابل استفاده می‌مانند.

## فایل‌های اصلی

```text
src/Tatakae.Infrastructure/Seeding/DevelopmentSeedCatalog.cs
src/Tatakae.Infrastructure/Seeding/DevelopmentIdentitySeeder.cs
src/Tatakae.Infrastructure/Seeding/StoreDataSeeder.cs
src/Tatakae.Infrastructure/Seeding/SeedDataOptions.cs
src/Tatakae.Infrastructure/Seeding/SeedIds.cs
```

## اجرای محلی

```powershell
dotnet run --launch-profile https --project .\src\Tatakae.Api\Tatakae.Api.csproj
```

برای ساخت دوباره دیتابیس فاز ۱۴ می‌توان دیتابیس LocalDB زیر را حذف کرد و API را مجدداً اجرا کرد:

```text
TatakaeEmbroideryCommerce_Phase14ReliableSeedV1
```
