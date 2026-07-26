# PermissionCheckerAttribute

این نسخه علاوه بر Authorization policy مبتنی بر Claim، مدل عددی PermissionChecker را هم اضافه کرده است.

ساختار دسترسی:

```text
User -> UserRole -> Role -> PermissionsRole -> Permission
```

مدل‌ها:

```text
PermissionUsers
PermissionRoles
PermissionDefinitions
UserRoles
PermissionsRoles
```

نمونه استفاده در API:

```csharp
[PermissionChecker(PermissionIds.AdminProductsView)]
public sealed class AdminProductsController : ControllerBase
{
}

[PermissionChecker(PermissionIds.AdminProductsManage)]
[HttpPost]
public async Task<IActionResult> Create(...)
{
}
```

Attribute از `ClaimTypes.Name` کاربر را می‌خواند. در این پروژه `ClaimTypes.Name` برابر موبایل/شناسه ورود کاربر قرار داده شده است. سپس `IPermissionService.CheckPermissionByInsuranceNumberAsync` اجرا می‌شود.

Permission IDهای اصلی:

```text
1000  admin.dashboard.view
1100  admin.products.view
1101  admin.products.manage
1200  admin.categories.view
1201  admin.categories.manage
1300  admin.orders.view
1301  admin.orders.manage
1400  admin.customers.view
1500  admin.coupons.view
1501  admin.coupons.manage
1600  admin.shipping.view
1601  admin.shipping.manage
1700  admin.media.view
1701  admin.media.manage
1800  admin.seo.view
1801  admin.seo.manage
1850  admin.legal.view
1851  admin.legal.manage
1900  admin.security.view
1901  admin.security.manage
```

در `/admin/security` برای هر Permission عدد Permission ID هم نمایش داده می‌شود تا بتوانی دقیقاً همان عدد را داخل `[PermissionChecker(...)]` استفاده کنی.
