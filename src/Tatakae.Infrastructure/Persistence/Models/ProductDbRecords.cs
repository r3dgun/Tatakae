using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("Products")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(CategoryId))]
[Index(nameof(IsPublished), nameof(IsFeatured))]
public sealed class ProductDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(240)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public ApparelCategory ApparelCategory { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    /// <summary>برند محصول، برای فیلتر و SEO. در کالای اختصاصی می‌تواند خالی باشد.</summary>
    public Guid? BrandId { get; set; }

    [Required, MaxLength(700)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required, MaxLength(6000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Material { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Fit { get; set; } = string.Empty;

    [Required, MaxLength(1400)]
    public string CareGuide { get; set; } = string.Empty;

    [MaxLength(1000), Url]
    public string SizeGuideUrl { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string SeoMetaTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string SeoMetaDescription { get; set; } = string.Empty;

    [MaxLength(600)]
    public string? SeoCanonicalPath { get; set; }

    [MaxLength(1000), Url]
    public string? SeoOpenGraphImageUrl { get; set; }

    public bool SeoAllowIndex { get; set; } = true;
    public bool SeoAllowFollow { get; set; } = true;
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; }
    /// <summary>False یعنی محصول آماده/گلدوزی‌شده است و استودیو برای آن غیرفعال می‌شود.</summary>
    public bool SupportsEmbroidery { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public CategoryDbRecord? Category { get; set; }

    [ForeignKey(nameof(BrandId))]
    public BrandDbRecord? Brand { get; set; }

    [InverseProperty(nameof(ProductEmbroideryPolicyDbRecord.Product))]
    public ProductEmbroideryPolicyDbRecord? EmbroideryPolicy { get; set; }

    [InverseProperty(nameof(ProductImageDbRecord.Product))]
    public List<ProductImageDbRecord> Images { get; set; } = [];

    [InverseProperty(nameof(ProductVariantDbRecord.Product))]
    public List<ProductVariantDbRecord> Variants { get; set; } = [];

    [InverseProperty(nameof(ProductSpecificationDbRecord.Product))]
    public List<ProductSpecificationDbRecord> Specifications { get; set; } = [];

    [InverseProperty(nameof(ProductTagDbRecord.Product))]
    public List<ProductTagDbRecord> Tags { get; set; } = [];
}

[Table("ProductImages")]
[Index(nameof(ProductId), nameof(SortOrder))]
public sealed class ProductImageDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    [Required, MaxLength(1000), Url]
    public string Url { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string AltText { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }
}

[Table("ProductVariants")]
[Index(nameof(Sku), IsUnique = true)]
[Index(nameof(ProductId), nameof(Size), nameof(ColorHex))]
public sealed class ProductVariantDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    [Required, MaxLength(80)]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Size { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string ColorName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ColorHex { get; set; } = string.Empty;

    [Precision(18, 2)]
    public decimal RegularPrice { get; set; }

    [Precision(18, 2)]
    public decimal? SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReservedQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; } = 3;

    [MaxLength(1000), Url]
    public string? ImageUrl { get; set; }

    [MaxLength(120)]
    public string? Barcode { get; set; }

    public bool IsActive { get; set; } = true;

    // Added Concurrency Token
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }
}
[Table("ProductSpecifications")]
[Index(nameof(ProductId), nameof(SortOrder))]
public sealed class ProductSpecificationDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    [Required, MaxLength(140)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(700)]
    public string Value { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }
}

[Table("ProductTags")]
[Index(nameof(ProductId), nameof(Value))]
public sealed class ProductTagDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    [Required, MaxLength(120)]
    public string Value { get; set; } = string.Empty;

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }
}

[Table("ProductEmbroideryPolicies")]
[Index(nameof(ProductId), IsUnique = true)]
public sealed class ProductEmbroideryPolicyDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    [Precision(18, 2)]
    public decimal BasePrice { get; set; }

    [Precision(18, 2)]
    public decimal PerThreadColorPrice { get; set; }

    [Precision(18, 2)]
    public decimal PerSquareCentimeterPrice { get; set; }

    [Range(1, 24)]
    public int MaxThreadColors { get; set; }

    [Precision(9, 2)]
    public decimal MaxWidthCm { get; set; }

    [Precision(9, 2)]
    public decimal MaxHeightCm { get; set; }

    public bool AllowArtworkUpload { get; set; } = true;
    public bool AllowTextEmbroidery { get; set; } = true;

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }

    [InverseProperty(nameof(ProductAllowedPlacementDbRecord.Policy))]
    public List<ProductAllowedPlacementDbRecord> AllowedPlacements { get; set; } = [];

    [InverseProperty(nameof(ProductAllowedThreadColorDbRecord.Policy))]
    public List<ProductAllowedThreadColorDbRecord> AllowedThreadColors { get; set; } = [];
}

[Table("ProductAllowedPlacements")]
[Index(nameof(ProductEmbroideryPolicyId), nameof(Placement), IsUnique = true)]
public sealed class ProductAllowedPlacementDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductEmbroideryPolicyId { get; set; }

    [Required, MaxLength(80)]
    public EmbroideryPlacement Placement { get; set; }

    [ForeignKey(nameof(ProductEmbroideryPolicyId))]
    public ProductEmbroideryPolicyDbRecord? Policy { get; set; }
}

[Table("ProductAllowedThreadColors")]
[Index(nameof(ProductEmbroideryPolicyId), nameof(ColorHex), IsUnique = true)]
public sealed class ProductAllowedThreadColorDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductEmbroideryPolicyId { get; set; }

    [Required, MaxLength(20)]
    public string ColorHex { get; set; } = string.Empty;

    [ForeignKey(nameof(ProductEmbroideryPolicyId))]
    public ProductEmbroideryPolicyDbRecord? Policy { get; set; }
}
