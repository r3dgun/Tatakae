# Phase 25 - Zarinpal production payment boundary

این فاز پرداخت آزمایشی را با جریان واقعی Request / Redirect / Callback / Verify زرین‌پال جایگزین می‌کند، Reverse کامل و Refund رسمی را اضافه می‌کند و مرز Clean Architecture را اصلاح می‌کند.

## جریان پرداخت آنلاین

1. Web درخواست `POST /api/payments/start` را برای سفارش ثبت می‌کند.
2. `PaymentService` مالکیت سفارش، وضعیت قابل پرداخت و idempotency را بررسی می‌کند.
3. Application یک رکورد پرداخت Pending ایجاد می‌کند.
4. `IZarinpalPaymentGateway.RequestAsync` توسط adapter زرین‌پال اجرا می‌شود.
5. Authority ذخیره و URL انتقال به StartPay به Web برگردانده می‌شود.
6. زرین‌پال مرورگر را به `GET /api/payments/zarinpal/callback` بازمی‌گرداند.
7. callback ناشناس است، اما نتیجه query string منبع حقیقت نیست. Application ابتدا `paymentId` و Authority ذخیره‌شده را تطبیق می‌دهد و سپس Verify سمت سرور را با مبلغ ذخیره‌شده انجام می‌دهد.
8. فقط پس از Verify موفق، `Order.MarkPaid()` اجرا و Payment، Transaction، Order و OrderStatusHistory در یک واحد persistence ذخیره می‌شوند.
9. صفحه نتیجه Web فقط `paymentId/orderId` را از URL می‌گیرد و وضعیت نهایی را دوباره از API دریافت می‌کند.

## مرز معماری

### Application

- `PaymentService`: هماهنگی use case، مالکیت سفارش، idempotency، قواعد callback، Reverse/Refund و فراخوانی رفتار Domain.
- `IZarinpalPaymentGateway`: port اختصاصی provider؛ هیچ EF Core یا Aggregate در قرارداد آن وجود ندارد.
- `IPaymentRepository`: port persistence.
- `PersistPaymentOutcome` و `PersistPaymentRefundOutcome`: snapshotهای immutable از تصمیم Application/Domain؛ Aggregate دامنه به Infrastructure پاس داده نمی‌شود.

### Domain

- `Order.MarkPaid()`، `Order.MarkPaymentFailed()` و transition بازپرداخت، invariantهای سفارش را اجرا می‌کنند.
- Domain هیچ وابستگی به زرین‌پال، HTTP، EF Core یا `ResultDto` ندارد.

### Infrastructure

- `ZarinpalPaymentGateway`: فقط HTTP/JSON زرین‌پال، تبدیل مبلغ، redaction و ترجمه پاسخ provider.
- `EfPaymentRepository`: فقط خواندن و ذخیره Payment/Transaction/Refund/Order snapshot.
- Infrastructure درباره Paid/Failed/Refunded بودن سفارش تصمیم نمی‌گیرد.

## endpointها

```text
POST  /api/payments/start
GET   /api/payments/order/{orderId}
GET   /api/payments/zarinpal/callback
GET   /api/admin/payments
PATCH /api/admin/payments/{paymentId}/status
POST  /api/admin/payments/{paymentId}/refund
```

endpoint آزمایشی `simulate-success` حذف شده است.

## تنظیمات

برای Development/Sandbox:

```json
"Zarinpal": {
  "MerchantId": "YOUR-SANDBOX-MERCHANT-ID",
  "AccessToken": "YOUR-REFUND-ACCESS-TOKEN",
  "Sandbox": true,
  "Currency": "IRT",
  "CallbackUrl": "https://localhost:7075/api/payments/zarinpal/callback",
  "ProductionApiBaseUrl": "https://payment.zarinpal.com/",
  "SandboxApiBaseUrl": "https://sandbox.zarinpal.com/",
  "ProductionStartPayBaseUrl": "https://www.zarinpal.com/pg/StartPay/",
  "SandboxStartPayBaseUrl": "https://sandbox.zarinpal.com/pg/StartPay/",
  "GraphQlUrl": "https://next.zarinpal.com/api/v4/graphql/",
  "TimeoutSeconds": 30
},
"Payments": {
  "WebReturnUrl": "https://localhost:7076/payment-result"
}
```

برای Production، secret را داخل repository قرار ندهید:

```powershell
$env:Zarinpal__MerchantId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
$env:Zarinpal__AccessToken="your-refund-access-token"
$env:Zarinpal__Sandbox="false"
$env:Zarinpal__Currency="IRT"
$env:Zarinpal__CallbackUrl="https://api.example.com/api/payments/zarinpal/callback"
$env:Payments__WebReturnUrl="https://example.com/payment-result"
```

`IRT` یعنی مبلغ‌های پروژه به تومان ذخیره می‌شوند. اگر `IRR` انتخاب شود، adapter هنگام ارسال مبلغ را در ۱۰ ضرب و هنگام خواندن مبلغ Refund آن را به تومان برمی‌گرداند.

