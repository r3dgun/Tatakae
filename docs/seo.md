# SEO فروشگاه

## URL Strategy

- محصول: `/product/{slug}`
- دسته‌بندی: `/category/{slug}`
- فروشگاه: `/shop`
- پنل: `/admin/*` و مسدود در `robots.txt`

## Product Page Checklist

- یک H1 معادل نام محصول
- Meta title و description قابل ویرایش از پنل
- Canonical path
- Open Graph image / title / description
- JSON-LD `Product` و `Offer` با SKU، قیمت و availability
- Breadcrumb واقعی
- تصویر با alt text قابل ویرایش
- محتوای معرفی، جنس، فیت، نگهداری و مشخصات
- URL پایدار و بدون ID

## Important Blazor WASM Note

`HeadContent` در Blazor WASM برای کاربر و مرورگر اعمال می‌شود، اما در خزش بدون JavaScript تضمین کامل ندارد. برای فروشگاه production، storefront را با Blazor Web App prerendered یا ASP.NET Core SSR host ارائه دهید و کامپوننت‌ها را به Interactive WebAssembly وصل کنید. Domain/Application/API فعلی بدون تغییر قابل استفاده خواهند بود.
