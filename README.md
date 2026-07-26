# Tatakae Embroidery Commerce

نمونه فروشگاه کامل لباس با **سفارشی‌سازی فقط گلدوزی**، پیاده‌سازی‌شده با **Blazor WebAssembly + ASP.NET Core Web API + Clean Architecture**.

> این پروژه، طرح HTML اولیه را به یک مدل فروشگاهی عملیاتی تبدیل می‌کند: لباس، دسته‌بندی، واریانت/SKU، موجودی، قوانین گلدوزی، سبد خرید، Checkout، سفارش، کد تخفیف، مشتری و پنل ادمین.

## قابلیت‌های پیاده‌سازی‌شده

### فروشگاه عمومی

- صفحه خانه، دسته‌بندی و فروشگاه با فیلتر، جست‌وجو، مرتب‌سازی و صفحه‌بندی
- صفحه دسته‌بندی با URL خوانا: `/category/{slug}`
- صفحه محصول با URL خوانا: `/product/{slug}`
- صفحه محصول شامل H1، breadcrumb، گالری، رنگ/سایز، موجودی، مشخصات، راهنمای نگهداری و CTA سفارشی‌سازی
- SEO صفحه محصول: `title`، description، canonical، Open Graph، robots و JSON-LD نوع `Product`/`Offer`
- استودیو فقط گلدوزی: انتخاب لباس و واریانت، آپلود طرح یا متن، محل گلدوزی، ابعاد، رنگ نخ و قیمت‌گذاری لحظه‌ای
- سبد خرید Drawer و Checkout با آدرس، موبایل، کد تخفیف و تبدیل خط‌های سبد به سفارش واقعی
- کارت سفارش برای کاربر شامل وضعیت، خطوط سفارش، تنظیمات گلدوزی، مبلغ، کد رهگیری و یادداشت مدیر

### پنل ادمین

- `/admin` داشبورد فروش، موجودی کم و سفارش‌های اخیر
- `/admin/products` مدیریت محصولات
- `/admin/products/new` و `/admin/products/{id}` ویرایش محصول کامل
- `/admin/categories` مدیریت دسته‌بندی و SEO آن
- `/admin/orders` فهرست سفارش‌های گلدوزی
- `/admin/orders/{id}` جزئیات سفارش و گردش وضعیت تولید
- `/admin/customers` نمای پایه CRM
- `/admin/coupons` مدیریت کدهای تخفیف

### مدل‌های حرفه‌ای و اعتبارسنجی

مدل‌ها و DTOها در `Tatakae.Application/Contracts` با Data Annotation طراحی شده‌اند:

- Required، StringLength، Range، RegularExpression، Url، MinLength و MaxLength
- اعتبارسنجی Slug، SKU، کد رنگ HEX، موبایل ایران، کد پستی، قیمت، موجودی، واریانت و تنظیمات گلدوزی
- اعتبارسنجی تجاری در Serviceها: موجودی، نرخ فروش ویژه، Slug یکتا، محدودیت رنگ نخ، ابعاد، محل مجاز گلدوزی و کد تخفیف

## Phase 12 - SEO و صفحات قانونی

- slug فارسی و انگلیسی برای محصول، دسته‌بندی و صفحات قانونی
- title/meta، canonical، Open Graph، robots و JSON-LD مستقل
- sitemap و robots داینامیک از دیتابیس و سیاست مسیرها
- صفحات درباره ما، قوانین، حریم خصوصی، مرجوعی، ارسال و تماس قابل مدیریت
- noindex برای حساب، Checkout، پرداخت، استودیو و صفحات نتیجه سفارش

راهنمای کامل: `docs/phase12-seo-legal.md`  
راهنمای تست‌ها: `docs/phase12-seo-legal-tests.md`

## اجرای پروژه

نیازمندی: `.NET SDK 10`

ترمینال اول:

```bash
cd src/Tatakae.Api
dotnet run
```

API به‌صورت پیش‌فرض روی `http://localhost:5075` اجرا می‌شود.

