# Permission Identity Insert Fix

این نسخه خطای SQL Server زیر را اصلاح می‌کند:

```text
Cannot insert explicit value for identity column in table 'PermissionDefinitions' when IDENTITY_INSERT is set to OFF.
```

علت این بود که PermissionId باید مقدار ثابت و عددی داشته باشد، چون در Attribute به شکل زیر استفاده می‌شود:

```csharp
[PermissionChecker(PermissionIds.AdminProductsView)]
```

بنابراین در `TatakaeDbContext` این تنظیم اعمال شد:

```csharp
modelBuilder.Entity<Permission>(entity =>
{
    entity.HasKey(x => x.PermissionId);
    entity.Ignore(x => x.Id);
    entity.Property(x => x.PermissionId).ValueGeneratedNever();
});
```

همچنین برای مدل‌های legacy-style زیر، `BaseEntity.Id` در EF Core ignore شد تا ستون اضافه و بلااستفاده ایجاد نشود:

- User
- Role
- Permission
- UserRole
- PermissionsRole

نام دیتابیس این نسخه:

```text
TatakaeEmbroideryCommerce_FinalReviewedFixed5V1
```
