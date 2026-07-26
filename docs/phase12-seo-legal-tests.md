# Phase 12 - تست‌های SEO و صفحات قانونی

تست‌های مرحله ۱۲ در پروژه‌های موجود solution اضافه شده‌اند و با دستور زیر اجرا می‌شوند:

```bash
dotnet test Tatakae.sln
```

یا با اسکریپت‌های آماده:

```bash
./scripts/tests/run-tests.sh
```

```powershell
.\scripts\tests\run-tests.ps1
```

## پوشش تست‌ها

### `Tatakae.Application.Tests`

- نرمال‌سازی slug فارسی، انگلیسی و ورودی ترکیبی.
- تبدیل ارقام فارسی و عربی به لاتین.
- یکسان‌سازی نویسه‌های عربی و فارسی.
- حذف نویسه‌های نامعتبر و فشرده‌سازی separatorها.
- idempotent بودن نرمال‌سازی slug.
- نرمال‌سازی canonical شامل URL کامل، query، fragment، slash تکراری و backslash.
- اعتبارسنجی Data Annotation برای slug فارسی صفحات قانونی.
- mapping مسیرهای قانونی استاندارد و صفحات سفارشی.
- نرمال‌سازی `PublicBaseUrl`.
- فیلتر محصولات منتشرنشده و دسته‌بندی‌های غیرفعال یا `noindex` از sitemap.
- `lastmod` واقعی محصول.
- priority و change frequency محصول featured و ناموجود.
- حذف URL تکراری بر اساس canonical.
- audit برای عنوان و توضیح کوتاه/بلند، canonical نامعتبر، تصویر، SKU و `noindex`.
- سیاست `index/noindex` مسیرهای عمومی و خصوصی.

### `Tatakae.Api.Tests`

- خروجی XML معتبر و UTF-8 برای `/sitemap.xml`.
- namespace استاندارد sitemap و مقادیر `loc`، `lastmod`، `changefreq` و `priority`.
- استفاده از `PublicBaseUrl` و جلوگیری از نشت localhost.
- محتوای `/robots.txt` و `Disallow` مسیرهای خصوصی.
- fallback دامنه robots از origin درخواست جاری.
- routeهای عمومی صفحات قانونی و فرم تماس.
- route و permissionهای مدیریت صفحات قانونی.
- دریافت فقط صفحات منتشرشده با ترتیب صحیح.
- aliasهای `rules -> terms` و `shipping-policy -> shipping`.
- عدم نمایش صفحه draft در API عمومی.
- rename کردن slug بدون ساخت رکورد تکراری.
- جلوگیری از slug تکراری.
- fallback و محدودیت ۶۵/۱۶۰ کاراکتری متادیتای SEO.
- ثبت فرم تماس، نرمال‌سازی شماره موبایل و ذخیره IP.
- تغییر وضعیت پیام تماس و ثبت زمان پاسخ.
- خطای پیام تماس ناموجود.

## فایل‌های اصلی تست مرحله ۱۲

```text
tests/Tatakae.Application.Tests/SeoSlugTests.cs
tests/Tatakae.Application.Tests/SeoServiceTests.cs
tests/Tatakae.Api.Tests/SeoControllerRouteTests.cs
tests/Tatakae.Api.Tests/SeoEndpointTests.cs
tests/Tatakae.Api.Tests/LegalContentServiceTests.cs
```

## نکته محیط اجرا

تست‌های دیتابیس از `Microsoft.EntityFrameworkCore.InMemory` استفاده می‌کنند و به SQL Server خارجی وابسته نیستند. تست endpointهای sitemap و robots نیز controller را مستقیم اجرا می‌کنند و به اجرای وب‌سرور نیاز ندارند.
