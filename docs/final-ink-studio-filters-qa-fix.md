# Final Ink Editorial studio / filters / Q&A fix

این نسخه روی پوسته `tatakae-ink-editorial-stylekit` پیاده شده و هدف آن تغییر سبک اصلی نیست؛ فقط بخش‌های ecommerce، استودیو، فیلتر و پرسش‌وپاسخ با همان زبان طراحی مرتب شده‌اند.

## تغییرات

- بازچینی کامل `/shop` با فیلتر جدا از محصول‌ها
- اصلاح `/category/{slug}` با همان ساختار فروشگاه
- بازنویسی کارت محصول با کلاس‌های مستقل `ink-product-card`
- بازچینی کامل `/customize/{slug}` به‌صورت سه ستون: تنظیمات، پیش‌نمایش، خلاصه/کنترل
- افزودن بخش پرسش و پاسخ در صفحه محصول
- افزودن endpointهای پایه Q&A:
  - `GET /api/products/{productId}/questions`
  - `POST /api/products/{productId}/questions`
- افزودن صفحه ادمین `/admin/questions`
- افزودن Permissionهای:
  - `admin.questions.view`
  - `admin.questions.manage`
- افزودن فونت تیتر شبیه B Nazanin با stack زیر:
  - `B Nazanin`, `BNazanin`, `Noto Naskh Arabic`, `Vazirmatn`, serif

## نکته فونت

فایل فونت B Nazanin داخل پروژه قرار داده نشده چون فونت اختصاصی/سیستمی است. اگر روی سیستم کاربر نصب باشد استفاده می‌شود؛ در غیر این صورت `Noto Naskh Arabic` به عنوان جایگزین نزدیک استفاده می‌شود.
