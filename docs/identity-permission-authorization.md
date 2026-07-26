# Identity + Role + Permission Authorization

این نسخه برای پنل مدیریت از ASP.NET Core Identity استفاده می‌کند.

## مدل دسترسی

ساختار به این شکل است:

```text
User -> Roles -> RolePermissions -> Permissions -> Admin Pages / API Policies
```

هر صفحه ادمین یک Permission دارد. اگر کاربر لاگین کند ولی Permission همان صفحه را نداشته باشد، هم در Blazor اجازه ورود نمی‌گیرد و هم API مربوط به آن صفحه با `403 Forbidden` جواب می‌دهد.

## جدول‌های اصلی

```text
IdentityUsers
IdentityRoles
IdentityUserRoles
AppPermissions
AppRolePermissions
```

مدل‌های اصلی در مسیر زیر هستند:

```text
src/Tatakae.Infrastructure/Persistence/Models/IdentityDbRecords.cs
```

Permissionهای ثابت در مسیر زیر تعریف شده‌اند:

```text
src/Tatakae.Application/Security/PermissionNames.cs
```

## ادمین پیش‌فرض

در Seed دیتابیس یک کاربر مدیر کل ساخته می‌شود:

```text
Mobile:   09120000000
Password: Admin@123456
Role:     SuperAdmin
```

بعد از ورود با این حساب، مسیر زیر را باز کن:

```text
/admin/security
```

در این صفحه Roleها، Userها و Permissionهای متصل به صفحات ادمین دیده می‌شوند. برای ویرایش Permission یک Role، همان Role را انتخاب کن، Permissionها را تیک بزن و ذخیره کن.

## APIهای امنیت

```text
POST /api/account/register
POST /api/account/login
GET  /api/account/me

GET  /api/admin/security/permissions
GET  /api/admin/security/roles
POST /api/admin/security/roles
PUT  /api/admin/security/roles/{roleId}/permissions
GET  /api/admin/security/users
POST /api/admin/security/users
PUT  /api/admin/security/users/{userId}/roles
```

## محافظت صفحات ادمین

نمونه:

```csharp
@attribute [Authorize(Policy = PermissionNames.AdminProductsView)]
```

و در API:

```csharp
[Authorize(Policy = PermissionNames.AdminProductsView)]
```

برای عملیات ایجاد/ویرایش/حذف، Permission جداگانه استفاده شده است؛ مثلاً:

```csharp
[Authorize(Policy = PermissionNames.AdminProductsManage)]
```

## نکته دیتابیس

چون DbContext تغییر کرده و Identity tableها اضافه شده‌اند، اگر دیتابیس قبلی ساخته شده، یکی از این دو کار را انجام بده:

1. دیتابیس قبلی را حذف کن؛ یا
2. اسم دیتابیس را در `appsettings.Development.json` عوض کن.

مثلاً:

```json
"Database=TatakaeEmbroideryCommerce_IdentityV1;"
```

بعد API را دوباره اجرا کن تا جدول‌ها و Seed جدید ساخته شوند.
