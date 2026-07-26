# Phase 26.9 — Zarinpal Sandbox and production gap review

## نتیجه اجرایی

زرین‌پال برای اجرای محلی روی **Sandbox** تنظیم شده است:

- `Zarinpal:Enabled = true`
- `Zarinpal:Sandbox = true`
- API: `https://sandbox.zarinpal.com/pg/v4/payment/`
- StartPay: `https://sandbox.zarinpal.com/pg/StartPay/`
- Currency: `IRT`، چون قیمت‌های پروژه به تومان نگهداری می‌شوند.
- Refund در Sandbox خاموش است تا endpoint مالی GraphQL محیط Production تصادفی فراخوانی نشود.

تنها مقدار عمداً داخل repository قرار نگرفته، `MerchantId` است. آن را با User Secrets تنظیم کنید:

```powershell
.\scripts\configure-zarinpal-sandbox.ps1 `
  -MerchantId "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

بعد از ورود ادمین، وضعیت تنظیمات بدون نمایش Merchant ID یا Access Token:

```text
GET /api/admin/payments/zarinpal/configuration
```

`RequestReady=true` یعنی Request/Redirect/Verify قابل آزمایش است.

## تغییرات ایمنی Sandbox

1. `UserSecretsId` به پروژه API اضافه شد.
2. اسکریپت تنظیم امن Sandbox اضافه شد.
3. endpoint تشخیصی ادمین اضافه شد و هیچ secretی را برنمی‌گرداند.
4. mode و آماده‌بودن Merchant ID هنگام startup بدون نمایش مقدار secret لاگ می‌شود.
5. Refund در Sandbox پیش‌فرض غیرفعال است.
6. هنگام Sandbox، کد Refund دیگر هرگز به `GraphQlUrl` محیط Production fallback نمی‌کند.
7. تست regression اضافه شد تا ثابت کند Refund در Sandbox پیش از ارسال HTTP متوقف می‌شود.

## نقاط قوت فعلی پروژه

- مرزبندی Domain/Application/Infrastructure قابل قبول است.
- موفقیت callback مستقیماً معتبر فرض نمی‌شود؛ Verify سمت سرور انجام می‌شود.
- Authority و مبلغ از داده ذخیره‌شده سرور کنترل می‌شوند.
- کدهای Verify `100` و `101` به‌صورت idempotent مدیریت می‌شوند.
- پاسخ حساس `card_hash` قبل از persistence حذف می‌شود.
- رزرو موجودی پایدار، cleanup دوره‌ای Hangfire و transition شرطی وجود دارد.
- پوشش تست پروژه قابل توجه است و تست‌های Gateway/Payment/Reservation وجود دارند.

# کمبودهای مهم

## P0 — قبل از انتشار عمومی

### 1. Merchant ID هنوز تنظیم نشده است

مقادیر `Zarinpal:MerchantId` در فایل‌های تنظیمات خالی‌اند. Sandbox روشن است، اما تا Merchant ID معتبر از User Secrets یا Environment Variable داده نشود، Request پرداخت عمداً با خطای configuration متوقف می‌شود.

### 2. دیتابیس Migration ندارد

`DatabaseInitializer` از `EnsureCreatedAsync` استفاده می‌کند و خود کامنت کد نیز اعلام کرده که برای Production باید با Migration جایگزین شود. با تغییر schema، `EnsureCreated` دیتابیس موجود را ارتقا نمی‌دهد.

اقدام پیشنهادی:

- ایجاد Initial Migration
- استفاده از `MigrateAsync` در deployment کنترل‌شده
- backup پیش از migration
- migration bundle یا مرحله جداگانه CI/CD

### 3. Upload فایل یک ریسک امنیتی جدی دارد

`POST /api/files/upload` در وضعیت فعلی:

- Authorization ندارد.
- `purpose` را از کاربر گرفته و مستقیم در مسیر filesystem استفاده می‌کند.
- MIME type و extension ارسال‌شده از مرورگر را قابل اعتماد فرض می‌کند.
- SVG/PDF را در `wwwroot` قرار می‌دهد.
- malware scan، magic-byte validation و quota ندارد.

این وضعیت می‌تواند به path traversal، ذخیره فایل فعال، abuse فضای دیسک و XSS ذخیره‌شده منجر شود.

اقدام پیشنهادی فوری:

- محدودکردن endpoint به کاربر authenticated
- allow-list ثابت برای purpose
- ذخیره خارج از `wwwroot`
- stream از endpoint download با `Content-Disposition`
- magic-byte validation و sanitize کردن SVG یا حذف SVG
- quota، rate limit و malware scanning

### 4. JWT و تنظیمات HTTPS برای Production آماده نیست

- Signing key توسعه داخل `appsettings.json` ثبت شده است.
- در `Program.cs` یک fallback ثابت دیگر هم وجود دارد.
- `RequireHttpsMetadata=false` برای تمام محیط‌ها اعمال می‌شود.
- توکن Remember Me تا ۷ روز معتبر است و refresh-token/revocation وجود ندارد.
- Session در Blazor داخل `localStorage` ذخیره می‌شود و در برابر XSS حساس است.

اقدام پیشنهادی:

- fail-fast در Production اگر SigningKey secret تنظیم نشده باشد
- `RequireHttpsMetadata = !Development`
- access token کوتاه + refresh token چرخشی و قابل ابطال
- CSP جدی و ترجیح BFF/HttpOnly cookie برای پنل حساس

### 5. Rate limiting وجود ندارد

هیچ `AddRateLimiter/UseRateLimiter` در API دیده نشد. مسیرهای ورود، ثبت‌نام، پرداخت، callback، contact و upload باید policy جدا داشته باشند.

