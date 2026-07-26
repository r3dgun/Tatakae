# Iranian Commerce Code First Model Set

این نسخه مدل‌های دیتابیس را برای فروشگاه ایرانی توسعه می‌دهد. الگو از فروشگاه‌ها و مارکت‌پلیس‌های ایرانی گرفته شده است: موبایل و OTP، استان/شهر، کدپستی، فروشنده، گارانتی، درگاه پرداخت، کیف پول، مرجوعی، ارسال، فاکتور رسمی، نظر و پرسش محصول.

## اضافه‌شده به Domain Enums

`src/Tatakae.Domain/Enums/IranCommerceEnums.cs`

- `SellerType`, `SellerStatus`
- `WarrantyType`
- `PaymentMethod`, `IranianPaymentGateway`, `PaymentTransactionStatus`, `RefundStatus`
- `ShippingCarrier`, `ShipmentStatus`
- `ReturnRequestStatus`, `ReturnReason`
- `InvoiceType`, `InvoiceStatus`
- `MediaUsageType`
- `StockTransactionType`, `InventoryReservationStatus`
- `SeoRedirectType`
- `ReviewStatus`, `QuestionStatus`
- `UserRoleName`, `IranianAuthProvider`

## اضافه‌شده به Code First Models

`src/Tatakae.Infrastructure/Persistence/Models/IranCommerceDbRecords.cs`

### Geo
- `IranianProvinceDbRecord`
- `IranianCityDbRecord`

### Catalog / Marketplace
- `BrandDbRecord`
- `SellerDbRecord`
- `WarrantyDbRecord`
- `ProductOfferDbRecord`

### Auth / Customer
- `ApplicationUserDbRecord`
- `ApplicationUserRoleDbRecord`
- `OtpCodeDbRecord`
- `CustomerBankCardDbRecord`

### Payment / Wallet / Refund
- `PaymentDbRecord`
- `PaymentTransactionDbRecord`
- `RefundDbRecord`
- `WalletDbRecord`
- `WalletTransactionDbRecord`

### Shipping / Return / Invoice
- `ShippingMethodDbRecord`
- `ShippingZoneDbRecord`
- `ShipmentDbRecord`
- `ShipmentEventDbRecord`
- `ReturnRequestDbRecord`
- `ReturnRequestLineDbRecord`
- `InvoiceDbRecord`
- `InvoiceLineDbRecord`

### Inventory
- `WarehouseDbRecord`
- `StockItemDbRecord`
- `InventoryTransactionDbRecord`
- `InventoryReservationDbRecord`

### Engagement / SEO / Media
- `ProductReviewDbRecord`
- `ProductQuestionDbRecord`
- `MediaAssetDbRecord`
- `CartDbRecord`
- `CartItemDbRecord`
- `WishlistDbRecord`
- `DiscountCampaignDbRecord`
- `SeoRedirectDbRecord`
- `UrlSlugHistoryDbRecord`
- `AuditLogDbRecord`

## DTOs

`src/Tatakae.Application/Contracts/IranCommerce/IranianCommerceContracts.cs`

Request DTOها دارای DataAnnotation هستند، مخصوصاً برای ایران:

- موبایل: `^09[0-9]{9}$`
- کدپستی: `^[0-9]{10}$`
- کد ملی: `^[0-9]{10}$`
- شماره شبا: `^IR[0-9]{24}$`
- شماره کارت: `^[0-9]{16}$`

## نکته اجرایی

این مدل‌ها به `TatakaeDbContext` اضافه شده‌اند و با EF Core Code First جدول‌سازی می‌شوند. اگر دیتابیس قبلی ساخته شده است، برای تست سریع می‌توان نام دیتابیس را در `appsettings.json` تغییر داد تا مدل‌های جدید با `EnsureCreated` ساخته شوند.
