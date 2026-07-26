# Phase 13 - ریسپانسیو واقعی کل سایت

در این فاز، ریسپانسیو از حالت مجموعه‌ای از overrideهای عمومی خارج شده و بر اساس route صفحه اعمال می‌شود.

## معماری

کامپوننت زیر در هر دو Layout عمومی و مدیریت قرار دارد:

```text
src/Tatakae.Web/Shared/ResponsiveRouteMarker.razor
```

این کامپوننت با هر تغییر route، نوع صفحه را از `ResponsivePageCatalog` دریافت می‌کند و از طریق `phase13-responsive.js` روی `body` قرار می‌دهد:

```html
<body data-page="product" data-route="product/premium-cotton">
```

فایل CSS مرحله ۱۳ بعد از `mobile-rescue.css` بارگذاری می‌شود تا قراردادهای جدید بر وصله‌های قدیمی اولویت داشته باشند:

```text
src/Tatakae.Web/wwwroot/css/phase13-responsive.css
```

## Layoutهای اختصاصی

### Home

- Hero موبایل با ترکیب مستقل تصویر اصلی، تصویر فرعی و copy.
- حذف parallax و hover سنگین روی touch device.
- گرید دو ستونه تبلت و تک‌ستونه موبایل برای محصولات منتخب و تخفیف‌خورده.
- اندازه تایپوگرافی کنترل‌شده برای عرض‌های ۳۲۰ تا ۹۰۰ پیکسل.

### Shop و Category

- search bar چسبان و سازگار با header موبایل.
- category strip افقی با scroll snap.
- فیلترها به bottom-sheet واقعی تبدیل شده‌اند.
- overlay، دکمه بستن، `aria-expanded` و شمارنده فیلتر فعال اضافه شده است.
- گرید دو ستونه روی تبلت/موبایل بزرگ و تک‌ستونه زیر ۶۰۰ پیکسل.

### Product

- تصویر محصول قبل از اطلاعات خرید قرار می‌گیرد.
- واریانت‌ها و CTAها touch-friendly و تمام‌عرض هستند.
- مشخصات، پیشنهادها، Review و Q/A چیدمان مستقل موبایل دارند.
- فرم Review/Q&A قبل از لیست و بدون sticky دسکتاپ نمایش داده می‌شود.

### Studio

- Preview همیشه اولین بخش صفحه موبایل است.
- کنترل‌ها و خلاصه قیمت به کارت‌های جدا تبدیل می‌شوند.
- chipها، swatchها، rangeها و دکمه افزودن به سبد حداقل target مناسب لمس دارند.
- CTA نهایی با safe-area پایین سازگار است.

### Cart

- drawer دسکتاپ روی موبایل به bottom-sheet تبدیل شده است.
- ارتفاع با `dvh` و safe-area کنترل می‌شود.
- header، لیست scrollable و footer ثابت سه بخش مستقل هستند.

### Checkout

- خلاصه سفارش قبل از فرم نمایش داده می‌شود.
- فرم آدرس، ارسال و پرداخت کاملاً تک‌ستونه است.
- اندازه inputها برای جلوگیری از zoom ناخواسته iOS حداقل ۱۶px است.
- CTA ثبت سفارش با safe-area پایین سازگار است.

### Login و Account

- فرم ورود/ثبت‌نام keyboard-safe و تمام‌عرض است.
- actionهای حساب در rail افقی قرار می‌گیرند.
- داشبورد حساب، آمار، سفارش‌ها و cart sync تک‌ستونه می‌شوند.

### Admin

- sidebar به rail افقی sticky تبدیل می‌شود.
- همه module linkها scroll افقی و touch target مناسب دارند.
- metricها، editorها، variantها و order cards layout موبایل مستقل دارند.
- جدول‌های بزرگ داخل container افقی کنترل‌شده باقی می‌مانند.

### Legal

- navigation صفحات اعتماد sticky و افقی است.
- عرض متن، فاصله خطوط، تیترها و فرم تماس برای خوانایی موبایل تنظیم شده است.

## Breakpointها

```text
1180px  tablet / narrow desktop
900px   mobile layout boundary
600px   single-column commerce cards
420px   compact phone layout
```

## دسترس‌پذیری و موبایل

- حداقل touch target برابر ۴۴px.
- استفاده از `env(safe-area-inset-*)`.
- focus-visible واضح.
- پشتیبانی از `prefers-reduced-motion`.
- غیرفعال شدن parallax و hover composition روی coarse pointer.
- استفاده از `dvh` و fallback متغیر viewport برای مرورگرهای موبایل.
