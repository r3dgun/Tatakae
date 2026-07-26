# Phase 22 — ResultDto Web Error Routing

`ResultDto.Status` is the source of truth from Application to API and Web.

- ValidationError -> 400 view with field errors
- Unauthorized -> 401 view with login and returnUrl
- Forbidden -> 403 access-denied view
- NotFound -> 404 not-found view
- Conflict -> 409 conflict view
- Failure -> 500/service-failure view

The API maps ResultStatus to HTTP status codes. `ApiResultReader` restores the semantic status from either a ResultDto body or the HTTP status. `ApiClientException` preserves Message, ErrorCode and Errors. `App.razor` delegates all unhandled API errors to `ResultStatusView`.

Resource pages may still use `OptionalData()` when they intentionally render an inline not-found state while keeping the requested URL (for example ProductDetail and Customizer).
