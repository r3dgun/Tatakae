using Tatakae.Application.Contracts.Inventory;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task GetInventoryAsync_ReturnsAvailableAndLowStockValues()
    {
        var variant = Variant(stock: 5, reserved: 3, threshold: 3);
        var product = ProductWith(variant);
        var repository = new FakeProductRepository(product);
        var service = new InventoryService(repository);

        var result = await service.GetInventoryAsync();
        var item = Assert.Single(result);

        Assert.Equal(2, item.AvailableQuantity);
        Assert.True(item.IsLowStock);
        Assert.Equal("TT-TEE-BLK-M", item.Sku);
    }

    [Fact]
    public async Task AdjustAsync_UpdatesStockAndPersistsProduct()
    {
        var variant = Variant(stock: 5, reserved: 1, threshold: 3);
        var product = ProductWith(variant);
        var repository = new FakeProductRepository(product);
        var service = new InventoryService(repository);

        var result = await service.AdjustAsync(new InventoryAdjustmentRequest
        {
            VariantId = variant.Id,
            QuantityDelta = 4,
            Reason = "Restock",
            Note = "شارژ انبار"
        });

        Assert.Equal(9, result.StockQuantity);
        Assert.Equal(8, result.AvailableQuantity);
        Assert.Equal(1, repository.UpsertCount);
    }

    [Fact]
    public async Task AdjustAsync_WhenVariantDoesNotExist_Throws()
    {
        var repository = new FakeProductRepository(ProductWith(Variant()));
        var service = new InventoryService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AdjustAsync(new InventoryAdjustmentRequest
        {
            VariantId = Guid.NewGuid(),
            QuantityDelta = 1,
            Reason = "ManualCorrection"
        }));
    }

    private static ProductVariant Variant(int stock = 10, int reserved = 0, int threshold = 3) => new(
        Guid.NewGuid(),
        "TT-TEE-BLK-M",
        "M",
        "مشکی",
        "#111111",
        regularPrice: 900_000m,
        salePrice: 810_000m,
        stockQuantity: stock,
        reservedQuantity: reserved,
        lowStockThreshold: threshold);

    private static Product ProductWith(ProductVariant variant) => Product.Create(
        Guid.NewGuid(),
        "تی‌شرت گلدوزی",
        "embroidered-tshirt",
        ApparelCategory.TShirt,
        Guid.NewGuid(),
        "توضیح کوتاه",
        "توضیح کامل محصول",
        "پنبه",
        "Regular",
        "شستشو با آب سرد",
        "https://example.com/size-guide",
        new SeoMetadata("تی‌شرت گلدوزی", "خرید تی‌شرت گلدوزی"),
        new EmbroideryPolicy(
            BasePrice: 100_000m,
            PerThreadColorPrice: 20_000m,
            PerSquareCentimeterPrice: 5_000m,
            MaxThreadColors: 6,
            MaxWidthCm: 20m,
            MaxHeightCm: 20m,
            AllowedPlacements: new[] { EmbroideryPlacement.LeftChest },
            AllowedThreadColors: new[] { "#111111", "#FFFFFF" }),
        new[] { new ProductImage(Guid.NewGuid(), "https://example.com/product.jpg", "تی‌شرت", true, 1) },
        new[] { variant },
        Array.Empty<ProductSpecification>(),
        new[] { "گلدوزی" },
        true,
        false,
        true,
        DateTimeOffset.UnixEpoch);

    private sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        private readonly List<Product> _products = products.ToList();

        public int UpsertCount { get; private set; }

        public Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Product>>().Success("محصولات دریافت شدند.", _products));

        public Task<ResultDto<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = _products.SingleOrDefault(x => x.Id == id);
            var result = new ResultDto<Product>();
            return Task.FromResult(item is null ? result.NotFound("محصول پیدا نشد.") : result.Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = _products.SingleOrDefault(x => x.Slug == slug);
            var result = new ResultDto<Product>();
            return Task.FromResult(item is null ? result.NotFound("محصول پیدا نشد.") : result.Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> UpsertAsync(Product product, CancellationToken cancellationToken = default)
        {
            UpsertCount++;
            return Task.FromResult(new ResultDto<Product>().Success("محصول ذخیره شد.", product));
        }

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _products.RemoveAll(x => x.Id == id);
            return Task.FromResult(new ResultDto().Success("محصول حذف شد."));
        }
    }
}
