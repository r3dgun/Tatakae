# معماری Clean Architecture

```text
Tatakae.Web (Blazor WASM) ──HTTP──► Tatakae.Api
                                          │
                                          ▼
                                 Tatakae.Application
                                ↙         │          ↘
                       Tatakae.Domain   Use Cases   Repository interfaces
                                                   ▲
                                                   │
                                  Tatakae.Infrastructure
```

## مسئولیت هر لایه

| لایه | مسئولیت | نباید به چه چیزی وابسته باشد |
|---|---|---|
| Domain | Entityها، enumها و رفتارهای اصلی مانند موجودی و تخفیف | UI، HTTP، EF Core |
| Application | DTOها، Data Annotation، interfaceها و use caseها | Web، Controller، دیتابیس |
| Infrastructure | repository، seed، EF Core در نسخه واقعی | UI |
| Api | Controller، CORS، DI، HTTP contract | کامپوننت‌های Blazor |
| Web | UI، state سبد، API client، فرم‌ها | Domain entity مستقیم |

## جریان سفارش گلدوزی

```text
Product + Variant
     ↓
EmbroideryCustomizationRequest
     ↓
EmbroideryPricingService validates and quotes
     ↓
CartLine (client snapshot)
     ↓
CheckoutRequest
     ↓
OrderService validates inventory/coupon and creates Order
     ↓
ArtworkReview → InEmbroidery → QualityControl → Shipped
```
