using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Infrastructure.Seeding;

namespace Tatakae.Infrastructure.Persistence.Mappers;

internal static class SqlDomainMapper
{
    public static Category ToDomain(this CategoryDbRecord source) => new(
        source.Id,
        source.Name,
        source.Slug,
        source.Description,
        source.CoverImageUrl,
        new SeoMetadata(source.SeoMetaTitle, source.SeoMetaDescription, source.SeoCanonicalPath, source.SeoOpenGraphImageUrl, source.SeoAllowIndex, source.SeoAllowFollow),
        source.ParentId,
        source.SortOrder,
        source.IsActive);

    public static CategoryDbRecord ToRecord(this Category source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Slug = source.Slug,
        Description = source.Description,
        CoverImageUrl = source.CoverImageUrl,
        SeoMetaTitle = source.Seo.MetaTitle,
        SeoMetaDescription = source.Seo.MetaDescription,
        SeoCanonicalPath = source.Seo.CanonicalPath,
        SeoOpenGraphImageUrl = source.Seo.OpenGraphImageUrl,
        SeoAllowIndex = source.Seo.AllowIndex,
        SeoAllowFollow = source.Seo.AllowFollow,
        ParentId = source.ParentId,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive
    };

    public static Product ToDomain(this ProductDbRecord source)
    {
        var policyRecord = source.EmbroideryPolicy ?? throw new InvalidOperationException($"Product {source.Id} has no embroidery policy.");
        var policy = new EmbroideryPolicy(
            policyRecord.BasePrice,
            policyRecord.PerThreadColorPrice,
            policyRecord.PerSquareCentimeterPrice,
            policyRecord.MaxThreadColors,
            policyRecord.MaxWidthCm,
            policyRecord.MaxHeightCm,
            policyRecord.AllowedPlacements.OrderBy(x => x.Placement).Select(x => x.Placement).ToArray(),
            policyRecord.AllowedThreadColors.OrderBy(x => x.ColorHex).Select(x => x.ColorHex).ToArray(),
            policyRecord.AllowArtworkUpload,
            policyRecord.AllowTextEmbroidery);

        return Product.Rehydrate(
            source.Id,
            source.Name,
            source.Slug,
            source.ApparelCategory,
            source.CategoryId,
            source.ShortDescription,
            source.Description,
            source.Material,
            source.Fit,
            source.CareGuide,
            source.SizeGuideUrl,
            new SeoMetadata(source.SeoMetaTitle, source.SeoMetaDescription, source.SeoCanonicalPath, source.SeoOpenGraphImageUrl, source.SeoAllowIndex, source.SeoAllowFollow),
            policy,
            source.Images.OrderBy(x => x.SortOrder).Select(x => new ProductImage(x.Id, x.Url, x.AltText, x.IsPrimary, x.SortOrder)).ToArray(),
            source.Variants.OrderBy(x => x.Sku).Select(x => new ProductVariant(x.Id, x.Sku, x.Size, x.ColorName, x.ColorHex, x.RegularPrice, x.SalePrice, x.StockQuantity, x.IsActive, x.ReservedQuantity, x.LowStockThreshold, x.ImageUrl, x.Barcode)).ToArray(),
            source.Specifications.OrderBy(x => x.SortOrder).Select(x => new ProductSpecification(x.Name, x.Value, x.SortOrder)).ToArray(),
            source.Tags.OrderBy(x => x.Value).Select(x => x.Value).ToArray(),
            source.IsPublished,
            source.IsFeatured,
            source.SupportsEmbroidery,
            source.CreatedAt,
            source.UpdatedAt);
    }

