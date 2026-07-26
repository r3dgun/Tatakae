# Phase 12 - SEO و صفحات قانونی

این مرحله SEO فروشگاه و صفحات اعتماد را از حالت نمایشی به جریان قابل مدیریت متصل می‌کند.

## قابلیت‌های پیاده‌سازی‌شده

### SEO محصول و دسته‌بندی

- `MetaTitle` و `MetaDescription` مستقل برای هر محصول و دسته‌بندی.
- `CanonicalPath` قابل ویرایش با مقدار پیش‌فرض خودکار:
  - `/product/{slug}`
  - `/category/{slug}`
- کنترل `index/noindex` و `follow/nofollow` از پنل مدیریت.
- Open Graph و Twitter Card از طریق `SeoHead` مشترک.
- JSON-LD محصول، Offer، AggregateRating، Breadcrumb و CollectionPage.
- صفحه‌های محصول و دسته‌بندی ناموجود با `noindex,nofollow` خروجی می‌شوند.

### Slug فارسی و انگلیسی

`SeoSlug` ورودی را برای URL عمومی نرمال می‌کند:

- پشتیبانی از حروف فارسی و انگلیسی.
- تبدیل فاصله، underscore و نیم‌فاصله به `-`.
- تبدیل ارقام فارسی و عربی به ارقام لاتین.
- یکسان‌سازی `ي/ی` و `ك/ک`.
- حذف نویسه‌های نامعتبر و جلوگیری از slug تکراری.

نمونه‌ها:

```text
تی شرت گلدوزی  ->  تی-شرت-گلدوزی
Hoodie Oversize -> hoodie-oversize
مدل_۱۴۰۵        -> مدل-1405
```

### Sitemap واقعی

خروجی `GET /sitemap.xml` از دیتابیس ساخته می‌شود و شامل موارد زیر است:

- خانه و فروشگاه.
- دسته‌بندی‌های فعال و قابل ایندکس.
- محصولات منتشرشده و قابل ایندکس.
- فقط صفحات قانونی منتشرشده.
- `lastmod` واقعی برای محصول و صفحه قانونی.
- `changefreq` و `priority` متناسب با نوع صفحه.

فایل‌های static قدیمی Web حذف شده‌اند تا sitemap قدیمی یا دارای دامنه localhost منتشر نشود.

### Robots واقعی

خروجی `GET /robots.txt` از سیاست مسیرهای `SeoService` ساخته می‌شود. مسیرهای زیر مسدود هستند:

- `/admin`
- `/account`
- `/checkout`
- `/cart`
- `/customize`
- `/payment`
- `/order-success`
- `/login`
- `/register`
- `/kimi-award`

### صفحات قانونی و اعتماد

صفحات زیر از دیتابیس دریافت و از `/admin/legal` ویرایش می‌شوند:

- `/about`
- `/rules`؛ رکورد داخلی `terms`
- `/privacy`
- `/returns`
- `/shipping-policy`؛ رکورد داخلی `shipping`
- `/contact`
- صفحات سفارشی: `/pages/{slug}`

برای صفحات قانونی نیز SEO title، SEO description، canonical، JSON-LD و breadcrumb تولید می‌شود. Slug صفحه قانونی می‌تواند فارسی یا انگلیسی باشد و تغییر slug رکورد قبلی را rename می‌کند، نه اینکه یک رکورد تکراری بسازد.

## تنظیم دامنه Production

متغیر `PublicBaseUrl` باید روی دامنه عمومی سایت تنظیم شود:

```text
PublicBaseUrl=https://shop.example.com
```

در development مقدار آن روی `https://localhost:7076` قرار گرفته است. در production باید reverse proxy مسیرهای زیر را روی همان دامنه عمومی به API ارسال کند:

```text
/sitemap.xml
/robots.txt
/api/*
```

به این ترتیب canonicalها و URLهای sitemap همگی روی دامنه فروشگاه باقی می‌مانند.

## تست‌های اضافه‌شده

پوشش خودکار مرحله ۱۲ شامل slug و canonical، فیلتر و audit سئو، XML واقعی sitemap، robots، route و permissionها، CRUD صفحات قانونی، rename و جلوگیری از slug تکراری، انتشار draft/published و جریان پیام تماس است.

جزئیات کامل سناریوها در `docs/phase12-seo-legal-tests.md` ثبت شده است.

اجرای تست‌ها:

```bash
dotnet test Tatakae.sln
```
