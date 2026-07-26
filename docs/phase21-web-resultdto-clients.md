# Web API Clients and ResultDto

All HTTP-facing services in `Tatakae.Web/ApiClients` expose one consistent contract:

- Commands return `Task<ResultDto>`.
- Queries and commands with data return `Task<ResultDto<T>>`.
- API clients never return raw DTOs, `bool`, tuples, or `null` to represent failure.
- Persian messages, `ResultStatus`, `ErrorCode`, and field validation errors are preserved from API to UI.
- Network failures and timeouts are converted to `ResultDto` by `ApiClientTransport`.
- Multipart upload failures are converted to `ResultDto` by `FileUploadApiClient`.

Presentation-only state abstractions such as `IAuthSessionStore`, `ICartState`, and `IUiPermissionEvaluator` are intentionally not HTTP result contracts. They remain typed browser/UI state services and do not represent remote application operations.

Razor pages may inspect results directly for forms:

```csharp
var result = await Store.SubmitContactMessageAsync(request);
message = result.Message;
if (result.IsSuccess)
{
    // update UI state
}
```

Existing query-oriented pages can use the presentation helpers:

```csharp
var products = (await Store.GetProductsAsync(query)).RequireUiData();
var optionalProduct = (await Store.GetProductAsync(slug)).OptionalData();
(await Admin.DeleteProductAsync(id)).EnsureUiSuccess();
```

The helpers preserve the complete `ResultDto` inside `ApiClientException`, allowing the global Blazor error boundary to display the server message without losing status or validation details.
