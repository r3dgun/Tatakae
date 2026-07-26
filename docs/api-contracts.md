# API Contract Map

## Public Storefront

| Method | Route | DTO |
|---|---|---|
| GET | `/api/products` | `ProductListQuery` → `PagedResult<ProductCardDto>` |
| GET | `/api/products/by-slug/{slug}` | `ProductDetailDto` |
| GET | `/api/categories` | `CategoryDto[]` |
| POST | `/api/checkout/quote-embroidery` | `EmbroideryCustomizationRequest` → `EmbroideryQuoteDto` |
| POST | `/api/checkout` | `CheckoutRequest` → `OrderDto` |

## Admin

| Method | Route | عملیات |
|---|---|---|
| GET/POST/PUT/DELETE | `/api/admin/products` | CRUD محصول |
| GET/POST/PUT/DELETE | `/api/admin/categories` | CRUD دسته‌بندی |
| GET/PATCH | `/api/admin/orders` | مشاهده و تغییر وضعیت سفارش |
| GET | `/api/admin/customers` | CRM پایه |
| GET/POST/PUT/DELETE | `/api/admin/coupons` | CRUD کد تخفیف |
| GET | `/api/admin/dashboard` | KPIها و سفارش‌های اخیر |

> در نسخه تولید، همه مسیرهای `/api/admin/*` باید تحت policy نقش `Admin` قرار گیرند.