ترمینال دوم:

```bash
cd src/Tatakae.Web
dotnet run
```

سپس آدرس خروجی Web را در مرورگر باز کنید.

## ساختار راهکار

```text
src/
├── Tatakae.Domain/            # Entity، enum و قوانین دامنه
├── Tatakae.Application/       # DTO، Data Annotation، قراردادها و Use Caseها
├── Tatakae.Infrastructure/    # Repositoryهای In-Memory و داده‌های نمونه
├── Tatakae.Api/               # Web API عمومی، Checkout و API ادمین
└── Tatakae.Web/               # Blazor WebAssembly storefront و admin
```

جزئیات کامل در فایل‌های زیر آمده است:

- `docs/architecture.md`
- `docs/ecommerce-model.md`
- `docs/api-contracts.md`
- `docs/seo.md`

## نکات مهم برای production

نسخه فعلی از SQL Server، EF Core، ASP.NET Core Identity/JWT، Hangfire، رزرو موجودی و اتصال Request/Verify زرین‌پال استفاده می‌کند. بااین‌حال هنوز برای انتشار عمومی باید این موارد تکمیل شوند:

1. جایگزینی `EnsureCreated` با EF Core Migration و فرایند backup/deployment
2. انتقال JWT key، Merchant ID و سایر secretها به secret store و حذف کلیدهای توسعه
3. امن‌سازی upload و انتقال فایل‌ها از `wwwroot` محلی به object storage
4. rate limiting، health/readiness check، telemetry و alerting
5. reconciliation دوره‌ای پرداخت‌هایی که مرورگرشان به callback برنگشته است
6. provider واقعی ارسال، پیامک/ایمیل و transactional outbox
7. Blazor Web App یا ASP.NET Core SSR/Prerender برای SEO پایدار صفحات عمومی

گزارش اولویت‌بندی‌شده: `docs/phase26.9-zarinpal-sandbox-and-gap-review.md`.

## Kimi Award Ecommerce Page

The uploaded Kimi-style ecommerce page has been included without modifying its design.

Run the API and Web projects, then open:

- `/kimi-award`
- or directly `/kimi-award/index.html`

The original HTML is stored at:

`src/Tatakae.Web/wwwroot/kimi-award/index.html`

## Kimi Theme Integrated Version

در نسخه `kimi-integrated`، پوسته‌ی فایل HTML مرجع به صورت واقعی روی پروژه Blazor اعمال شده است. صفحات فروشگاه، محصول، دسته‌بندی، استودیو، سبد، پرداخت و سفارش‌ها از کلاس‌ها و ظاهر Kimi استفاده می‌کنند و فقط به صورت iframe یا فایل مستقل اضافه نشده‌اند.

مسیرهای اصلی:

- `/` صفحه اصلی روی پوسته Kimi
- `/shop` فروشگاه
- `/product/{slug}` صفحه محصول
- `/customize/{slug}` استودیوی گلدوزی
- `/checkout` پرداخت
- `/account/orders` سفارش‌ها
- `/admin` پنل مدیریت

برای جزئیات: `docs/kimi-theme-integration.md`


## Nika Cinematic Ecommerce Final

نسخه فعلی روی تم جدید ارسالی Nika/Cinematic سوار شده و شامل فروشگاه، صفحه محصول، استودیو، Checkout، Card/Order، ورود/ثبت‌نام، حساب کاربری، پنل ادمین و ساختار SEO است.

مسیرهای مهم:

- `/` صفحه اصلی سینمایی
- `/shop` فروشگاه
- `/category/{slug}` دسته‌بندی
- `/product/{slug}` صفحه محصول SEO-ready
- `/customize/{slug}` استودیوی گلدوزی
- `/checkout` پرداخت
- `/login`, `/register`, `/account` حساب کاربری
- `/account/orders` سفارش‌های کاربر
- `/admin` پنل مدیریت
- `/admin/seo` مرکز SEO

