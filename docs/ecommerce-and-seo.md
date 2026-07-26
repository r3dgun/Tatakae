# Ecommerce and SEO extension

## Domain model

- `Product`: public product entity and its canonical `Slug`.
- `ProductVariant`: sellable SKU with color, size, price, sale price and inventory.
- `ProductImage`, `ProductSpecification`: product content.
- `SeoMetadata`: editable title, description, canonical path, Open Graph image and robots directives.
- `Category`, `Customer`, `Address`, `Coupon`, `InventoryMovement`: ecommerce aggregate support models.

## Product URL and SEO

The indexed product page is `/product/{slug}`. It renders exactly one `h1`, breadcrumb navigation, descriptive copy, specifications, images with alt text, canonical link, robots meta, Open Graph tags and Schema.org Product / Offer JSON-LD.

## Admin routes

- `/admin`: dashboard
- `/admin/products`: product management
- `/admin/products/new`: create product
- `/admin/products/{id}`: edit product, variants and SEO metadata
- `/admin/categories`: category catalog overview
- `/admin/orders`: orders operation list
- `/admin/customers`: customer insight list calculated from orders
- `/admin/coupons`: promotion model and integration screen

## Production hardening

The supplied repository is intentionally in-memory. Replace it with EF Core persistence, enforce an Admin role/policy on `api/admin`, add file storage/CDN for product media, and calculate prices/coupons/inventory on the server before enabling real checkout.

## Admin coupon API

`api/admin/coupons` supports listing, create, update and delete operations for `Coupon`. Coupon redemption is deliberately not trusted to the browser; it must be applied and revalidated on the API during checkout.
