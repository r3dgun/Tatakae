# Phase 09 - Admin Dashboard Tests

برای این مرحله تست‌های Application و API اضافه شد.

## تست‌های Application

```text
tests/Tatakae.Application.Tests/AdminDashboardServiceTests.cs
```

پوشش تست:

- محاسبه فروش، موجودی و وضعیت خط تولید سفارش
- ساخت Action Item برای پرداخت معلق، طرح در انتظار بررسی، نظر در انتظار، پرسش بی‌پاسخ و SKU ناموجود
- محاسبه پرفروش‌ترین محصول فقط از سفارش‌های پرداخت‌شده

## تست‌های API

```text
tests/Tatakae.Api.Tests/AdminDashboardRouteTests.cs
```

پوشش تست:

- مسیر `api/admin/dashboard`
- وجود PermissionChecker روی کنترلر داشبورد

## اجرا

```powershell
dotnet test .\Tatakae.sln
```
