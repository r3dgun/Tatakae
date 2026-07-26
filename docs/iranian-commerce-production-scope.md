# Iranian Ecommerce Production Scope

This version intentionally keeps real payment gateway and real OTP authentication out of scope for now.

Implemented in this package:

- Manual shipping methods defined by the store admin.
- Customer-selectable shipping method in checkout.
- Shipping method price calculation with optional free-shipping threshold.
- Legal/trust pages: terms, privacy, returns, shipping, contact and about.
- Admin pages for shipping methods and media library.
- Real multipart file upload endpoint for product images, banners and embroidery artwork.
- MediaAsset Code First table and repository.
- Security baseline: security headers, restricted CORS origins, upload validation and package vulnerability mitigation.

Important SQL note:

If you already created the old LocalDB database, rename the database in `src/Tatakae.Api/appsettings.json` or drop the old DB, because Code First `EnsureCreated` will not alter an existing schema.

Recommended local database name for this version:

```json
"Database=TatakaeEmbroideryCommerce_IranianProductionV1;"
```

Run API first, then Web.
