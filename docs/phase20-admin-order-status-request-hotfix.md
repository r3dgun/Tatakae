# Phase 20 AdminOrderStatusRequest build hotfix

`AdminOrderStatusRequest` is declared in `Tatakae.Application.Contracts.Admin`.
`AdminOrdersController` now imports that namespace explicitly.

The missing `Tatakae.Api.dll` metadata error is a downstream build error and should disappear after the API project compiles.
