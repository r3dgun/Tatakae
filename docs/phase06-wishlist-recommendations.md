# Phase 06 - Wishlist, Recently Viewed, Recommendations

این مرحله بعد از تخفیف‌ها اضافه شد تا فروشگاه فقط checkout محور نباشد و تجربه خرید برگشتی هم داشته باشد.

## قابلیت‌ها

- علاقه‌مندی‌های دائمی برای کاربر لاگین‌شده
- حذف و اضافه علاقه‌مندی از صفحه محصول
- صفحه `/account/wishlist`
- محصولات اخیراً دیده‌شده در localStorage مرورگر
- محصولات مشابه در صفحه محصول
- پیشنهادهای شخصی‌سازی‌شده بر اساس علاقه‌مندی‌های کاربر

## APIها

```http
GET    /api/account/wishlist
GET    /api/account/wishlist/{productId}/status
POST   /api/account/wishlist/{productId}/toggle
DELETE /api/account/wishlist/{productId}
GET    /api/account/wishlist/recommendations?take=8
GET    /api/recommendations/similar/{slug}?take=6
```

## تست‌ها

```text
tests/Tatakae.Application.Tests/WishlistServiceTests.cs
tests/Tatakae.Api.Tests/WishlistControllerRouteTests.cs
```

سناریوهای تست:

- Toggle علاقه‌مندی محصول را اضافه می‌کند.
- Toggle دوباره همان محصول را حذف می‌کند.
- پیشنهادها محصول‌های داخل علاقه‌مندی را تکرار نمی‌کنند.
- محصول مشابه، خود محصول فعلی را نشان نمی‌دهد.
- محصول ناموجود امتیاز پیشنهاد منفی می‌گیرد.
- route و authorize بودن کنترلر wishlist بررسی می‌شود.

## دیتابیس

از جدول موجود `Wishlists` استفاده می‌شود. نام دیتابیس این نسخه:

```text
TatakaeEmbroideryCommerce_Phase06WishlistV1
```
