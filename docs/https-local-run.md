# اجرای پروژه با HTTPS در لوکال

این نسخه برای اجرای HTTPS تنظیم شده است.

## آدرس‌ها

- API: `https://localhost:7075`
- Web: `https://localhost:7076`

فایل‌های تنظیم‌شده:

- `src/Tatakae.Api/Properties/launchSettings.json`
- `src/Tatakae.Web/Properties/launchSettings.json`
- `src/Tatakae.Web/wwwroot/appsettings.json`

## اعتماد به گواهی توسعه .NET

اگر مرورگر خطای certificate داد، یک‌بار این دستور را اجرا کنید:

```powershell
dotnet dev-certs https --trust
```

سپس Visual Studio را ببندید و دوباره باز کنید.

## اجرای دستی

```powershell
dotnet run --launch-profile https --project .\src\Tatakae.Api\Tatakae.Api.csproj
```

و در ترمینال دوم:

```powershell
dotnet run --launch-profile https --project .\src\Tatakae.Web\Tatakae.Web.csproj
```

## تنظیم Visual Studio

در Solution Properties → Startup Project، حالت Multiple Startup Projects را انتخاب کنید:

- `Tatakae.Api` = Start
- `Tatakae.Web` = Start

اگر Visual Studio بین پروفایل‌ها انتخاب می‌گذارد، برای هر دو پروژه پروفایل `https` را انتخاب کنید.
