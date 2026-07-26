# Phase 18 — Unified ResultDto Repository Architecture

## تصمیم معماری

تمام Repositoryهای لایه Application اکنون فقط یکی از دو نوع زیر را برمی‌گردانند:

```csharp
Task<ResultDto>
Task<ResultDto<T>>
```

هیچ Repository عمومی خروجی خام مانند `Task<T?>`، `Task<bool>` یا `Task<IReadOnlyCollection<T>>` ندارد.

Repository مسئول این موارد است:

- اعتبارسنجی ابتدایی پارامترهای persistence
- اجرای عملیات دیتابیس
- برگرداندن نتیجه موفق، NotFound، Conflict یا Failure
- ثبت خطای Infrastructure با `ILogger`
- بازگرداندن پیام فارسی امن از طریق `ResultDto`

Service مسئول قوانین use case و تبدیل مدل دامنه به DTO است. خطای Repository بدون تغییر پیام، `Status` و `ErrorCode` به نتیجه Service منتقل می‌شود.

## قرارداد ResultDto

```csharp
public enum ResultStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}
```

ساختار پاسخ:

```json
{
  "isSuccess": false,
  "status": 2,
  "message": "محصول موردنظر پیدا نشد.",
  "errorCode": "product_not_found",
  "errors": null,
  "data": null
}
```

`Errors` برای خطاهای validation فیلدی استفاده می‌شود.

## Repositoryهای یکپارچه‌شده

- `IProductRepository`
- `ICategoryRepository`
- `ICouponRepository`
- `ICustomerRepository`
- `IOrderRepository`
- `IShippingMethodRepository`
- `IMediaAssetRepository`
- `IWishlistRepository`
- `IProductEngagementRepository`
- `IEmbroideryArtworkRepository`
- `INotificationRepository`

پیاده‌سازی‌های SQL همگی یک الگوی واحد دارند:

1. متدهای Core دیتابیس `private` هستند.
2. قرارداد عمومی فقط از طریق Interface در دسترس است.
3. `RepositoryResult` مدیریت مشترک query، find، mutation و command را انجام می‌دهد.
4. `OperationCanceledException` دوباره پرتاب می‌شود.
5. خطاهای قابل پیش‌بینی به `ValidationError`، `NotFound` یا `Conflict` تبدیل می‌شوند.
6. خطاهای غیرمنتظره log شده و با `repository_failure` برگردانده می‌شوند.

## انتقال خطا تا API

سه مسیر خطا همگی به ResultDto ختم می‌شوند:

- خطای Repository از طریق `ResultDtoException` در Service حفظ می‌شود.
- exceptionهای مدیریت‌نشده توسط `ResultDtoExceptionMiddleware` تبدیل می‌شوند.
- پاسخ‌های قدیمی Controller مانند `BadRequest`, `NotFound` و `ProblemDetails` توسط `ResultDtoErrorFilter` به ResultDto تبدیل می‌شوند.
- خطاهای ModelState با `Errors` و کد `model_validation_failed` برگردانده می‌شوند.

HTTP status از روی `ResultStatus` تعیین می‌شود و دیگر لازم نیست متن فارسی برای تشخیص نوع خطا parse شود.

## Soft Delete

تغییر این فاز Soft Delete فاز ۱۷ را حفظ می‌کند. Deleteهای SQL همچنان:

```csharp
IsRemoved = true;
RemoveTime = DateTimeOffset.UtcNow;
```

را ثبت می‌کنند و Global Query Filter رکوردهای حذف‌شده را از queryهای عادی کنار می‌گذارد.

## نمونه Repository

```csharp
public interface IProductRepository
{
    Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ResultDto<Product>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ResultDto<Product>> UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task<ResultDto> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
```

## استفاده در Service

```csharp
var repositoryResult = await products.GetByIdAsync(id, cancellationToken);
if (!repositoryResult.IsSuccess)
    return repositoryResult.ForwardFailure<ProductDto>();

var product = repositoryResult.Data!;
return result.Success("محصول دریافت شد.", Map(product));
```

در Serviceهای قدیمی که API خام آن‌ها برای backward compatibility حفظ شده، `RequireData()` و `EnsureSuccess()` همان خطا را با `ResultDtoException` منتقل می‌کنند و middleware آن را دوباره به ResultDto تبدیل می‌کند.

## تست‌های قرارداد

تست‌ها بررسی می‌کنند که:

- تمام Interfaceهای با پسوند `Repository` فقط `Task<ResultDto>` یا `Task<ResultDto<T>>` برگردانند.
- پیام، Status و ErrorCode Repository در Service حفظ شود.
- middleware وضعیت معنایی را به HTTP status درست تبدیل کند.
- `ProblemDetails` قدیمی به ResultDto تبدیل شود.
- خطاهای validation فیلدی از بین نروند.
- ResultDto موجود دوباره wrap نشود.
