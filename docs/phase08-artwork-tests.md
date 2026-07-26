# Phase 08 Tests

تست‌های این مرحله:

```text
tests/Tatakae.Application.Tests/EmbroideryArtworkServiceTests.cs
tests/Tatakae.Api.Tests/ArtworkControllerRouteTests.cs
```

## موارد تست‌شده

- ثبت طرح معتبر با وضعیت `PendingReview`
- رد کردن تعداد رنگ بیش از حد مجاز
- جلوگیری از وضعیت `NeedsRevision` بدون دلیل
- تأیید طرح توسط ادمین و ثبت فرمت تولید
- وجود route و authorize برای API مشتری
- وجود route و permission checker برای API ادمین

## اجرا

```powershell
dotnet test .\Tatakae.sln
```
