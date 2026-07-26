# Kimi Theme Integration

در این نسخه، فایل `tatakae_kimi_award_ecommerce_full(1).html` دیگر فقط به‌صورت صفحه مستقل استفاده نشده است. پوسته‌ی آن به پروژه Blazor WebAssembly منتقل شده و صفحات اصلی فروشگاه روی همان طراحی سوار شده‌اند.

## فایل‌های اضافه‌شده

- `src/Tatakae.Web/wwwroot/css/kimi-theme.css`  
  CSS استخراج‌شده از فایل HTML مرجع، بدون تغییر در توکن‌ها، رنگ‌ها و کلاس‌های اصلی.

- `src/Tatakae.Web/wwwroot/css/kimi-integration.css`  
  Adapterهای لازم برای اتصال Blazor به همان کامپوننت‌های بصری Kimi.

- `src/Tatakae.Web/wwwroot/js/kimi-blazor.js`  
  اسکریپت سبک برای reveal animation و horizontal motion در رندر Blazor.

## صفحات سوار شده روی پوسته

- `/` صفحه اصلی سینمایی
- `/shop` فروشگاه و دسته‌بندی‌ها
- `/category/{slug}` صفحه دسته‌بندی
- `/product/{slug}` صفحه محصول SEO-ready
- `/customize/{slug}` استودیوی گلدوزی
- `/checkout` پرداخت
- `/account/orders` پیگیری سفارش‌ها
- سبد خرید Drawer داخل Layout اصلی

## نکته

صفحه مستقل قبلی `/kimi-award` هنوز باقی مانده، اما مسیر اصلی پروژه دیگر از آن استفاده نمی‌کند؛ خود پروژه روی پوسته Kimi سوار شده است.
