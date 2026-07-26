# Shop, Legal Pages and Permission Admin Fix

این نسخه سه مشکل اصلی را اصلاح می‌کند:

1. صفحه فروشگاه `/shop` دیگر فقط به وضعیت دیتابیس وابسته نیست. ابتدا از API می‌خواند؛ اگر API یا دیتابیس آماده نبود، یک کاتالوگ نمونه داخلی برای نمایش محصول و تست فیلترها استفاده می‌شود.
2. فیلترهای فروشگاه واقعی شده‌اند: دسته‌بندی، جست‌وجو، سایز، رنگ، قیمت، موجودی، پیشنهاد ویژه و مرتب‌سازی.
3. صفحات اعتماد و قوانین ساخته شدند و از API/ادمین قابل مدیریت‌اند:
   - `/about`
   - `/terms`
   - `/privacy`
   - `/returns`
   - `/shipping-policy`
   - `/contact`

## Admin Legal

صفحه جدید:

```text
/admin/legal
```

با Permissionهای زیر محافظت می‌شود:

```text
admin.legal.view
admin.legal.manage
```

در این صفحه ادمین می‌تواند متن صفحات قوانین، درباره ما، تماس، مرجوعی، حریم خصوصی و ارسال را ویرایش کند. پیام‌های فرم تماس هم در همین بخش ثبت و مدیریت می‌شوند.

## Permission Page Mapping

مدل جدید اضافه شد:

```text
AdminPageAccessDbRecord
```

جدول:

```text
AdminPageAccesses
```

این مدل مشخص می‌کند هر صفحه ادمین با کدام Permission باز شود:

```text
PageKey
Title
Path
RequiredPermissionKey
MenuGroup
Icon
ShowInMenu
IsActive
SortOrder
```

در صفحه زیر مدیریت می‌شود:

```text
/admin/security
```

## Database

برای اینکه جدول‌های جدید بدون تداخل ساخته شوند، نام دیتابیس به این تغییر داده شد:

```text
TatakaeEmbroideryCommerce_PermissionsShopLegalV1
```

اگر دیتابیس قبلی را نگه داری، جدول‌های جدید ساخته نمی‌شوند چون پروژه از `EnsureCreated` استفاده می‌کند. پس یا همین نام جدید را استفاده کن، یا دیتابیس قبلی را حذف کن.