## SQL Server database

این نسخه Repositoryهای اصلی را به SQL Server وصل می‌کند. ConnectionString در `src/Tatakae.Api/appsettings.json` قرار دارد:

```json
"ConnectionStrings": {
  "TatakaeSqlServer": "Server=(localdb)\\MSSQLLocalDB;Database=TatakaeEmbroideryCommerce;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

هنگام اجرای API، دیتابیس و جدول‌ها به‌صورت خودکار ساخته و Seed اولیه وارد می‌شود. اسکریپت دستی هم اینجاست:

```text
scripts/sql/01-create-database.sql
```

راهنمای کامل‌تر: `docs/sql-server-database.md`.


## Code First SQL Server Models

این نسخه مدل‌های دیتابیس را به‌صورت Code First در مسیر زیر دارد:

```text
src/Tatakae.Infrastructure/Persistence/Models
```

مدل‌ها با DataAnnotation کامل شده‌اند: `Table`, `Key`, `Required`, `MaxLength`, `Precision`, `Index`, `ForeignKey`, `InverseProperty`, `Range`, `Phone`, `EmailAddress`, `Url`.

راهنمای کامل:

```text
docs/code-first-models.md
```

## DTO Layer Added

DTOهای کامل فروشگاهی در `src/Tatakae.Application/Contracts` اضافه شده‌اند. این DTOها برای فروشگاه، محصول، دسته‌بندی، استودیو، سبد خرید، checkout، ورود/ثبت‌نام، پنل ادمین، SEO، پرداخت، ارسال، موجودی، فایل، review و wishlist آماده‌اند.

راهنما: `docs/dto-layer.md`


## Iranian Commerce Model Set

نسخه جدید مدل‌های مخصوص فروشگاه ایرانی را به Code First اضافه کرده است: موبایل/OTP، استان و شهر، فروشنده و Offer، گارانتی، درگاه پرداخت ایرانی، کیف پول، فاکتور رسمی، ارسال، مرجوعی، انبار، نظر و پرسش محصول، SEO redirect و audit log.

راهنما: `docs/iranian-commerce-models.md`

## Identity / Role / Permission Authorization

پنل ادمین با ASP.NET Core Identity و JWT محافظت شده است. هر صفحه ادمین Permission جدا دارد و Roleها از طریق `AppRolePermissions` به Permissionها وصل می‌شوند.

ادمین پیش‌فرض Seed می‌شود:

```text
Mobile: 09120000000
Password: Admin@123456
```

بعد از ورود، از مسیر `/admin/security` می‌توان Roleها، Permissionها و کاربران را دید و Permissionهای هر Role را مدیریت کرد.

جزئیات کامل در `docs/identity-permission-authorization.md` آمده است.


## BaseEntity

مدل‌های Persistence به `BaseEntity<TKey>` مجهز شدند. توضیحات: `docs/base-entity-models.md`.

## نسخه نهایی بازبینی‌شده

این نسخه با نام دیتابیس `TatakaeEmbroideryCommerce_FinalReviewedV1` تنظیم شده و شامل BaseEntity، Identity، PermissionCheckerAttribute، فروشگاه با fallback، فیلتر واقعی، صفحات قوانین/ارتباط و پنل ادمین Permission است.

راهنمای کامل‌تر: `docs/final-structure-review.md`


## Visual Studio launch fix

در نسخه fixed6، پروژه Web با `launchBrowser: true` تنظیم شده تا هنگام اجرای همزمان API و Web، صفحه سایت به‌صورت خودکار روی `http://localhost:5076` باز شود. API روی `http://localhost:5075` اجرا می‌شود.

## اجرای HTTPS در لوکال

این نسخه روی HTTPS تنظیم شده است:

- API: `https://localhost:7075`
- Web: `https://localhost:7076`

اگر خطای certificate گرفتید:

```powershell
dotnet dev-certs https --trust
```

اجرای دستی:

