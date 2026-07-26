# Phase 08 - Embroidery Artwork Approval

این مرحله ساختار واقعی بررسی طرح‌های گلدوزی را اضافه می‌کند.

## قابلیت‌ها

- آپلود فایل طرح با فرمت‌های PNG, JPG, WEBP, SVG, PDF, DST, PES
- محدودیت حجم ۱۵ مگابایت
- ثبت درخواست بررسی طرح توسط مشتری
- اتصال طرح به مشتری، محصول، سفارش یا آیتم سفارش
- وضعیت‌های PendingReview, Approved, Rejected, NeedsRevision, Archived
- پنل مشتری برای مشاهده وضعیت طرح‌ها: `/account/artworks`
- پنل ادمین برای بررسی طرح‌ها: `/admin/artworks`
- APIهای مشتری و ادمین برای ثبت و moderation

## API مشتری

```text
GET  /api/account/artworks/policy
GET  /api/account/artworks
POST /api/account/artworks
```

## API ادمین

```text
GET   /api/admin/artworks
PATCH /api/admin/artworks/{id}/moderate
```

## جدول جدید

```text
EmbroideryArtworks
```

این جدول روی `MediaAssets` می‌نشیند و وضعیت تأیید/رد طرح را جدا از فایل خام نگه می‌دارد.
