# اجرای همزمان API و Web در Visual Studio

در این نسخه تنظیم `launchBrowser` برای پروژه Web فعال شد. بنابراین وقتی Solution را با Multiple Startup Projects اجرا می‌کنید، مرورگر باید روی آدرس زیر باز شود:

```text
http://localhost:5076
```

API همچنان روی آدرس زیر اجرا می‌شود و مرورگر را باز نمی‌کند:

```text
http://localhost:5075/health
```

اگر Visual Studio مرورگر را باز نکرد، دستی همین آدرس Web را باز کنید.

## تنظیم پیشنهادی در Visual Studio

Solution → Properties → Startup Project → Multiple startup projects

```text
Tatakae.Api  = Start
Tatakae.Web  = Start
```

## نکته

لاگ اجرای موفق باید این دو خط را نشان دهد:

```text
Now listening on: http://localhost:5075
Now listening on: http://localhost:5076
```
