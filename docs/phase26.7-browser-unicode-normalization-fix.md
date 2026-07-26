# Phase 26.7 — Browser Unicode normalization hotfix

## Problem

`SeoSlug.Normalize` used `NormalizationForm.FormKC`. Blazor WebAssembly throws
`Argument_UnsupportedNormalizationFormInBrowser` for compatibility normalization
forms, causing admin pages that render slug previews to fail with
`unexpected_ui_error`.

## Fix

- Replace compatibility normalization with a browser-safe shared pipeline.
- Convert full-width ASCII characters and ideographic spaces explicitly.
- Apply canonical composition (`FormC`) when available.
- Fall back to the mapped input when normalization data is unavailable.
- Add a regression case for full-width input and Persian digits.

The same slug algorithm now runs in both the API and Blazor WebAssembly.
