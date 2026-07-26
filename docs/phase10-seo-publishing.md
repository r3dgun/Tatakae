# Phase 10 - SEO & Publishing Readiness

این مرحله فروشگاه را برای انتشار عمومی، گوگل و اشتراک‌گذاری شبکه‌های اجتماعی آماده‌تر می‌کند.

## اضافه‌شده‌ها

- `SeoService` برای تولید Sitemap، قوانین robots و audit سئو.
- `/sitemap.xml` داینامیک با `lastmod`، `changefreq` و `priority`.
- `/robots.txt` داینامیک در API؛ نسخه static حذف شده تا دامنه یا محتوای قدیمی منتشر نشود.
- `/api/admin/seo/audit` برای بررسی عنوان، توضیح، canonical، تصویر OG و وضعیت ایندکس.
- `/api/admin/seo/routes` برای نمایش سیاست ایندکس مسیرها.
- صفحه `/admin/seo` با امتیاز کلی، خطاها، هشدارها، URLهای مهم و قوانین noindex.
- بهبود `SeoHead` با `og:locale`، `og:site_name`، تصویر Twitter و canonical absolute.

## مسیرهای عمومی قابل ایندکس

- `/`
- `/shop`
- `/category/{slug}`
- `/product/{slug}`
- صفحات اعتماد و قانونی مثل `/about`، `/rules`، `/privacy`، `/returns`، `/shipping-policy` و `/contact`

## مسیرهای noindex

- `/admin/*`
- `/account/*`
- `/checkout`
- `/payment/*`
- `/login`
- `/register`

## تنظیم Production

در production مقدار زیر را در `appsettings.Production.json` یا environment بگذار:

```json
{
  "PublicBaseUrl": "https://your-domain.com"
}
```

اگر این مقدار تنظیم نشود، URLها با host درخواست ساخته می‌شوند.

## تست

تست‌های این مرحله:

- `SeoServiceTests`
- `SeoControllerRouteTests`

اجرای تست:

```powershell
dotnet test .\Tatakae.sln
```


> تکمیل نسخه production و slug فارسی/انگلیسی در `docs/phase12-seo-legal.md` مستند شده است.
