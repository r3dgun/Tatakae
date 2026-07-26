# SQL Server Database Integration

این نسخه ذخیره‌سازی In-Memory را کنار گذاشته و Repositoryهای اصلی را روی SQL Server اجرا می‌کند.

## معماری

- `Tatakae.Domain`: مدل‌های اصلی فروشگاه، سفارش، گلدوزی و SEO
- `Tatakae.Application`: قراردادها، DTOها و سرویس‌های کاربردی
- `Tatakae.Infrastructure`: `TatakaeDbContext`، Entityهای Persistence، Mapperها و Repositoryهای SQL
- `Tatakae.Api`: Composition Root و ConnectionString

## Connection String

در فایل زیر قابل تنظیم است:

```json
src/Tatakae.Api/appsettings.json
```

پیش‌فرض:

```json
"ConnectionStrings": {
  "TatakaeSqlServer": "Server=(localdb)\\MSSQLLocalDB;Database=TatakaeEmbroideryCommerce;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

برای SQL Server واقعی:

```json
"TatakaeSqlServer": "Server=.;Database=TatakaeEmbroideryCommerce;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

یا با یوزر/پسورد:

```json
"TatakaeSqlServer": "Server=YOUR_SERVER;Database=TatakaeEmbroideryCommerce;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## ساخت دیتابیس

API هنگام اجرا، دیتابیس و جدول‌ها را با EF Core می‌سازد و Seed اولیه را وارد می‌کند:

```powershell
dotnet run --project .\src\Tatakae.Api\Tatakae.Api.csproj
```

در صورت نیاز، اسکریپت SQL دستی هم وجود دارد:

```text
scripts/sql/01-create-database.sql
```

## EF Core Migration

برای پروژه واقعی، به‌جای `EnsureCreated` از Migration استفاده کن:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialSqlServer -p .\src\Tatakae.Infrastructure -s .\src\Tatakae.Api -c TatakaeDbContext
dotnet ef database update -p .\src\Tatakae.Infrastructure -s .\src\Tatakae.Api -c TatakaeDbContext
```

بعد در `DatabaseInitializer`، `EnsureCreatedAsync` را با `MigrateAsync` جایگزین کن.

## جدول‌های اصلی

- Categories
- Products
- ProductImages
- ProductVariants
- ProductSpecifications
- ProductTags
- ProductEmbroideryPolicies
- ProductAllowedPlacements
- ProductAllowedThreadColors
- Customers
- CustomerAddresses
- Orders
- OrderLines
- Coupons

## نکته تولیدی

این نسخه دیتابیس واقعی دارد، اما هنوز برای production کامل باید موارد زیر اضافه شود:

- Identity/JWT واقعی برای ورود و نقش Admin
- Password hashing
- Upload واقعی فایل طرح گلدوزی
- Payment gateway
- Migrationهای رسمی به‌جای EnsureCreated
- RowVersion برای کنترل همزمانی موجودی
