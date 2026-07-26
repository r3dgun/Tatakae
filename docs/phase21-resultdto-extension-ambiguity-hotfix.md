# Phase 21 ResultDto extension ambiguity hotfix

`Tatakae.Application` and `Tatakae.Web` previously exposed identical extension method names for `ResultDto`:

- `RequireData`
- `EnsureSuccess`

Because Razor imports both namespaces, every call was ambiguous. Application keeps the generic use-case helpers, while Web now exposes presentation-specific names:

- `RequireUiData`
- `EnsureUiSuccess`
- `OptionalData`

The Web helpers throw `ApiClientException` so the Blazor error boundary can preserve the Persian message, `ResultStatus`, `ErrorCode`, and field errors. Application helpers continue to throw `ResultDtoException`.
