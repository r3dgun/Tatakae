# Phase 18 Hotfix — Soft-delete-aware seeding

`StoreDataSeeder` now searches seed fixtures with `IgnoreQueryFilters()` before deciding to insert them.
This prevents a soft-deleted fixture from being inserted again with the same primary key.

## Repair behavior

- Existing active fixture: preserved.
- Existing soft-deleted fixture: restored and reused.
- Missing fixture: inserted.
- Missing product child (image, variant, specification, tag, embroidery policy option): inserted.
- Soft-deleted product child with the seed ID: restored instead of inserted.

The same behavior is applied to categories, products, customers/addresses, orders/lines/history, and product questions.

## Regression coverage

- Restoring soft-deleted product variants keeps the original IDs and does not create duplicate rows.
- Restoring a soft-deleted seed product reuses the existing database row.
