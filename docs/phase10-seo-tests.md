# Phase 10 SEO Tests

تست‌های اضافه‌شده بررسی می‌کنند:

- Sitemap شامل صفحه اصلی، فروشگاه، دسته‌بندی و محصول منتشرشده باشد.
- محصول noindex وارد Sitemap نشود.
- URLهای Sitemap با base url تمیز ساخته شوند.
- Audit برای عنوان خیلی کوتاه، توضیح خیلی کوتاه، canonical خالی و تصویر خالی هشدار بدهد.
- مسیرهای `api/admin/seo/audit` و `api/admin/seo/routes` وجود داشته باشند.
- `AdminSeoController` permission مربوط به SEO داشته باشد.
- `SitemapController` مسیر `/sitemap.xml` داشته باشد.
- `RobotsController` مسیر `/robots.txt` داشته باشد.
