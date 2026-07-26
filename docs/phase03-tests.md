# Phase 03 Tests

این نسخه برای بخش‌های مهم فروشگاه تست دارد:

## Test projects

- `tests/Tatakae.Domain.Tests`
  - قوانین SKU و موجودی
  - قوانین Order و وضعیت پرداخت/سفارش

- `tests/Tatakae.Application.Tests`
  - سرویس Inventory و محاسبه موجودی قابل فروش/کم‌موجودی

- `tests/Tatakae.Api.Tests`
  - سرویس Payment با دیتابیس InMemory
  - ایجاد پرداخت Pending
  - جلوگیری از دسترسی مشتری اشتباه به سفارش
  - تأیید پرداخت دمو
  - تأیید دستی کارت‌به‌کارت توسط ادمین
  - ناموفق کردن پرداخت

## Commands

```powershell
dotnet test .\Tatakae.sln
```

یا:

```powershell
.\scripts\tests\run-tests.ps1
```

در لینوکس/macOS:

```bash
./scripts/tests/run-tests.sh
```