```powershell
dotnet run --launch-profile https --project .\src\Tatakae.Api\Tatakae.Api.csproj
dotnet run --launch-profile https --project .\src\Tatakae.Web\Tatakae.Web.csproj
```

## Fixed10 - Category + Admin User

- صفحه دسته‌بندی فروشگاه بازنویسی شد و مسیرهای `/category/{slug}`, `/categories/{slug}` و `/shop/category/{slug}` را پشتیبانی می‌کند.
- دسته‌بندی‌ها اکنون محصول‌ها را همان ابتدای صفحه نشان می‌دهند و فیلترها فقط روی همان دسته اعمال می‌شوند.
- دو کاربر ادمین Seed شده‌اند:
  - `09123456789` / `Admin@123456`
  - `09120000000` / `Admin@123456`
- نام دیتابیس این نسخه: `TatakaeEmbroideryCommerce_FinalReviewedFixed10CategoryAdminV1`

## Phase 13 - Route-aware responsive layouts

ریسپانسیو اختصاصی Home، Shop، Category، Product، Studio، Cart، Checkout، Login، Account، Admin و Legal اضافه شده است. صفحه‌های Shop و Category فیلتر bottom-sheet دارند و Cart در موبایل به bottom-sheet مستقل تبدیل می‌شود.

راهنما:

```text
docs/phase13-responsive-layouts.md
docs/phase13-responsive-tests.md
```

## Phase 14 - Reliable development seed

Seed فاز ۱۴ با شناسه‌های ثابت و اجرای idempotent اضافه شده است. این Seed محصول آماده، قابل شخصی‌سازی، تخفیف‌خورده و ناموجود، سفارش و آدرس تستی، پرسش‌وپاسخ و حساب‌های مشتری/ادمین را فراهم می‌کند. fixtureهای دمو فقط در Development فعال هستند.

```text
Customer: 09121234567 / Customer@123456
Admin:    09120000000 / Admin@123456
Order:    EMB-TEST-0001
```

راهنما:

```text
docs/phase14-reliable-seed.md
docs/phase14-seed-tests.md
```

## Form validation hardening

تمام فرم‌های ورودی اصلی فروشگاه و پنل مدیریت اکنون اعتبارسنجی سمت کاربر و سمت API دارند. اعتبارسنجی بازگشتی برای مدل‌های تو‌در‌تو مانند آدرس Checkout، واریانت‌های محصول و تنظیمات گلدوزی اضافه شده و ارقام فارسی/عربی در موبایل، کدپستی و فیلتر قیمت پشتیبانی می‌شوند.

راهنما:

```text
docs/phase15-form-validation.md
```

## ResultDto coupon repository/service refactor

The coupon vertical slice now uses `ResultDto` directly across `ICouponRepository`, `IAdminCouponService`, `ICouponService`, API controllers, and Web clients. See `docs/phase16-resultdto-coupon-pattern.md` for the contract and implementation convention.

## Phase 17: soft delete

Database entities derived from `BaseEntity` now use global EF Core query filters. Delete operations set `IsRemoved` and `RemoveTime` instead of physically deleting user-facing records. See `docs/phase17-soft-delete.md`.

## Phase 18: unified ResultDto repositories

تمام Repositoryهای Application اکنون قرارداد یکسان `Task<ResultDto>` یا `Task<ResultDto<T>>` دارند. پیاده‌سازی‌های SQL از helper مشترک `RepositoryResult` استفاده می‌کنند و متن فارسی خطا، `ResultStatus`، `ErrorCode` و خطاهای فیلدی بدون از بین رفتن تا API منتقل می‌شوند. پاسخ‌های خطای قدیمی Controller و ModelState نیز به‌صورت سراسری به ResultDto تبدیل می‌شوند. Soft Delete و Global Query Filter فاز ۱۷ حفظ شده‌اند.

راهنما:

```text
docs/phase18-unified-resultdto-repositories.md
```

## Phase 19: Clean Architecture API boundary

