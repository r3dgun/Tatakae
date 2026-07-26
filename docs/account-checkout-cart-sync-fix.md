# Account, checkout auth, and cart sync fix

This version tightens the customer account and checkout flow:

- Checkout UI is hidden until the customer is signed in.
- The `/api/checkout` endpoint is protected with JWT `[Authorize]` and uses the authenticated customer claims for name/mobile/email.
- Identity login now guarantees an attached `Customers` row and returns a real `CustomerId` in `AccountSessionDto`.
- Guest cart data is persisted in browser storage.
- When a guest signs in or registers, the guest cart is merged into the account cart locally and synced to `/api/cart/merge`.
- The account page now shows profile details, session expiry, roles, permissions, order stats, and cart summary.
- `/account/orders` now loads only the current customer's orders from `/api/account/orders`.

Important routes:

- `/account`
- `/account/orders`
- `/checkout`
- `/login?returnUrl=/checkout`
- `/register?returnUrl=/checkout`

API routes:

- `GET /api/account/me`
- `GET /api/account/orders`
- `POST /api/cart/merge`
- `DELETE /api/cart`
- `POST /api/checkout` requires authorization.
