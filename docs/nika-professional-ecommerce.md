# Nika Cinematic Professional Ecommerce Integration

این نسخه فروشگاه Tatakae را روی تم ارسالی Nika/Cinematic پیاده‌سازی می‌کند.

## بخش‌های اضافه‌شده

- Layout اصلی با topbar، drawer سبد خرید، لینک ورود/ثبت‌نام و مسیر ادمین.
- فروشگاه `/shop` با جست‌وجو، دسته‌بندی، sort، کارت محصول و pagination.
- صفحه دسته‌بندی `/category/{slug}` با canonical و CollectionPage JSON-LD.
- صفحه محصول `/product/{slug}` با Product/Offer JSON-LD، Open Graph، canonical و محتوای محصول.
- استودیو `/customize/{slug}` با مدل کامل گلدوزی: لباس، سایز، رنگ لباس، فایل/طرح آماده/متن، X/Y، scale، rotation، opacity، رنگ نخ و ابعاد.
- Checkout با DataAnnotations، آدرس، کوپن، سفارش و OrderCard.
- حساب کاربری، ورود و ثبت‌نام نمایشی با API: `/login`, `/register`, `/account`.
- پنل ادمین برای محصول، دسته‌بندی، سفارش، مشتری، کوپن و مرکز SEO.

## نکته تولید

ذخیره‌سازی فعلاً In-Memory است. برای production باید EF Core، دیتابیس، Identity/JWT، آپلود فایل واقعی و SSR/Prerender اضافه شود.
