# Phase 21 — Tatakae.Web presentation boundary

`Tatakae.Web` is an outer presentation layer in Clean Architecture. Browser-specific
code remains in this project, while domain and application use cases remain in
`Tatakae.Application`.

## Final folders

- `ApiClients/Abstractions`: browser-to-API contracts used by Razor components.
- `ApiClients/Http`: HTTP adapter implementations.
- `ApiClients/Results`: one reader/transport for `ResultDto`, HTTP status mapping and Persian errors.
- `Authentication`: Blazor authentication state and bearer-token handler.
- `Authorization`: UI-only permission evaluation. API authorization remains authoritative.
- `State`: browser session and cart state backed by local storage.
- `Formatting`: presentation-only formatting helpers.

## Rules

1. Razor pages inject `I...ApiClient`, `IAuthSessionStore` and `ICartState`, never concrete classes.
2. API clients do not contain business rules. They only translate HTTP to application contracts.
3. API clients do not call `EnsureSuccessStatusCode`, return fabricated catalog data, or hide errors as `null`/`false` for server failures.
4. `ApiResultReader` preserves `ResultStatus`, `ErrorCode`, Persian `Message` and field-level `Errors`.
5. Authentication headers are attached in one `BearerTokenHandler`.
6. `StoreFallbackCatalog` is removed. Development data must come from the backend development seed.
7. `CartState` may calculate a display-only subtotal, but checkout totals, inventory, coupon and shipping rules remain server-side.
8. UI permission evaluation only controls presentation; API policies enforce real security.

The Web project references Application contracts only. It does not reference API or Infrastructure.
