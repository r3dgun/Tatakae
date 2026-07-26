# Phase 11 - Notifications Center

در این مرحله سیستم اعلان فروشگاه اضافه شد تا مشتری و ادمین تغییرات مهم را از داخل سایت دنبال کنند.

## قابلیت‌ها

- اعلان داخل حساب کاربری مشتری
- صف دمو برای پیامک، ایمیل و اعلان ادمین
- اعلان ثبت سفارش بعد از Checkout
- اعلان موفق/ناموفق بودن پرداخت
- اعلان تغییر وضعیت سفارش
- اعلان ثبت کد رهگیری ارسال
- خواندن تکی اعلان و خواندن همه اعلان‌ها
- مرکز اعلان‌ها در پنل ادمین
- ارسال اعلان دستی توسط ادمین
- تغییر وضعیت اعلان به Sent / Failed / Cancelled

## مسیرهای مشتری

```text
/account/notifications
```

## مسیرهای ادمین

```text
/admin/notifications
```

## API مشتری

```text
GET   /api/account/notifications
GET   /api/account/notifications/unread-count
PATCH /api/account/notifications/{id}/read
PATCH /api/account/notifications/read-all
```

## API ادمین

```text
GET   /api/admin/notifications
POST  /api/admin/notifications
PATCH /api/admin/notifications/{id}/status
```

## مدل دیتابیس

جدول جدید:

```text
Notifications
```

فیلدهای اصلی:

```text
CustomerId
Channel: InApp / Sms / Email / Admin
Type: OrderCreated / PaymentSucceeded / PaymentFailed / OrderStatusChanged / ShipmentTrackingAdded / Manual
Status: Queued / Sent / Failed / Cancelled
Recipient
Subject
Body
RelatedOrderId
RelatedOrderNumber
RelatedProductId
ActionUrl
IsRead
CreatedAt
SentAt
ReadAt
FailureReason
```

## نکته اجرایی

در این نسخه ارسال SMS/Email واقعی انجام نمی‌شود؛ پیام‌ها در صف و لاگ دیتابیس ذخیره می‌شوند. برای اتصال واقعی بعداً می‌توانیم provider اضافه کنیم:

```text
INotificationSender
SmsIrNotificationSender
EmailSmtpNotificationSender
```

این مرحله عمداً provider واقعی ندارد تا قبل از انتخاب سرویس پیامک/ایمیل، پروژه به سرویس خارجی وابسته نشود.
