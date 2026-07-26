using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class InventoryService(
    IProductRepository products,
    ILogger<InventoryService>? logger = null) : IInventoryService
{
    private readonly ILogger<InventoryService> _logger = logger ?? NullLogger<InventoryService>.Instance;
    public async Task<IReadOnlyCollection<InventoryVariantDto>> GetInventoryAsync(CancellationToken cancellationToken = default)
        => (await products.GetAllAsync(cancellationToken)).RequireData()
            .OrderBy(x => x.Name)
            .SelectMany(product => product.Variants.OrderBy(v => v.Sku).Select(variant => new InventoryVariantDto(
                variant.Id,
                product.Id,
                product.Name,
                variant.Sku,
                variant.Size,
                variant.ColorName,
                variant.ColorHex,
                variant.StockQuantity,
                variant.ReservedQuantity,
                variant.AvailableQuantity,
                variant.IsLowStock,
                variant.IsActive)))
            .ToArray();

    public async Task<InventoryVariantDto> AdjustAsync(InventoryAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var allProducts = (await products.GetAllAsync(cancellationToken)).RequireData();
        var product = allProducts.SingleOrDefault(x => x.Variants.Any(v => v.Id == request.VariantId))
            ?? throw new KeyNotFoundException("SKU پیدا نشد.");
        var variant = product.Variants.Single(x => x.Id == request.VariantId);
        variant.AdjustStock(request.QuantityDelta);
        (await products.UpsertAsync(product, cancellationToken)).EnsureSuccess();

        return new InventoryVariantDto(
            variant.Id,
            product.Id,
            product.Name,
            variant.Sku,
            variant.Size,
            variant.ColorName,
            variant.ColorHex,
            variant.StockQuantity,
            variant.ReservedQuantity,
            variant.AvailableQuantity,
            variant.IsLowStock,
            variant.IsActive);
    }
}
