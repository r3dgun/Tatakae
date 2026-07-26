# Shop, Filter, About and Terms Fix

This patch fixes the shop listing and filter flow for the Iranian ecommerce version.

## Fixed

- Added real query fields to `ProductListQuery`: size, color, min price, max price, in-stock-only and featured-only.
- Added `/api/products/filters` endpoint.
- Added `CatalogService.GetFiltersAsync` to calculate available categories, sizes, colors, price range and tags from real products.
- Rebuilt `/shop` so sidebar filters actually call the API and refresh the listing.
- Added defensive catalog seeding: if the database is new or catalog seed data is missing/corrupted, seed categories and products are restored.
- Reworked `/pages/about` and `/pages/terms` with richer Persian content for a real Iranian embroidery store.
- Added direct routes: `/about`, `/terms`, `/privacy`, `/returns`, `/shipping-policy`, `/contact`.

## Important database note

If your old database was created before this patch and shop still shows no product, either delete the old LocalDB database or change the database name in:

`src/Tatakae.Api/appsettings.json`

Example:

```json
"Database=TatakaeEmbroideryCommerce_IdentityShopFixV1;"
```

Then run API again so `EnsureCreated` and seeding can run fresh.
