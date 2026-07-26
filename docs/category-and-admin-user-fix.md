# اصلاح دسته‌بندی و کاربر ادمین

این نسخه دو اصلاح اصلی دارد:

## دسته‌بندی فروشگاه

صفحه دسته‌بندی بازنویسی شده و این مسیرها را پشتیبانی می‌کند:

- `/category/{slug}`
- `/categories/{slug}`
- `/shop/category/{slug}`

نمونه تست:

- `/category/embroidered-tshirts`
- `/category/embroidered-hoodies`
- `/category/embroidered-sweatshirts`
- `/category/embroidered-polos`

صفحه دسته‌بندی اکنون از همان UI تمیز فروشگاه استفاده می‌کند، محصول‌ها را همان ابتدای صفحه نشان می‌دهد و فیلترهای قیمت، سایز، رنگ، موجودی و پیشنهادی را فقط برای همان دسته اعمال می‌کند.

## ادمین Seed شده

دو حساب ادمین Seed می‌شوند:

| موبایل | رمز عبور | نقش |
|---|---|---|
| `09123456789` | `Admin@123456` | SuperAdmin |
| `09120000000` | `Admin@123456` | SuperAdmin |

بعد از ورود به `/login`، اگر کاربر Permission داشبورد ادمین را داشته باشد مستقیم به `/admin` می‌رود.

## دیتابیس

برای جلوگیری از تداخل با دیتابیس‌های قبلی، نام دیتابیس به این مقدار تغییر کرده است:

`TatakaeEmbroideryCommerce_FinalReviewedFixed10CategoryAdminV1`
