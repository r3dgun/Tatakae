# Phase 04 - Address Book & Shipping Flow

این فاز دفترچه آدرس مشتری و اتصال آن به Checkout را کامل‌تر می‌کند.

## امکانات اضافه‌شده

- صفحه جدید `/account/addresses` برای مدیریت آدرس‌ها.
- APIهای امن حساب کاربری برای آدرس‌ها:
  - `GET /api/account/addresses`
  - `POST /api/account/addresses`
  - `PUT /api/account/addresses/{id}`
  - `DELETE /api/account/addresses/{id}`
- انتخاب آدرس ذخیره‌شده در `/checkout`.
- انتخاب خودکار آدرس پیش‌فرض در Checkout.
- ذخیره یا به‌روزرسانی آدرس بعد از ثبت سفارش.
- حفظ آدرس پیش‌فرض هنگام حذف/ویرایش.
- تست‌های Application برای آدرس‌های حساب کاربری.

## دلیل تغییر

قبل از این فاز، مشتری در هر Checkout باید آدرس را دوباره وارد می‌کرد. حالا فروشگاه برای خریدهای تکراری آماده‌تر است و Checkout سریع‌تر انجام می‌شود.

## دیتابیس این نسخه

`TatakaeEmbroideryCommerce_Phase04AddressShippingV1`
