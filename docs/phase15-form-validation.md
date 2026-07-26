# Form validation hardening

This patch adds validation to all user-facing and administrative input flows.

## Validation layers

1. **Blazor forms** use `RecursiveDataAnnotationsValidator` so nested objects and list items are validated, including checkout addresses, product variants, SEO data and embroidery requests.
2. **Application contracts** use DataAnnotations and `IValidatableObject` for field and cross-field rules.
3. **API controllers** return RFC 7807 validation responses with a Persian title and field error dictionary when model binding fails.
4. **Manual controls** such as shop price filters, media uploads, inventory adjustments, review moderation and studio uploads validate before calling the API.
5. **Normalization** accepts Persian/Arabic digits for price, postal code and Iranian mobile inputs where the page normalizes values before submission.

## Main rules covered

- Iranian mobile and ten-digit postal code formats
- required fields and maximum lengths
- nested checkout address and cart data
- product SKU format, duplicate SKU, prices, stock and reserved quantity
- coupon percentage and date range
- shipping delivery-day range and free-shipping threshold
- embroidery dimensions, thread colors and conditional design source fields
- moderation reason/answer requirements
- upload type and 15 MB size limit
- SEO/legal title and description lengths

## Test

```powershell
dotnet clean
dotnet restore
dotnet build Tatakae.sln
dotnet test Tatakae.sln
```
