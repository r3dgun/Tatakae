# Persistent Identity Login Review

این نسخه سیستم ورود را با ASP.NET Core Identity نگه می‌دارد، اما برای Blazor WebAssembly از JWT و localStorage استفاده می‌کند تا کاربر بعد از refresh یا بستن/باز کردن مرورگر وارد بماند.

## تغییرات اصلی

- `ApplicationUserIdentity` همچنان مدل اصلی Identity است.
- `LoginAudits` اضافه شد تا ورود موفق/ناموفق ثبت شود.
- توکن JWT دارای `sid` و `session_id` است.
- اگر کاربر گزینه «مرا در این مرورگر وارد نگه دار» را فعال کند، زمان ماندگاری توکن از `Jwt:RememberMeAccessTokenMinutes` خوانده می‌شود.
- نشست کاربر در `localStorage` با کلید `tatakae.identity.session.v1` ذخیره می‌شود.
- هنگام خروج، endpoint `/api/account/logout` فراخوانی می‌شود و رکورد لاگین `LogoutAt` می‌گیرد.
- در پنل ادمین `/admin/security` بخش «لاگ ورود کاربران» اضافه شد.

## مسیرهای مهم

- `POST /api/account/login`
- `GET /api/account/me`
- `POST /api/account/logout`
- `GET /api/admin/security/login-audits`

## صفحات عمومی

- `/rules` قوانین و مقررات
- `/terms` alias قوانین
- `/privacy` حریم خصوصی
- `/returns` مرجوعی
- `/shipping-policy` ارسال
- `/contact` ارتباط با ما

## Footer

لینک شبکه‌های اجتماعی در Footer اضافه شده‌اند. مقدارها placeholder هستند و قبل از انتشار باید با لینک واقعی فروشگاه جایگزین شوند.
