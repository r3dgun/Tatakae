# Tatakae Final Structure Review

این نسخه پس از بازبینی ساختارهای اصلی پروژه آماده شده است.

## لایه‌ها

- `Tatakae.Domain`: موجودیت‌ها و enumهای اصلی دامنه فروشگاه و گلدوزی.
- `Tatakae.Application`: DTOها، قراردادها، سرویس‌های کاربردی، Interfaceهای Repository و Permission catalog.
- `Tatakae.Infrastructure`: مدل‌های Code First، DbContext، Identity، Repositoryهای SQL Server، Seed و BaseEntity.
- `Tatakae.Api`: کنترلرهای عمومی و ادمین، Identity/JWT، PermissionCheckerAttribute، سرویس‌های امنیت، فایل و قوانین.
- `Tatakae.Web`: Blazor WebAssembly، فروشگاه، فیلتر، Checkout، استودیو، ادمین، صفحات قانونی و مدیریت Permission.

## BaseEntity

همه مدل‌های دیتابیسی غیر از کلاس‌های اصلی Identity از `BaseEntity<TKey>` ارث‌بری می‌کنند و فیلدهای زیر را دارند:

- `Id`
- `InsertTime`
- `UpdateTime`
- `IsRemoved`
- `RemoveTime`

در `TatakaeDbContext` ثبت و ویرایش رکوردها audit می‌شود و `InsertTime`/`UpdateTime` به‌صورت خودکار مقدار می‌گیرد.

## فروشگاه و فیلتر

مسیرهای اصلی:

- `/shop`
- `/products`
- `/category/{slug}`
- `/product/{slug}`
- `/customize/{slug}`
- `/checkout`

فیلترهای فروشگاه:

- جست‌وجو
- دسته‌بندی
- سایز
- رنگ
- قیمت حداقل/حداکثر
- فقط موجودها
- فقط پیشنهادی‌ها
- مرتب‌سازی

اگر API یا SQL Server آماده نباشد، Web از `StoreFallbackCatalog` استفاده می‌کند تا فروشگاه و فیلترها خالی نمایش داده نشوند.

## صفحات اعتماد و قوانین

مسیرها:

- `/about`
- `/terms`
- `/privacy`
- `/returns`
- `/shipping-policy`
- `/contact`

مدیریت ادمین:

- `/admin/legal`

صفحات از API خوانده می‌شوند و اگر API در دسترس نباشد، fallback داخلی نمایش داده می‌شود.

## احراز هویت و دسترسی

ساختار امنیت:

```text
User -> UserRole -> Role -> PermissionsRole -> Permission
```

Identity هم فعال است:

- `ApplicationUserIdentity`
- `ApplicationRoleIdentity`
- `AppPermissionDbRecord`
- `AppRolePermissionDbRecord`
- `AdminPageAccessDbRecord`

PermissionCheckerAttribute برای API:

```csharp
[PermissionChecker(PermissionIds.AdminProductsView)]
```

Permission برای صفحات Blazor:

```csharp
@attribute [Authorize(Policy = PermissionNames.AdminProductsView)]
```

ادمین پیش‌فرض:

```text
Mobile: 09120000000
Password: Admin@123456
Role: SuperAdmin
```

## ادمین

مسیرهای ادمین:

- `/admin`
- `/admin/products`
- `/admin/categories`
- `/admin/orders`
- `/admin/customers`
- `/admin/coupons`
- `/admin/shipping`
- `/admin/media`
- `/admin/seo`
- `/admin/legal`
- `/admin/security`

در `/admin/security` این موارد قابل مدیریت است:

- لیست Permissionها و Permission ID عددی
- Roleها
- Permissionهای هر Role
- Userها
- Roleهای هر User
- نگاشت صفحه ادمین به Permission

## دیتابیس

نام دیتابیس نسخه نهایی:

```text
TatakaeEmbroideryCommerce_FinalReviewedV1
```

Connection string در این فایل‌ها هماهنگ شده است:

- `src/Tatakae.Api/appsettings.json`
- `src/Tatakae.Api/appsettings.Development.json`
- `src/Tatakae.Infrastructure/Persistence/TatakaeDbContextFactory.cs`

## نکته اجرا

برای اجرا باید SQL Server LocalDB یا SQL Server Express نصب باشد. اگر LocalDB نصب نیست، connection string را به SQL Server خودت تغییر بده.

