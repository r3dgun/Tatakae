# Phase 16 — ResultDto application service interfaces

The injectable services in `Tatakae.Application` now expose Result-based contracts under:

```text
Tatakae.Application.Interfaces.Services
```

Every interface method returns either `ResultDto` or `ResultDto<T>`. Existing concrete APIs remain available for backward compatibility, while interface calls use explicit implementations with:

- input guards;
- Persian success/failure messages;
- exception handling;
- structured `ILogger` logging;
- null/not-found conversion to failed results.

## Usage

Inject the interface rather than the concrete service when the caller needs the standardized response:

```csharp
public sealed class SampleController(IAdminCatalogService products) : ControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AdminProductRequest request, CancellationToken cancellationToken)
    {
        var result = await products.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

The old `AdminCatalogService.UpdateAsync` still returns `ProductDetailDto`; casting/injecting `IAdminCatalogService` selects the Result-based implementation.

## Registered contracts

- `IAccountService`
- `IAdminCatalogService`
- `IAdminCategoryService`
- `IAdminCouponService`
- `IAdminDashboardService`
- `ICatalogService`
- `ICouponService`
- `ICustomerService`
- `IEmbroideryArtworkService`
- `IEmbroideryPricingService`
- `IInventoryService`
- `IMediaAssetService`
- `INotificationService`
- `IOrderService`
- `IProductEngagementService`
- `ISeoService`
- `IShippingService`
- `IWishlistService`

The static `ProductRecommendationEngine` remains a stateless calculation utility and is not registered as an injectable service.
