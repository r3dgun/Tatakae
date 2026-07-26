# Phase 24 — AI Search Readiness (AEO / GEO)

این فاز سایت را برای موتورهای پاسخ‌گو، جستجوی مولد و دستیارهای هوش مصنوعی آماده می‌کند؛ بدون تولید محتوای مخفی، تکراری یا مخصوص دستکاری رتبه.

## خروجی‌های عمومی

- `/llms.txt` — راهنمای کوتاه Markdown شامل معرفی سایت، دسته‌ها، محصولات منتخب و صفحات اعتماد.
- `/llms-full.txt` — نسخه تفصیلی شامل مشخصات محصولات و SKUهای عمومی. با `AiSeo:ExposeFullCatalog` قابل خاموش‌کردن است.
- `/ai/catalog.json` — کاتالوگ عمومی JSON شامل دسته‌ها، محصولات، قیمت، موجودی، واریانت‌ها و قوانین منتشرشده.
- `/sitemap.xml` — منبع canonical URLها.
- `/robots.txt` — کنترل جداگانه OAI-SearchBot، ChatGPT-User و GPTBot.

تمام feedها فقط داده عمومی، منتشرشده و `AllowIndex=true` را ارائه می‌کنند. مسیرهای حساب، سفارش، پرداخت، سبد، Checkout و مدیریت هیچ‌وقت وارد خروجی AI نمی‌شوند.

## تنظیمات

```json
"AiSeo": {
  "SiteName": "Tatakae",
  "OrganizationName": "Tatakae",
  "Summary": "فروشگاه ایرانی پوشاک گلدوزی آماده و قابل شخصی‌سازی.",
  "Language": "fa-IR",
  "Currency": "IRR",
  "AreaServed": "Iran",
  "SupportEmail": "",
  "SupportPhone": "",
  "MaxProductsInLlms": 100,
  "ExposeFullCatalog": true,
  "AllowOpenAiSearch": true,
  "AllowOpenAiUserFetch": true,
  "AllowOpenAiTraining": false
}
```

- `AllowOpenAiSearch`: اجازه خزش محتوای عمومی به `OAI-SearchBot` برای جستجو و citation.
- `AllowOpenAiUserFetch`: اجازه fetch درخواستی توسط `ChatGPT-User`.
- `AllowOpenAiTraining`: کنترل مستقل `GPTBot`. مقدار پیش‌فرض `false` است.

## Structured data

- صفحه اصلی: `Organization` + `WebSite`
- فروشگاه و دسته‌بندی: `CollectionPage` + `ItemList` + `BreadcrumbList`
- محصول: `ProductGroup` و `Product` برای SKUها، `Offer`، موجودی، قیمت، `AggregateRating` و Reviewهای واقعی
- پرسش‌های پاسخ‌داده‌شده و قابل مشاهده: `FAQPage`
- صفحات قانونی: `AboutPage`، `ContactPage` یا `WebPage`

Structured data فقط از داده‌ای ساخته می‌شود که روی همان صفحه برای کاربر قابل مشاهده است. امتیاز، نظر، موجودی، قیمت یا پاسخ ساختگی تولید نمی‌شود.

## محتوای قابل استخراج

صفحه محصول یک بخش «خلاصه سریع محصول» دارد که موارد زیر را به شکل semantic `dl` نمایش می‌دهد:

- دسته محصول
- آماده یا قابل شخصی‌سازی بودن
- وضعیت موجودی
- قیمت شروع
- جنس
- فیت

این بخش هم برای کاربر مفید است و هم استخراج facts را برای موتورهای پاسخ ساده‌تر می‌کند.

## نکات انتشار

1. `PublicBaseUrl` باید دامنه واقعی HTTPS باشد.
2. در استقرار جداگانه Web/API، reverse proxy باید مسیرهای `/robots.txt`، `/sitemap.xml`، `/llms.txt`، `/llms-full.txt` و `/ai/catalog.json` را روی دامنه عمومی سایت به API هدایت کند.
3. اطلاعات تماس واقعی را در `AiSeo` و صفحات قانونی وارد کنید.
4. داده structured را با Rich Results Test و Schema.org Validator بررسی کنید.
5. فایل‌های AI با `X-Robots-Tag: noindex, follow` ارائه می‌شوند تا crawl شوند ولی نتیجه مستقل جستجو نشوند.
6. llms.txt یک convention در حال شکل‌گیری است و جایگزین SEO، sitemap یا robots.txt نیست.
7. برای بهترین crawl صفحه‌های محصول در همه سامانه‌ها، SSR یا prerender صفحات عمومی همچنان پیشنهاد می‌شود.