## امنیت و idempotency

- مقدار `Status=OK` فقط باعث شروع Verify سمت سرور می‌شود و هرگز به‌تنهایی موفقیت پرداخت را ثابت نمی‌کند؛ `Status=NOK` نیز فقط لغو/عدم تکمیل را ثبت می‌کند.
- Authority callback باید دقیقاً با Authority ذخیره‌شده برابر باشد.
- مبلغ Verify از رکورد سرور خوانده می‌شود، نه از مرورگر.
- کدهای Verify موفق 100 و 101 هر دو idempotent پذیرفته می‌شوند.
- callback تکراری دوباره تراکنش یا history ایجاد نمی‌کند.
- ایجاد هم‌زمان پرداخت فعال و ثبت Refund با transaction سریال‌شونده کنترل می‌شود.
- timeout یا قطع شبکه هنگام Request به‌عنوان نتیجه نامشخص نگه داشته می‌شود؛ تا پنج دقیقه درخواست جدید ساخته نمی‌شود تا session تکراری در زرین‌پال ایجاد نشود.
- مجموع Refundهای Requested/Approved/Completed نمی‌تواند از مبلغ پرداخت بیشتر شود.
- درخواست Refund یکسان دوباره به provider ارسال نمی‌شود.
- مبلغ تکمیل‌شده provider باید دقیقاً با مبلغ درخواست تطبیق داشته باشد؛ در غیر این صورت سفارش تغییر وضعیت نمی‌دهد و reconciliation دستی لازم است.
- `card_hash` در raw response ذخیره نمی‌شود و redacted است.
- صفحه نتیجه Web فقط وضعیت ذخیره‌شده API را معتبر می‌داند.

## Reverse کامل

Reverse کامل تراکنش از endpoint REST رسمی زرین‌پال انجام می‌شود:

```text
POST /pg/v4/payment/reverse.json
```

وقتی مدیر وضعیت یک پرداخت آنلاین تأییدشده زرین‌پال را به `Reversed` تغییر می‌دهد:

1. Application نوع gateway، وضعیت Verified/Succeeded و Authority را کنترل می‌کند.
2. adapter درخواست Reverse واقعی را به زرین‌پال می‌فرستد.
3. فقط پس از پاسخ موفق، Domain سفارش را به `Refunded` و پرداخت را به `Reversed` تغییر می‌دهد.
4. timeout یا نتیجه نامشخص هیچ تغییری در سفارش ایجاد نمی‌کند و مدیر باید وضعیت را در پنل زرین‌پال بررسی کند.

تأیید یا ناموفق‌کردن دستی پرداخت آنلاین زرین‌پال مسدود است؛ پرداخت آنلاین فقط از Verify سرور تغییر وضعیت می‌دهد. عملیات دستی برای روش‌هایی مانند کارت‌به‌کارت باقی مانده است.

## Refund کامل یا جزئی

Refund رسمی از GraphQL زرین‌پال و mutation `AddRefund` استفاده می‌کند:

- credential آن `AccessToken` جداگانه است.
- شناسه تراکنش `session_id` از Authority ذخیره‌شده سرور خوانده می‌شود.
- مبلغ می‌تواند کامل یا جزئی باشد.
- API ورودی مبلغ را می‌پذیرد؛ پنل فعلی دکمه Refund کامل دارد.
- تا وقتی provider فقط درخواست را پذیرفته ولی تکمیل بانکی را اعلام نکرده است، Refund در وضعیت `Approved` می‌ماند و Order/Payment تغییر نمی‌کنند.
- Refund جزئی تکمیل‌شده یک PaymentTransaction از نوع `Refunded` ثبت می‌کند، ولی Payment اصلی Verified باقی می‌ماند.
- وقتی مجموع Refundهای تکمیل‌شده به کل مبلغ پرداخت برسد، Application رفتار Domain را اجرا می‌کند و Order/Payment به Refunded منتقل می‌شوند.
- timeout، پاسخ نامعتبر یا mismatch مبلغ به تغییر خودکار Order منجر نمی‌شود.

## تست‌ها

- ساخت request رسمی و payload
- URL انتقال و Authority
- Verify موفق با کد 100
- Verify تکراری با کد 101
- تبدیل IRT/IRR
- ترجمه errors و redaction اطلاعات حساس
- ownership سفارش، idempotency ایجاد پرداخت و جلوگیری از duplicate request هنگام timeout
- callback با Authority اشتباه، NOK و callback تکراری
- persistence اتمیک Payment/Order/History
- اجرای Reverse واقعی پیش از ثبت Reversed/Refunded
- جلوگیری از mutation هنگام نتیجه نامشخص Reverse
- GraphQL Refund با Bearer access token
- Refund کامل، Refund جزئی، idempotency و سقف مجموع Refund
- عدم mutation سفارش تا تکمیل provider
- عدم وابستگی adapter به DbContext/Order/Repository
- عدم اعتماد صفحه نتیجه به query string