سرویس‌های احراز هویت، پرداخت، محتوای قانونی، امنیت، Permission، Cart و Locations از پروژه API خارج شدند. پیاده‌سازی use caseها و مدیریت `ResultDto` در Application قرار دارد و جزئیات EF Core، Identity، JWT و SQL Server پشت Gatewayهای Infrastructure پنهان شده‌اند. API فقط Controller، Middleware، Filter و Composition Root است و هیچ Controllerای مستقیماً `TatakaeDbContext` دریافت نمی‌کند. ثبت DI نیز به `AddTatakaeApplication()` و `AddTatakaeSqlInfrastructure()` تفکیک شده است.

راهنما:

```text
docs/phase19-clean-architecture-api-boundary.md
```

## Phase 20: Interface-only Controller dependencies

تمام Controllerهای API اکنون فقط Interfaceهای `Tatakae.Application.Interfaces.Services` را دریافت می‌کنند. ثبت DI نیز فقط به‌صورت interface-to-implementation انجام می‌شود و Application Serviceهای concrete مستقیماً در container منتشر نمی‌شوند. پاسخ خطا از `ResultDto` و mapper مشترک HTTP عبور می‌کند.

راهنما:

```text
docs/phase20-controller-service-interfaces.md
```

## Phase 21: Clean Web presentation boundary

سرویس‌های مرورگر از پوشه عمومی `Services` تفکیک شدند. Razor Componentها فقط Interfaceهای Presentation را inject می‌کنند، API Clientها از transport مشترک `ResultDto` استفاده می‌کنند، Bearer Token در یک handler مرکزی افزوده می‌شود و stateهای `localStorage` در پوشه `State` باقی مانده‌اند. `StoreFallbackCatalog` حذف شده و داده دمو فقط باید از Development Seed سمت سرور تأمین شود.

راهنما:

```text
docs/phase21-web-presentation-clean-boundary.md
```

## Phase 24 — AI search readiness

مسیرهای `/llms.txt`، `/llms-full.txt` و `/ai/catalog.json`، کنترل crawlerهای OpenAI، JSON-LD محصول/واریانت/نظر/پرسش و auditهای AEO اضافه شده‌اند. تنظیمات در بخش `AiSeo` فایل appsettings قرار دارد. جزئیات: `docs/phase24-ai-search-readiness.md`.
## Phase 25 - Zarinpal payment

پرداخت آنلاین آزمایشی با جریان واقعی Request / Redirect / Callback / Verify زرین‌پال جایگزین شده است. `PaymentService` در Application مالک use case و تغییر وضعیت Domain است؛ `ZarinpalPaymentGateway` فقط adapter HTTP و `EfPaymentRepository` فقط persistence است. صفحه نتیجه نیز به query string اعتماد نمی‌کند و وضعیت پرداخت را از API بازخوانی می‌کند. Reverse کامل REST و Refund رسمی کامل/جزئی GraphQL با access token، idempotency، تطبیق مبلغ و persistence اتمیک پیاده‌سازی شده‌اند. برای تنظیمات و نکات امنیتی به `docs/phase25-zarinpal-production-payment.md` مراجعه کنید.


## Zarinpal Sandbox

حالت پیش‌فرض زرین‌پال در این نسخه `Sandbox` است. برای قراردادن Merchant ID در User Secrets و جلوگیری از ثبت secret داخل Git:

```powershell
.\scripts\configure-zarinpal-sandbox.ps1 `
  -MerchantId "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

سپس API را اجرا کنید و بعد از ورود ادمین، وضعیت بدون نمایش secret از endpoint زیر قابل بررسی است:

```text
GET /api/admin/payments/zarinpal/configuration
```

در Sandbox، Refund عمداً غیرفعال است؛ چون endpoint تنظیم‌شده‌ی GraphQL مربوط به Production است و نباید در تست به‌صورت تصادفی فراخوانی شود. جزئیات و بازبینی کمبودها در `docs/phase26.9-zarinpal-sandbox-and-gap-review.md` آمده است.