    public static ProductDbRecord ToRecord(this Product source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Slug = source.Slug,
        ApparelCategory = source.ApparelCategory,
        CategoryId = source.CategoryId,
        ShortDescription = source.ShortDescription,
        Description = source.Description,
        Material = source.Material,
        Fit = source.Fit,
        CareGuide = source.CareGuide,
        SizeGuideUrl = source.SizeGuideUrl,
        SeoMetaTitle = source.Seo.MetaTitle,
        SeoMetaDescription = source.Seo.MetaDescription,
        SeoCanonicalPath = source.Seo.CanonicalPath,
        SeoOpenGraphImageUrl = source.Seo.OpenGraphImageUrl,
        SeoAllowIndex = source.Seo.AllowIndex,
        SeoAllowFollow = source.Seo.AllowFollow,
        IsPublished = source.IsPublished,
        IsFeatured = source.IsFeatured,
        SupportsEmbroidery = source.SupportsEmbroidery,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        Images = source.Images.Select(x => new ProductImageDbRecord { Id = x.Id, ProductId = source.Id, Url = x.Url, AltText = x.AltText, IsPrimary = x.IsPrimary, SortOrder = x.SortOrder }).ToList(),
        Variants = source.Variants.Select(x => new ProductVariantDbRecord { Id = x.Id, ProductId = source.Id, Sku = x.Sku, Size = x.Size, ColorName = x.ColorName, ColorHex = x.ColorHex, RegularPrice = x.RegularPrice, SalePrice = x.SalePrice, StockQuantity = x.StockQuantity, ReservedQuantity = x.ReservedQuantity, LowStockThreshold = x.LowStockThreshold, ImageUrl = x.ImageUrl, Barcode = x.Barcode, IsActive = x.IsActive }).ToList(),
        Specifications = source.Specifications.Select(x => new ProductSpecificationDbRecord { Id = SeedIds.From($"product-spec:{source.Id}:{x.SortOrder}:{x.Name}"), ProductId = source.Id, Name = x.Name, Value = x.Value, SortOrder = x.SortOrder }).ToList(),
        Tags = source.Tags.Select(x => new ProductTagDbRecord { Id = SeedIds.From($"product-tag:{source.Id}:{x}"), ProductId = source.Id, Value = x }).ToList(),
        EmbroideryPolicy = new ProductEmbroideryPolicyDbRecord
        {
            Id = SeedIds.From($"product-policy:{source.Id}"),
            ProductId = source.Id,
            BasePrice = source.EmbroideryPolicy.BasePrice,
            PerThreadColorPrice = source.EmbroideryPolicy.PerThreadColorPrice,
            PerSquareCentimeterPrice = source.EmbroideryPolicy.PerSquareCentimeterPrice,
            MaxThreadColors = source.EmbroideryPolicy.MaxThreadColors,
            MaxWidthCm = source.EmbroideryPolicy.MaxWidthCm,
            MaxHeightCm = source.EmbroideryPolicy.MaxHeightCm,
            AllowArtworkUpload = source.EmbroideryPolicy.AllowArtworkUpload,
            AllowTextEmbroidery = source.EmbroideryPolicy.AllowTextEmbroidery,
            AllowedPlacements = source.EmbroideryPolicy.AllowedPlacements.Select(x => new ProductAllowedPlacementDbRecord { Id = SeedIds.From($"product-placement:{source.Id}:{x}"), Placement = x }).ToList(),
            AllowedThreadColors = source.EmbroideryPolicy.AllowedThreadColors.Select(x => new ProductAllowedThreadColorDbRecord { Id = SeedIds.From($"product-thread:{source.Id}:{x}"), ColorHex = x }).ToList()
        }
    };

    public static Coupon ToDomain(this CouponDbRecord source) => Coupon.Rehydrate(source.Id, source.Code, source.Type, source.Value, source.StartsAt, source.EndsAt, source.UsageLimit, source.UsageCount, source.MinimumOrderAmount, source.IsActive);

    public static CouponDbRecord ToRecord(this Coupon source) => new()
    {
        Id = source.Id,
        Code = source.Code,
        Type = source.Type,
        Value = source.Value,
        StartsAt = source.StartsAt,
        EndsAt = source.EndsAt,
        UsageLimit = source.UsageLimit,
        UsageCount = source.UsageCount,
        MinimumOrderAmount = source.MinimumOrderAmount,
        IsActive = source.IsActive
    };

    public static Customer ToDomain(this CustomerDbRecord source) => Customer.Rehydrate(
        source.Id,
        source.FullName,
        source.Mobile,
        source.Email,
        source.CreatedAt,
        source.Addresses.OrderByDescending(x => x.IsDefault).Select(ToDomainAddress).ToArray());

    public static CustomerDbRecord ToRecord(this Customer source) => new()
    {
        Id = source.Id,
        FullName = source.FullName,
        Mobile = source.Mobile,
        Email = source.Email,
        CreatedAt = source.CreatedAt,
        Addresses = source.Addresses.Select(x => new CustomerAddressDbRecord
        {
            Id = x.Id,
            CustomerId = source.Id,
            RecipientName = x.RecipientName,
            Mobile = x.Mobile,
            Province = x.Province,
            City = x.City,
            PostalCode = x.PostalCode,
            AddressLine = x.AddressLine,
            Plaque = x.Plaque,
            Unit = x.Unit,
            IsDefault = x.IsDefault
        }).ToList()
    };

    public static Order ToDomain(this OrderDbRecord source)
    {
        var address = new Address(Guid.NewGuid(), source.ShippingRecipientName, source.ShippingMobile, source.ShippingProvince, source.ShippingCity, source.ShippingPostalCode, source.ShippingAddressLine, source.ShippingPlaque, source.ShippingUnit, true);
        var lines = source.Lines.OrderBy(x => x.Id).Select(ToDomainLine).ToArray();
        return Order.Rehydrate(source.Id, source.OrderNumber, source.CustomerId, source.CustomerName, source.CustomerMobile, address, lines, source.ShippingAmount, source.DiscountAmount, source.ShippingMethodCode, source.ShippingMethodTitle, source.CreatedAt, source.Status, source.PaymentStatus, source.Subtotal, source.Total, source.TrackingCode, source.AdminNote);
    }

