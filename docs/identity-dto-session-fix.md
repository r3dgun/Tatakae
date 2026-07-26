# Identity DTO/session compile fix

This fix synchronizes the login DTOs used by the API and Blazor WebAssembly client.

Changes:
- Added `RememberMe` to `LoginRequest`.
- Added `SessionKey` to `AccountSessionDto` while keeping the old constructor shape compatible through optional parameters.
- Added `RememberMe` to `AccountSessionDto` so the client can keep the session metadata.
- Updated `IdentityAuthService.CreateSessionAsync` to return the session key and remember-me flag.
- Updated `/api/account/me` flow to preserve the current session key instead of issuing an unrelated new session id.
