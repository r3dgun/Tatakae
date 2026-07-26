# BaseEntity روی مدل‌های دیتابیس

در این نسخه برای مدل‌های Persistence یک کلاس پایه مشترک اضافه شده است:

```csharp
public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; }
    public DateTime InsertTime { get; set; } = DateTime.Now;
    public DateTime? UpdateTime { get; set; }
    public bool IsRemoved { get; set; } = false;
    public DateTime? RemoveTime { get; set; }
}

public abstract class BaseEntity : BaseEntity<long>
{
}
```

مسیر فایل:

```text
src/Tatakae.Infrastructure/Persistence/Models/BaseEntity.cs
```

مدل‌هایی که کلید `Guid Id` داشتند، حالا از `BaseEntity<Guid>` ارث‌بری می‌کنند و `Id` از کلاس پایه می‌آید.

مدل‌های PermissionChecker که قبلاً کلیدهای اختصاصی مثل `UserId`, `RoleId`, `PermissionId`, `UR_Id`, `RP_Id` داشتند نیز از `BaseEntity<long>` ارث‌بری می‌کنند، اما کلیدهای اختصاصی‌شان حفظ شده تا کدهای قبلی و روابط موجود نشکند.

> نکته: کلاس‌های Identity مثل `ApplicationUserIdentity` و `ApplicationRoleIdentity` به خاطر ارث‌بری از `IdentityUser<Guid>` و `IdentityRole<Guid>` نمی‌توانند مستقیم از `BaseEntity` ارث‌بری کنند؛ این‌ها فیلدهای audit خودشان را جداگانه نگه می‌دارند.
