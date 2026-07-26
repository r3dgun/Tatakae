using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Products;

namespace Tatakae.Web.Models;

/// <summary>Client-side cart snapshot. Server validates all price, inventory and embroidery rules at checkout.</summary>
public sealed class CartLine
{
    public required string Key { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid VariantId { get; init; }
    public required string ProductName { get; init; }
    public required string ProductSlug { get; init; }
    public required string ProductImageUrl { get; init; }
    public required string Sku { get; init; }
    public required string Size { get; init; }
    public required string ColorName { get; init; }
    public required string ColorHex { get; init; }
    public decimal GarmentUnitPrice { get; init; }
    public required EmbroideryCustomizationRequest Embroidery { get; init; }
    public decimal EmbroideryUnitPrice { get; init; }
    public bool SupportsEmbroidery { get; init; } = true;
    public bool IsReadyMade => !SupportsEmbroidery;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice => GarmentUnitPrice + EmbroideryUnitPrice;
    public decimal LineTotal => UnitPrice * Quantity;

    public static CartLine From(ProductDetailDto product, ProductVariantDto variant, EmbroideryCustomizationRequest embroidery, decimal embroideryPrice) => new()
    {
        Key = Guid.NewGuid().ToString("N"),
        ProductId = product.Id,
        VariantId = variant.Id,
        ProductName = product.Name,
        ProductSlug = product.Slug,
        ProductImageUrl = product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).First().Url,
        Sku = variant.Sku,
        Size = variant.Size,
        ColorName = variant.ColorName,
        ColorHex = variant.ColorHex,
        GarmentUnitPrice = variant.EffectivePrice,
        Embroidery = Clone(embroidery),
        EmbroideryUnitPrice = embroideryPrice,
        SupportsEmbroidery = product.SupportsEmbroidery
    };

    public static CartLine FromReadyMade(ProductDetailDto product, ProductVariantDto variant) => new()
    {
        Key = Guid.NewGuid().ToString("N"),
        ProductId = product.Id,
        VariantId = variant.Id,
        ProductName = product.Name,
        ProductSlug = product.Slug,
        ProductImageUrl = product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).First().Url,
        Sku = variant.Sku,
        Size = variant.Size,
        ColorName = variant.ColorName,
        ColorHex = variant.ColorHex,
        GarmentUnitPrice = variant.EffectivePrice,
        Embroidery = new EmbroideryCustomizationRequest
        {
            ProductId = product.Id,
            VariantId = variant.Id,
            GarmentType = CheckoutGarmentType(product.ApparelCategory),
            GarmentSize = variant.Size,
            GarmentColorHex = variant.ColorHex,
            Placement = "CenterChest",
            WidthCm = 1m,
            HeightCm = 1m,
            ThreadColorCount = 1,
            ThreadColorHexes = ["#111111"],
            DesignSource = "Motif",
            MotifKey = "custom",
            Note = "READY_MADE_PRODUCT"
        },
        EmbroideryUnitPrice = 0m,
        SupportsEmbroidery = false
    };

    private static string CheckoutGarmentType(string apparelCategory) => apparelCategory switch
    {
        "Hoodie" => "Hoodie",
        "Sweatshirt" => "Sweatshirt",
        "Crewneck" => "Crewneck",
        _ => "TShirt"
    };

    private static EmbroideryCustomizationRequest Clone(EmbroideryCustomizationRequest source) => new()
    {
        ProductId = source.ProductId,
        VariantId = source.VariantId,
        GarmentType = source.GarmentType,
        GarmentSize = source.GarmentSize,
        GarmentColorHex = source.GarmentColorHex,
        Placement = source.Placement,
        WidthCm = source.WidthCm,
        HeightCm = source.HeightCm,
        ThreadColorCount = source.ThreadColorCount,
        ThreadColorHexes = source.ThreadColorHexes.ToList(),
        DesignSource = source.DesignSource,
        MotifKey = source.MotifKey,
        ArtworkFileUrl = source.ArtworkFileUrl,
        ArtworkFileName = source.ArtworkFileName,
        Text = source.Text,
        FontName = source.FontName,
        PositionX = source.PositionX,
        PositionY = source.PositionY,
        ScalePercent = source.ScalePercent,
        RotationDegrees = source.RotationDegrees,
        OpacityPercent = source.OpacityPercent,
        Note = source.Note
    };
}
