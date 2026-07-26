# Phase 11 Notification Tests

تست‌های اضافه‌شده:

```text
tests/Tatakae.Application.Tests/NotificationServiceTests.cs
tests/Tatakae.Api.Tests/NotificationControllerRouteTests.cs
```

پوشش تست‌ها:

- ایجاد اعلان دستی معتبر
- رد اعلان مشتری بدون گیرنده
- ساخت اعلان تغییر وضعیت سفارش
- تشخیص اعلان کد رهگیری به عنوان ShipmentTrackingAdded
- ساخت اعلان پرداخت موفق
- خواندن اعلان توسط مشتری
- صفر شدن شمارنده خوانده‌نشده بعد از خواندن
- route و authorization API مشتری
- route و permission API ادمین
- ثبت permissionهای admin.notifications.view/manage در PermissionCatalog

اجرای تست:

```powershell
dotnet test .\Tatakae.sln
```
