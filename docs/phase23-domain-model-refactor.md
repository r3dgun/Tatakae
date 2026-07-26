# Phase 23 — Domain Model Refactor

## هدف

لایه `Tatakae.Domain` باید فقط مدل و قوانین کسب‌وکار فروشگاه را نگه دارد. این لایه به Application، Infrastructure، API، Web، EF Core، HTTP، Logging و `ResultDto` وابسته نیست.

## ساختار

```text
Tatakae.Domain
├── Aggregates
│   ├── Catalog
│   │   ├── Category.cs
│   │   ├── Product.cs
│   │   └── ProductVariant.cs
│   ├── Customers
│   │   └── Customer.cs
│   ├── Orders
│   │   └── Order.cs
│   └── Promotions
│       └── Coupon.cs
├── ValueObjects
│   ├── Address.cs
│   ├── EmbroideryConfiguration.cs
│   ├── EmbroideryPolicy.cs
│   ├── ProductImage.cs
│   ├── ProductSpecification.cs
│   └── SeoMetadata.cs
├── Entities
│   └── InventoryMovement.cs
├── Common
│   └── DomainGuard.cs
└── Enums
```

## مرز مسئولیت

### Domain

- اعتبارسنجی موجودیت و Value Object
- محاسبه مبلغ سفارش و خطوط آن
- گردش مجاز وضعیت سفارش
- هماهنگی وضعیت پرداخت با وضعیت سفارش
- الزام کد رهگیری برای ارسال/تحویل
- قواعد اعتبار و مصرف کوپن
- قواعد موجودی، رزرو و مصرف SKU
- یکتایی SKU داخل Product aggregate
- الزام دقیقاً یک تصویر اصلی محصول
- محدودیت‌های ابعاد، رنگ و محل گلدوزی
- نگهداری دقیقاً یک آدرس پیش‌فرض برای مشتری

### Application

- تولید شناسه و شماره سفارش
- دریافت زمان جاری
- هماهنگی Repositoryها
- transaction و موجودی بین aggregateها
- ارسال اعلان
- تبدیل exception دامنه به `ResultDto`
- کنترل مجوز و actor جاری

### Infrastructure

- EF Core و SQL Server
- Identity و JWT
- درگاه پرداخت و سرویس‌های خارجی
- پیاده‌سازی Repository/Gateway

## تغییر مهم Order

`Order.Create` دیگر از `Guid.NewGuid()`، `DateTime.UtcNow` یا `Random.Shared` استفاده نمی‌کند. Application باید مقادیر زیر را ارسال کند:

```csharp
Order.Create(
    id,
    orderNumber,
    customerId,
    customerName,
    customerMobile,
    shippingAddress,
    lines,
    shippingAmount,
    discountAmount,
    shippingMethodCode,
    shippingMethodTitle,
    createdAt);
```

قواعد transition در خود aggregate قرار دارند:

```csharp
order.ChangeStatus(OrderStatus.ArtworkReview);
order.ChangeStatus(OrderStatus.InEmbroidery);
```

Application همچنان مسئول آزادسازی یا کسر موجودی و ثبت history است.

## Rehydrate

Factoryهای `Rehydrate` فقط برای بازسازی state ذخیره‌شده استفاده می‌شوند. آن‌ها invariantهای اصلی را کنترل می‌کنند، ولی هیچ I/O انجام نمی‌دهند.

## ResultDto

`ResultDto` وارد Domain نشده است. Domain با exceptionهای استاندارد قوانین نامعتبر را اعلام می‌کند و Application آن‌ها را به `ValidationError` یا `Conflict` تبدیل می‌کند.
