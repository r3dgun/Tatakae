using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_WhenSkuIsDuplicated_Throws()
    {
        var variants = new[] { Variant("SKU-1"), Variant("sku-1") };

        var error = Assert.Throws<ArgumentException>(() => Create(variants: variants));

        Assert.Contains("SKU تکراری", error.Message);
    }

    [Fact]
    public void Create_WhenThereIsNotExactlyOnePrimaryImage_Throws()
    {
        var images = new[]
        {
            new ProductImage(Guid.NewGuid(), "/a.jpg", "تصویر اول", false, 0),
            new ProductImage(Guid.NewGuid(), "/b.jpg", "تصویر دوم", false, 1)
        };

        Assert.Throws<ArgumentException>(() => Create(images: images));
    }

    [Fact]
    public void Create_UsesExplicitTimestamp()
    {
        var timestamp = new DateTimeOffset(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);

        var product = Create(createdAt: timestamp);

        Assert.Equal(timestamp, product.CreatedAt);
        Assert.Equal(timestamp, product.UpdatedAt);
    }

    private static Product Create(
        IReadOnlyCollection<ProductImage>? images = null,
        IReadOnlyCollection<ProductVariant>? variants = null,
        DateTimeOffset? createdAt = null)
        => Product.Create(
            Guid.NewGuid(),
            "تی‌شرت تست",
            "test-tshirt",
            ApparelCategory.TShirt,
            Guid.NewGuid(),
            "توضیح کوتاه محصول",
            "توضیحات کامل محصول برای تست دامنه.",
            "پنبه",
            "Regular",
            "شستشو با آب سرد",
            null,
            new SeoMetadata("تی‌شرت تست", "توضیحات سئوی محصول تست"),
            new EmbroideryPolicy(0, 0, 0, 4, 12, 12, [EmbroideryPlacement.LeftChest], ["#111111"]),
            images ?? [new ProductImage(Guid.NewGuid(), "/p.jpg", "تصویر محصول", true, 0)],
            variants ?? [Variant("SKU-1")],
            [],
            ["test"],
            true,
            false,
            true,
            createdAt ?? DateTimeOffset.UnixEpoch);

    private static ProductVariant Variant(string sku)
        => new(Guid.NewGuid(), sku, "M", "مشکی", "#111111", 100m, null, 5);
}