    public static OrderDbRecord ToRecord(this Order source) => new()
    {
        Id = source.Id,
        OrderNumber = source.OrderNumber,
        CustomerId = source.CustomerId,
        CustomerName = source.CustomerName,
        CustomerMobile = source.CustomerMobile,
        ShippingRecipientName = source.ShippingAddress.RecipientName,
        ShippingMobile = source.ShippingAddress.Mobile,
        ShippingProvince = source.ShippingAddress.Province,
        ShippingCity = source.ShippingAddress.City,
        ShippingPostalCode = source.ShippingAddress.PostalCode,
        ShippingAddressLine = source.ShippingAddress.AddressLine,
        ShippingPlaque = source.ShippingAddress.Plaque,
        ShippingUnit = source.ShippingAddress.Unit,
        CreatedAt = source.CreatedAt,
        Status = source.Status,
        PaymentStatus = source.PaymentStatus,
        Subtotal = source.Subtotal,
        ShippingAmount = source.ShippingAmount,
        ShippingMethodCode = source.ShippingMethodCode,
        ShippingMethodTitle = source.ShippingMethodTitle,
        DiscountAmount = source.DiscountAmount,
        Total = source.Total,
        TrackingCode = source.TrackingCode,
        AdminNote = source.AdminNote,
        Lines = source.Lines.Select(ToRecordLine).ToList()
    };

    private static OrderLine ToDomainLine(OrderLineDbRecord source)
    {
        var threadColors = source.EmbroideryThreadColorHexesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var embroidery = new EmbroideryConfiguration(
            source.EmbroideryId,
            source.EmbroideryPlacement,
            source.EmbroideryWidthCm,
            source.EmbroideryHeightCm,
            source.EmbroideryThreadColorCount,
            threadColors,
            source.EmbroideryArtworkFileUrl,
            source.EmbroideryArtworkFileName,
            source.EmbroideryText,
            source.EmbroideryFontName,
            source.EmbroideryNote,
            source.EmbroideryCalculatedPrice,
            source.EmbroideryGarmentType,
            source.EmbroideryGarmentSize,
            source.EmbroideryGarmentColorHex,
            source.EmbroideryDesignSource,
            source.EmbroideryMotifKey,
            source.EmbroideryPositionX,
            source.EmbroideryPositionY,
            source.EmbroideryScalePercent,
            source.EmbroideryRotationDegrees,
            source.EmbroideryOpacityPercent);

        return new OrderLine(source.ProductId, source.VariantId, source.ProductName, source.ProductSlug, source.ProductImageUrl, source.Sku, source.Size, source.ColorName, source.ColorHex, source.Quantity, source.UnitGarmentPrice, embroidery);
    }

    private static OrderLineDbRecord ToRecordLine(OrderLine source) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = source.ProductId,
        VariantId = source.VariantId,
        ProductName = source.ProductName,
        ProductSlug = source.ProductSlug,
        ProductImageUrl = source.ProductImageUrl,
        Sku = source.Sku,
        Size = source.Size,
        ColorName = source.ColorName,
        ColorHex = source.ColorHex,
        Quantity = source.Quantity,
        UnitGarmentPrice = source.UnitGarmentPrice,
        EmbroideryId = source.Embroidery.Id,
        EmbroideryPlacement = source.Embroidery.Placement,
        EmbroideryWidthCm = source.Embroidery.WidthCm,
        EmbroideryHeightCm = source.Embroidery.HeightCm,
        EmbroideryThreadColorCount = source.Embroidery.ThreadColorCount,
        EmbroideryThreadColorHexesCsv = string.Join(',', source.Embroidery.ThreadColorHexes),
        EmbroideryArtworkFileUrl = source.Embroidery.ArtworkFileUrl,
        EmbroideryArtworkFileName = source.Embroidery.ArtworkFileName,
        EmbroideryText = source.Embroidery.Text,
        EmbroideryFontName = source.Embroidery.FontName,
        EmbroideryNote = source.Embroidery.Note,
        EmbroideryCalculatedPrice = source.Embroidery.CalculatedPrice,
        EmbroideryGarmentType = source.Embroidery.GarmentType,
        EmbroideryGarmentSize = source.Embroidery.GarmentSize,
        EmbroideryGarmentColorHex = source.Embroidery.GarmentColorHex,
        EmbroideryDesignSource = source.Embroidery.DesignSource,
        EmbroideryMotifKey = source.Embroidery.MotifKey,
        EmbroideryPositionX = source.Embroidery.PositionX,
        EmbroideryPositionY = source.Embroidery.PositionY,
        EmbroideryScalePercent = source.Embroidery.ScalePercent,
        EmbroideryRotationDegrees = source.Embroidery.RotationDegrees,
        EmbroideryOpacityPercent = source.Embroidery.OpacityPercent
    };

    private static Address ToDomainAddress(CustomerAddressDbRecord source) => new(source.Id, source.RecipientName, source.Mobile, source.Province, source.City, source.PostalCode, source.AddressLine, source.Plaque, source.Unit, source.IsDefault);
}
