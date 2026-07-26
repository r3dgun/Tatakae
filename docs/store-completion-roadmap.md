# Store completion roadmap

این پروژه از این نسخه به بعد مرحله‌ای کامل می‌شود تا ساختارها با هم تداخل پیدا نکنند.

## Phase 01 - Catalog / SKU / Inventory
انجام شد در همین نسخه.

## Phase 02 - Order workflow
- وضعیت سفارش‌ها
- عملیات گلدوزی
- بسته‌بندی
- ارسال
- لغو و مرجوعی
- کاهش/بازگشت موجودی بر اساس وضعیت

## Phase 03 - Payment foundation
- Payment
- PaymentTransaction
- وضعیت تراکنش
- حالت پرداخت نمایشی برای توسعه
- آماده‌سازی اتصال زرین‌پال یا درگاه ایرانی

## Phase 04 - Shipping and addresses
- چند آدرس برای هر کاربر
- آدرس پیش‌فرض
- انتخاب روش ارسال در checkout
- کد رهگیری

## Phase 05 - Discounts and campaigns
- کوپن درصدی/مبلغی
- کمپین تخفیف
- محدودیت تعداد استفاده
- حداقل مبلغ سفارش

## Phase 06 - Trust and content
- نظر محصول فقط بعد از خرید
- پرسش و پاسخ قابل تأیید
- علاقه‌مندی‌ها
- محصولات مشابه و اخیراً دیده‌شده

## Phase 07 - Studio production workflow
- بررسی فایل گلدوزی
- وضعیت تأیید طرح
- فایل DST/PES
- قیمت‌گذاری دقیق‌تر بر اساس رنگ، محل و ابعاد

## Phase 08 - SEO and production hardening
- sitemap.xml واقعی
- robots.txt
- redirectها
- لاگ خطا
- تست کامل موبایل و دسکتاپ

## Phase 13 - Route-aware responsive site
انجام شد در این نسخه.

- mobile layout اختصاصی برای Home، Shop، Category، Product، Studio، Cart، Checkout، Login، Account، Admin و Legal.
- route marker مشترک برای اعمال selector صفحه روی `body`.
- bottom-sheet موبایل برای Cart و فیلتر Shop/Category.
- safe-area، touch target، reduced-motion و breakpointهای مستقل.
- پروژه تست `Tatakae.Web.Tests` برای قرارداد route و assetهای responsive.
