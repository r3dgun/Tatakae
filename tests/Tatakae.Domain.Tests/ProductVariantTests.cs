using Tatakae.Domain.Entities;

namespace Tatakae.Domain.Tests;

public sealed class ProductVariantTests
{
    [Fact]
    public void Constructor_NormalizesSkuAndCalculatesAvailability()
    {
        var variant = new ProductVariant(
            Guid.NewGuid(),
            " tt-tee-blk-m ",
            "M",
            "مشکی",
            "#111111",
            regularPrice: 900_000m,
            salePrice: 810_000m,
            stockQuantity: 10,
            reservedQuantity: 2,
            lowStockThreshold: 3);

        Assert.Equal("TT-TEE-BLK-M", variant.Sku);
        Assert.Equal(810_000m, variant.EffectivePrice);
        Assert.Equal(8, variant.AvailableQuantity);
        Assert.True(variant.IsInStock);
        Assert.False(variant.IsLowStock);
    }

    [Fact]
    public void Constructor_WhenSalePriceIsGreaterThanRegularPrice_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ProductVariant(
            Guid.NewGuid(),
            "TT-TEE-BLK-M",
            "M",
            "مشکی",
            "#111111",
            regularPrice: 900_000m,
            salePrice: 950_000m,
            stockQuantity: 10));

        Assert.Equal("salePrice", exception.ParamName);
    }

    [Fact]
    public void LowStock_WhenAvailableQuantityIsUnderThreshold_ReturnsTrue()
    {
        var variant = new ProductVariant(
            Guid.NewGuid(),
            "TT-TEE-BLK-M",
            "M",
            "مشکی",
            "#111111",
            regularPrice: 900_000m,
            salePrice: null,
            stockQuantity: 5,
            reservedQuantity: 3,
            lowStockThreshold: 3);

        Assert.Equal(2, variant.AvailableQuantity);
        Assert.True(variant.IsLowStock);
    }

    [Fact]
    public void AdjustStock_WhenDeltaWouldMakeStockNegative_Throws()
    {
        var variant = new ProductVariant(
            Guid.NewGuid(),
            "TT-TEE-BLK-M",
            "M",
            "مشکی",
            "#111111",
            regularPrice: 900_000m,
            salePrice: null,
            stockQuantity: 2);

        Assert.Throws<InvalidOperationException>(() => variant.AdjustStock(-3));
    }

    [Fact]
    public void InactiveVariant_IsNotInStockEvenWhenStockExists()
    {
        var variant = new ProductVariant(
            Guid.NewGuid(),
            "TT-TEE-BLK-M",
            "M",
            "مشکی",
            "#111111",
            regularPrice: 900_000m,
            salePrice: null,
            stockQuantity: 20,
            isActive: false);

        Assert.False(variant.IsInStock);
    }
}

public sealed class ProductVariantReservationTests
{
    [Fact]
    public void Constructor_WhenReservedQuantityExceedsStock_Throws()
        => Assert.Throws<ArgumentException>(() => new ProductVariant(Guid.NewGuid(), "SKU-1", "M", "مشکی", "#111111", 100m, null, 2, reservedQuantity: 3));

    [Fact]
    public void ReserveAndFulfill_KeepStockInvariant()
    {
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-1", "M", "مشکی", "#111111", 100m, null, 5);

        variant.Reserve(2);
        variant.FulfillReservation(2);

        Assert.Equal(3, variant.StockQuantity);
        Assert.Equal(0, variant.ReservedQuantity);
    }
}
