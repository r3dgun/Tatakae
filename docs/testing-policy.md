# Testing Policy

از این نسخه به بعد هر Phase باید همراه با تست تحویل داده شود.

## قانون تحویل هر مرحله

هر تغییر جدید باید حداقل یکی از این تست‌ها را داشته باشد:

- Unit Test برای منطق دامنه یا سرویس‌ها
- Application Test برای Use Caseها و DTO Mapping
- API Test برای Controller، Permission، فیلترها یا endpointهای جدید
- Integration-style Test با EF InMemory برای جریان‌های وابسته به دیتابیس

## ساختار تست‌ها

```text
/tests/Tatakae.Domain.Tests
/tests/Tatakae.Application.Tests
/tests/Tatakae.Api.Tests
```

## تست‌های اضافه‌شده در Phase 04 Location Seed

```text
IranLocationSeedTests.cs
LocationControllerTests.cs
```

پوشش این تست‌ها:

```text
- وجود 31 استان
- وجود شهرهای اصلی Checkout
- نداشتن نام خالی یا تکراری
- داشتن حداقل 300 شهر
- خروجی API استان‌ها
- خروجی API شهرهای هر استان
- خروجی خالی برای province نامعتبر/خالی
```

## اجرای تست‌ها

```powershell
dotnet test .\Tatakae.sln
```

یا:

```powershell
.\scripts\tests\run-tests.ps1
```

## تست‌های Phase 14 Reliable Seed

```text
DevelopmentSeedCatalogTests.cs
StoreDataSeederTests.cs
DevelopmentIdentitySeederTests.cs
```

این تست‌ها پوشش سناریوهای محصول آماده/قابل شخصی‌سازی/تخفیف‌خورده/ناموجود، idempotency، تعمیر aggregate ناقص، سفارش، آدرس، پرسش‌وپاسخ و کاربران Identity را تضمین می‌کنند.
