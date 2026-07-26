# DTO Layer

این نسخه DTOها را به‌صورت مستقل از مدل‌های Domain و مدل‌های Code First دیتابیس نگه می‌دارد. هدف این است که:

- مدل‌های Code First فقط برای EF Core و SQL Server باشند.
- Entityهای Domain منطق کسب‌وکار را نگه دارند.
- DTOها قرارداد API، فرم‌های Blazor، اعتبارسنجی ورودی و خروجی صفحه‌ها را مدیریت کنند.

## مسیر DTOها

```text
src/Tatakae.Application/Contracts
```

## گروه‌های اصلی DTO

```text
Account       Login, Register, Profile, Address, ChangePassword
Admin         Dashboard, Grid, Product Row, Order Row, Customer Row, Coupon Row
Cart          Cart, CartLine, AddToCart, UpdateCartLine, ApplyCoupon
Categories    Category, AdminCategory, SEO input
Common        ApiResult, ApiError, Money, Pagination, Breadcrumb
Coupons       Coupon, AdminCoupon
Customers     Customer summary
Embroidery    Studio payload, quote, saved embroidery configuration
Files         Upload request, upload policy, uploaded file
Inventory     Inventory row, adjustment, movement
Lookups       Enum, size, color and store lookup DTOs
Orders        Checkout, Address, Order, OrderLine, Tracking, Timeline
Payments      Payment init and verify DTOs
Products      Product card, detail, listing, filter, options, admin product input
Reviews       Product review and moderation DTOs
Seo           SEO page input, sitemap item, SEO audit
Shipping      Shipping quote, shipping method, shipment
Studio        Studio state, presets, preview
Wishlist      Wishlist toggle and wishlist result
```

## قانون معماری

Controllerها و کامپوننت‌های Blazor باید فقط با DTOها کار کنند، نه با مدل‌های EF Core. Repositoryها با Domain یا Persistence کار می‌کنند و Application Serviceها وظیفه map کردن خروجی به DTO را دارند.

## DataAnnotations

برای request DTOها از DataAnnotation استفاده شده است:

```csharp
[Required]
[StringLength]
[RegularExpression]
[Range]
[EmailAddress]
[Phone]
[Url]
[MinLength]
[MaxLength]
```

این اعتبارسنجی هم در API Controller قابل استفاده است و هم در Blazor `EditForm`.
