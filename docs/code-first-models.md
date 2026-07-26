# Code First Persistence Models

این نسخه دیتابیس را بر اساس مدل‌های Code First داخل لایه Infrastructure می‌سازد.

مسیر مدل‌ها:

```text
src/Tatakae.Infrastructure/Persistence/Models
```

مدل‌ها با DataAnnotation کامل شده‌اند:

- `[Table]` برای نام جدول
- `[Key]` برای کلید اصلی
- `[Required]` برای فیلدهای اجباری
- `[MaxLength]` برای طول ستون‌ها
- `[Precision]` برای قیمت‌ها و اعداد مالی
- `[Index]` برای Slug، SKU، شماره سفارش و فیلدهای پرجست‌وجو
- `[ForeignKey]` و `[InverseProperty]` برای رابطه‌ها
- `[EmailAddress]`, `[Phone]`, `[Url]`, `[Range]` برای اعتبارسنجی داده

`TatakaeDbContext` فقط حداقل Fluent API را نگه داشته است؛ چون بعضی قوانین با DataAnnotation به‌تنهایی قابل تعریف دقیق نیستند:

- ذخیره enumها به‌صورت string در SQL Server
- DeleteBehavior رابطه‌ها
- رابطه one-to-one محصول و سیاست گلدوزی

## جدول‌های اصلی

```text
Categories
Products
ProductImages
ProductVariants
ProductSpecifications
ProductTags
ProductEmbroideryPolicies
ProductAllowedPlacements
ProductAllowedThreadColors
Customers
CustomerAddresses
Orders
OrderLines
Coupons
```

## روش اجرای Code First

در حالت توسعه، API هنگام شروع با `DatabaseInitializer` دیتابیس را ایجاد و Seed اولیه را وارد می‌کند.

برای روش migration-based در محیط واقعی:

```powershell
dotnet tool install --global dotnet-ef
cd src/Tatakae.Api
dotnet ef migrations add InitialCreate --project ..\Tatakae.Infrastructure\Tatakae.Infrastructure.csproj --startup-project .\Tatakae.Api.csproj --context TatakaeDbContext
dotnet ef database update --project ..\Tatakae.Infrastructure\Tatakae.Infrastructure.csproj --startup-project .\Tatakae.Api.csproj --context TatakaeDbContext
```

## نکته معماری

مدل‌های Domain همچنان تمیز و مستقل از EF Core باقی مانده‌اند. مدل‌های Code First مخصوص دیتابیس در Infrastructure هستند تا Clean Architecture خراب نشود.
