# Phase 21 Web build hotfix

- Legal page URL formatting moved from the concrete `SeoService` class to the shared `SeoSlug.LegalPagePath` utility.
- `Tatakae.Web` no longer references `SeoService` directly.
- The WebAssembly `HttpClient` registration now uses an explicitly typed factory method (`CreateApiHttpClient`) to avoid delegate inference errors.
- Architecture tests prevent concrete SEO service references from returning to the Web presentation layer.
