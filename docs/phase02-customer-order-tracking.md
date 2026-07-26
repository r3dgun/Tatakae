# Phase 02 Customer Order Tracking

Customer order status is now visible inside `/account/orders` as a timeline.

Added:
- `GET /api/account/orders/{id}/tracking`
- ownership check by current user's mobile/customer id
- customer-facing timeline in `OrderCard`
- tracking code display
- mobile responsive vertical timeline

The admin workflow remains unchanged.
