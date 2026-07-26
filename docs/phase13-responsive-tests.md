# Phase 13 - تست‌های Responsive

پروژه تست جدید:

```text
tests/Tatakae.Web.Tests
```

اجرا:

```bash
dotnet test tests/Tatakae.Web.Tests/Tatakae.Web.Tests.csproj
```

یا کل solution:

```bash
dotnet test Tatakae.sln
```

## تست‌های خودکار

- mapping مسیرهای Home، Shop، Category، Product، Studio، Checkout، Login، Account، Admin و Legal.
- عدم تداخل aliasهای `/products` با `/products/{slug}`.
- تشخیص route همراه query string و fragment.
- وجود profile تمام خانواده صفحه‌های فاز ۱۳.
- وجود selector اختصاصی CSS برای تمام صفحه‌ها و Cart.
- وجود breakpointهای 1180، 900، 600 و 420.
- وجود touch target 44px، safe-area، focus-visible و reduced-motion.
- بارگذاری CSS فاز ۱۳ بعد از `mobile-rescue.css`.
- نصب route marker در MainLayout و AdminLayout.
- وجود bottom-sheet controls در Shop و Category.
- ثبت `data-page` و `data-route` توسط JavaScript.

## چک‌لیست دستی مرورگر

هر صفحه در عرض‌های زیر بررسی شود:

```text
320 × 568
360 × 800
390 × 844
430 × 932
768 × 1024
1024 × 768
1440 × 900
```

برای هر اندازه:

1. overflow افقی وجود نداشته باشد.
2. header، filter sheet، cart sheet و admin rail قابل scroll و قابل بستن باشند.
3. input هنگام focus از viewport خارج نشود.
4. CTA اصلی حداقل 44px و بدون overlap با safe-area باشد.
5. متن فارسی و متن لاتین شکسته یا بریده نشود.
6. تصاویر نسبت صحیح و `object-fit` مناسب داشته باشند.
7. صفحه با `prefers-reduced-motion` بدون parallax و animation سنگین قابل استفاده باشد.
