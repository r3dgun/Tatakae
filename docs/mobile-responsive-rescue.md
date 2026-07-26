# Mobile responsive rescue

این نسخه یک لایه CSS جداگانه به نام `mobile-rescue.css` اضافه می‌کند و آن را بعد از همه CSSها لود می‌کند تا قوانین قبلی را روی موبایل override کند.

موارد اصلاح‌شده:

- منوی موبایل واقعی در `MainLayout.razor`
- تبدیل بنر اصلی از ساختار absolute/parallax به چیدمان خوانا در موبایل
- اصلاح `featured` و `discounted` به کارت‌های یک‌ستونه، بدون offset دسکتاپی
- فعال بودن CTA / quick-view روی touch به‌جای hover
- اصلاح فروشگاه، فیلترها، محصول‌ها، صفحه محصول، Q/A، استودیو، Checkout، Account، Cart Drawer و Footer
- غیرفعال شدن parallax JS روی موبایل و touch devices

فایل‌های تغییرکرده:

- `src/Tatakae.Web/Layout/MainLayout.razor`
- `src/Tatakae.Web/wwwroot/index.html`
- `src/Tatakae.Web/wwwroot/css/mobile-rescue.css`
- `src/Tatakae.Web/wwwroot/js/tatakae-ink.js`

بعد از جایگزینی نسخه، cache موبایل یا مرورگر را پاک کنید.
