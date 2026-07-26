# فاز ۱۹ — مرزبندی Clean Architecture

## قانون وابستگی

ساختار نهایی پروژه:

```text
Tatakae.Domain
        ↑
Tatakae.Application  ←  Tatakae.Infrastructure
        ↑                       ↑
        └──────── Tatakae.Api (Composition Root)

Tatakae.Web → Tatakae.Application
```

- `Domain` شامل Entity، Enum و قوانین مستقل دامنه است و به هیچ لایه دیگری وابسته نیست.
- `Application` شامل use caseها، اینترفیس سرویس‌ها، Gateway portها، DTOها، Validation و `ResultDto` است.
- `Infrastructure` فقط adapterهای EF Core، SQL Server، ASP.NET Identity و JWT را پیاده‌سازی می‌کند.
- `Api` فقط Controller، Middleware، Filter، استخراج Claims/IP/User-Agent و Composition Root است.
- `Api` پوشه `Services` ندارد و هیچ Controllerای مستقیماً `TatakaeDbContext` دریافت نمی‌کند.

## Use caseها در Application

پیاده‌سازی use caseهای زیر اکنون در `Tatakae.Application/Services` قرار دارد:

- `IdentityAuthService`
- `LegalContentService`
- `PaymentService`
- `PermissionService`
- `SecurityAdminService`
- `CartPersistenceService`
- `LocationService`

تمام این کلاس‌ها قراردادهای `I...Service` را پیاده‌سازی کرده و فقط `ResultDto` یا `ResultDto<T>` برمی‌گردانند.

## Gatewayها در Infrastructure

جزئیات وابسته به Framework در `Tatakae.Infrastructure/Gateways` قرار دارد:

- `AspNetIdentityAuthGateway`
- `EfLegalContentGateway`
- `EfPaymentRepository` برای persistence و `ZarinpalPaymentGateway` برای provider HTTP (از Phase 25)
- `EfPermissionGateway`
- `EfSecurityAdminGateway`
- `EfCartPersistenceGateway`
- `EfLocationGateway`

قرارداد Gatewayها در `Tatakae.Application.Interfaces.Gateways` تعریف شده است. این قراردادها هیچ نوعی از EF Core، Identity، JWT، `HttpContext` یا پروژه API را expose نمی‌کنند.

## Contextهای مستقل از HTTP

مقادیر HTTP در Controller به قراردادهای ساده تبدیل می‌شوند:

- `ClientRequestMetadata`
- `AuthenticatedSessionContext`
- `CartCustomerContext`

بنابراین `ClaimsPrincipal`، `HttpContext`، `HttpRequest` و connection object وارد Application یا Infrastructure use case نمی‌شوند.

## جریان ResultDto

تمام خطاهای مورد انتظار در Application به این قالب تبدیل می‌شوند:

```csharp
ResultDto
ResultDto<T>
```

و دارای این اطلاعات هستند:

```text
IsSuccess
Status
Message
ErrorCode
Errors
Data
```

نگاشت HTTP:

```text
ValidationError -> 400
Unauthorized    -> 401
Forbidden       -> 403
NotFound        -> 404
Conflict        -> 409
Failure         -> 500
```

پیام فارسی exceptionهای قابل پیش‌بینی در `ResultDto.Message` حفظ می‌شود. خطاهای غیرمنتظره log شده و پیام عمومی امن برگردانده می‌شود.

## Dependency Injection

ثبت use caseهای Application از طریق:

```csharp
services.AddTatakaeApplication();
```

و ثبت adapterهای Infrastructure از طریق:

```csharp
services.AddTatakaeSqlInfrastructure(configuration);
```

انجام می‌شود. `Program.cs` فقط Composition Root است و ثبت تک‌تک سرویس‌های کسب‌وکار را انجام نمی‌دهد.

## Soft Delete

رفتار فاز ۱۷ حفظ شده است. عملیات حذف همچنان `IsRemoved` و `RemoveTime` را تنظیم می‌کند و Global Query Filter رکوردهای حذف‌شده را از queryهای عادی کنار می‌گذارد.

## تست‌های مرزی

`CleanArchitectureServiceBoundaryTests` بررسی می‌کند که:

- API فاقد business service implementation باشد؛
- Application به API یا Infrastructure reference نداشته باشد؛
- use caseهای Application قراردادهای سرویس را پیاده‌سازی کنند؛
- Gatewayهای Infrastructure portهای Application را پیاده‌سازی کنند؛
- Controllerهای اصلاح‌شده فقط interfaceهای Application را دریافت کنند؛
- خطاها به‌صورت `ResultDto` با پیام فارسی برگردند.