### 6. پرداخت موفق وابسته به بازگشت مرورگر است

اگر مشتری در بانک پرداخت کند ولی مرورگر به callback برنگردد، رکورد می‌تواند در وضعیت Redirected/Pending باقی بماند. Job موجود فقط رزرو موجودی را cleanup می‌کند و reconciliation پرداخت زرین‌پال ندارد.

اقدام پیشنهادی:

- Job دوره‌ای برای paymentهای Redirected/Pending نزدیک انقضا
- استفاده از API رسمی unverified/inquiry در صورت دسترسی حساب
- Verify مجدد فقط با Authority و مبلغ ذخیره‌شده
- وضعیت `ReconciliationRequired` برای نتایج نامشخص
- عدم آزادسازی رزرو پرداختی که احتمال موفقیت آن هنوز وجود دارد

### 7. Checkout کلید idempotency ندارد

Payment شروع‌شده idempotency نسبی دارد، اما Checkout با retry مرورگر یا timeout می‌تواند سفارش جدید ایجاد کند. `Idempotency-Key` یا کلید یکتای client request در قرارداد Checkout وجود ندارد.

## P1 — آمادگی عملیاتی

### 8. Health check واقعی وجود ندارد

`/health` همیشه یک JSON ثابت برمی‌گرداند و SQL Server، Hangfire، filesystem و آمادگی تنظیمات پرداخت را بررسی نمی‌کند. Liveness و Readiness باید جدا شوند.

### 9. Observability محدود است

Structured logging پایه وجود دارد، ولی موارد زیر دیده نشد:

- OpenTelemetry traces/metrics
- correlation ID مشترک Web/API/Payment
- dashboard خطا و alert
- metricهای payment success/failure/uncertain
- metricهای reservation expiry و reconciliation lag

### 10. CI/CD و کنترل کیفیت خودکار وجود ندارد

Workflow، Dockerfile و pipeline در repository دیده نشد. حداقل pipeline باید restore/build/test، format/analyzer، dependency scan و migration validation را اجرا کند.

### 11. فایل‌ها روی دیسک محلی API ذخیره می‌شوند

برای چند instance، container یا deployment مجدد مناسب نیست. object storage، lifecycle، backup و signed URL لازم است.

### 12. فهرست‌های مدیریتی pagination یکدست ندارند

چند Repository مجموعه کامل را با `ToListAsync/ToArrayAsync` می‌خوانند. با رشد سفارش، مشتری، فایل و پرداخت، حافظه و زمان پاسخ بالا می‌رود.

### 13. سرویس اعلان Production کامل نیست

صف اعلان وجود دارد، اما provider واقعی پیامک/ایمیل، retry policy، dead-letter و outbox تراکنشی باید تکمیل شود.

### 14. تنظیمات محیط Production تفکیک نشده است

فایل پایه شامل LocalDB، JWT توسعه، Seed و Sandbox است. بهتر است:

- فایل پایه فقط defaultهای غیرحساس داشته باشد.
- Development تنظیمات localhost و Sandbox را نگه دارد.
- Production فقط از secret store و environment استفاده کند.
- startup guard مانع Production با LocalDB، Sandbox یا signing key توسعه شود.

### 15. زمان‌ها یکدست نیستند

بخشی از persistence از `DateTime.Now` و بخش پرداخت/رزرو از `DateTimeOffset.UtcNow` استفاده می‌کند. برای audit و چند منطقه زمانی، همه زمان‌های server-side باید UTC باشند.

### 16. مستندات README بخشی قدیمی است

README هنوز در بخشی پروژه را InMemory معرفی می‌کند، درحالی‌که نسخه فعلی SQL Server، Identity، Hangfire و پرداخت واقعی دارد. این موضوع راه‌اندازی و تصمیم deployment را گمراه می‌کند.

## P2 — بلوغ محصول

- SSR/Prerender برای SEO پایدار صفحات فروشگاه
- تست End-to-End واقعی با API و WebApplicationFactory
- تست قرارداد Sandbox در یک pipeline جدا، بدون اجرای تراکنش Production
- سیاست backup/restore و disaster recovery
- سیاست نگهداری AuditLog و اطلاعات پرداخت
- dashboard عملیات برای رزرو منقضی، پرداخت نامشخص و Refund نیازمند تطبیق
- pagination/filter/export استاندارد برای پنل ادمین
- محدودیت concurrency و row-version برای ویرایش‌های ادمین خارج از مسیر رزرو

# ترتیب پیشنهادی فازهای بعدی

1. **Phase 27 — Production security:** Upload hardening، rate limiting، JWT secrets و HTTPS.
2. **Phase 28 — Database lifecycle:** Migration، backup، deployment و UTC cleanup.
3. **Phase 29 — Payment reconciliation:** inquiry/unverified job، وضعیت نامشخص و dashboard تطبیق.
4. **Phase 30 — Operations:** Health checks، OpenTelemetry، alerting و Hangfire operations.
5. **Phase 31 — Delivery:** object storage، provider پیامک/ایمیل و transactional outbox.

## منابع بیرونی بررسی Sandbox

- ZarinPal-Lab SampleCode-Csharp: راهنمای رسمی نمونه اعلام می‌کند برای Sandbox میزبان `www.zarinpal.com` با `sandbox.zarinpal.com` جایگزین شود.
- endpointهای v4 استفاده‌شده در پروژه با نمونه‌ها و کتابخانه‌های متداول v4 هم‌راستا هستند؛ بااین‌حال پیش از Production باید قرارداد فعلی حساب پذیرنده و قابلیت Refund از پشتیبانی/مستندات روز زرین‌پال تأیید شود.
