# Ready-made embroidered products

این نسخه دو نوع محصول دارد:

1. **قابل شخصی‌سازی در استودیو** (`SupportsEmbroidery = true`)
   - دکمه محصول وارد `/customize/{slug}` می‌شود.
   - قیمت گلدوزی با `EmbroideryPricingService` محاسبه می‌شود.

2. **آماده گلدوزی‌شده** (`SupportsEmbroidery = false`)
   - محصول وارد استودیو نمی‌شود.
   - در صفحه محصول دکمه «افزودن محصول آماده به سبد» نشان داده می‌شود.
   - در Checkout هزینه گلدوزی جداگانه صفر است؛ چون طرح از قبل روی کالا اجرا شده است.

در پنل ادمین مسیر `/admin/products/new` یا ویرایش محصول، گزینه «قابل شخصی‌سازی در استودیو» را بردار تا محصول آماده محسوب شود.

محصول‌های آماده seed شده:

- `/product/ready-dragon-mark-embroidered-tshirt`
- `/product/ready-sword-crest-embroidered-hoodie`

نام دیتابیس این نسخه:

`TatakaeEmbroideryCommerce_ReadyMadeProductsV1`
