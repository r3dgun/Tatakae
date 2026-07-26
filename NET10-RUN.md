# اجرای نسخه .NET 10

این نسخه برای .NET 10 تنظیم شده است.

## پیش‌نیاز

- .NET SDK 10.x، ترجیحاً همان نسخه‌ای که نصب کرده‌اید: `10.0.301`

بررسی نصب:

```bash
dotnet --list-sdks
dotnet --version
```

## اجرا

از ریشه پروژه:

```bash
dotnet restore
dotnet build
```

ترمینال اول:

```bash
cd src/Tatakae.Api
dotnet run
```

ترمینال دوم:

```bash
cd src/Tatakae.Web
dotnet run
```

API: `http://localhost:5075`

Web: `http://localhost:5076`
