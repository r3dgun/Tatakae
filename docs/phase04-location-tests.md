# Phase 04 - Location Seed Tests

در این نسخه تست‌های استان/شهر اضافه شد تا Seed Data فقط یک لیست خام نباشد و هنگام تغییرات بعدی خراب نشود.

## فایل‌های تست

```text
tests/Tatakae.Application.Tests/IranLocationSeedTests.cs
tests/Tatakae.Api.Tests/LocationControllerTests.cs
```

## موارد تست شده

```text
- تعداد استان‌ها باید 31 باشد
- تهران، البرز، اصفهان، فارس، خراسان رضوی و آذربایجان شرقی باید وجود داشته باشند
- شهرهای اصلی مثل تهران، ری، کرج، مشهد، شیراز و اصفهان باید در دیتای Seed باشند
- هیچ استان یا شهر خالی نباشد
- داخل هر استان شهر تکراری نباشد
- کل شهرهای Seed حداقل 300 عدد باشد
- API /api/locations/provinces خروجی درست بدهد
- API /api/locations/cities?province=البرز فقط شهرهای البرز را بدهد
```
