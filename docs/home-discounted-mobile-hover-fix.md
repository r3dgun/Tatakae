# Home discounted section mobile and hover fix

This patch only changes the home editorial interaction layer:

- Discounted product grid becomes one clean column on mobile.
- `featured-card--lower` stagger spacing is disabled for the discounted section on small screens.
- Sale quick overlay is visible on touch devices because hover does not exist on mobile.
- The main opening campaign receives pointer parallax for `.campaign-depth` elements.
- Campaign shots and hero image portals now have touch/active behavior so the hover-like reveal works on phones.
- No checkout, account, Identity, product, cart, or admin logic was changed.
