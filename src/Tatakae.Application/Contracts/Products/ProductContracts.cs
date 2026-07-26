using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Seo;

namespace Tatakae.Application.Contracts.Products;

public sealed class ProductListQuery : IValidatableObject
{
    [StringLength(120)]
    public string? Search { get; set; }

    [StringLength(120)]
    public string? Category { get; set; }

    [StringLength(30)]
    public string? Size { get; set; }

    [StringLength(20)]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "رنگ باید به شکل #RRGGBB باشد.")]
    public string? Color { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? MaxPrice { get; set; }

    public bool InStockOnly { get; set; } = true;

    public bool FeaturedOnly { get; set; }

    public bool SaleOnly { get; set; }

    public bool ReadyMadeOnly { get; set; }

    public bool CustomizableOnly { get; set; }

    [Range(1, 200)]
    public int Page { get; set; } = 1;

    [Range(6, 48)]
    public int PageSize { get; set; } = 12;

    [RegularExpression("^(featured|newest|price-asc|price-desc)$", ErrorMessage = "مرتب‌سازی معتبر نیست.")]
    public string Sort { get; set; } = "featured";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice.Value > MaxPrice.Value)
        {
            yield return new ValidationResult("حداقل قیمت نمی‌تواند از حداکثر قیمت بیشتر باشد.", [nameof(MinPrice), nameof(MaxPrice)]);
        }

        if (ReadyMadeOnly && CustomizableOnly)
        {
            yield return new ValidationResult("فیلتر محصول آماده و قابل شخصی‌سازی را هم‌زمان انتخاب نکنید.", [nameof(ReadyMadeOnly), nameof(CustomizableOnly)]);
        }
    }
}

public sealed record ProductCardDto(
    Guid Id,
    string Name,
    string Slug,
    string CategoryName,
    string CategorySlug,
    string PrimaryImageUrl,
    string PrimaryImageAlt,
    string ShortDescription,
    decimal StartingPrice,
    decimal? CompareAtPrice,
    bool IsInStock,
    bool IsFeatured,
    bool SupportsEmbroidery,
    IReadOnlyCollection<string> Tags);

public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string CategoryName,
    string CategorySlug,
    string ApparelCategory,
    string ShortDescription,
    string Description,
    string Material,
    string Fit,
    string CareGuide,
    string SizeGuideUrl,
    bool IsInStock,
    bool IsFeatured,
    bool SupportsEmbroidery,
    IReadOnlyCollection<ProductImageDto> Images,
    IReadOnlyCollection<ProductVariantDto> Variants,
    IReadOnlyCollection<ProductSpecificationDto> Specifications,
    IReadOnlyCollection<string> Tags,
    EmbroideryPolicyDto EmbroideryPolicy,
    SeoDto Seo);

public sealed record ProductQuestionDto(Guid Id, Guid ProductId, string AuthorName, string QuestionText, string? AnswerText, DateTimeOffset CreatedAt, DateTimeOffset? AnsweredAt, bool IsAnswered);

public sealed class SubmitProductQuestionRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "نام پرسشگر الزامی است.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "نام باید بین ۲ تا ۸۰ کاراکتر باشد.")]
    public string AuthorName { get; set; } = string.Empty;

    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    [StringLength(20, ErrorMessage = "شماره موبایل حداکثر ۲۰ کاراکتر است.")]
    public string? Mobile { get; set; }

    [Required(ErrorMessage = "متن پرسش الزامی است.")]
    [StringLength(1200, MinimumLength = 5, ErrorMessage = "متن پرسش باید بین ۵ تا ۱۲۰۰ کاراکتر باشد.")]
    public string QuestionText { get; set; } = string.Empty;
}

public sealed record ProductImageDto(Guid Id, string Url, string AltText, bool IsPrimary, int SortOrder);
public sealed record ProductVariantDto(Guid Id, string Sku, string Size, string ColorName, string ColorHex, decimal RegularPrice, decimal? SalePrice, decimal EffectivePrice, int StockQuantity, bool IsActive, bool IsInStock)
{
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public int LowStockThreshold { get; init; } = 3;
    public bool IsLowStock { get; init; }
    public string? ImageUrl { get; init; }
    public string? Barcode { get; init; }
}

public sealed record ProductSpecificationDto(string Name, string Value, int SortOrder);
public sealed record EmbroideryPolicyDto(decimal BasePrice, decimal PerThreadColorPrice, decimal PerSquareCentimeterPrice, int MaxThreadColors, decimal MaxWidthCm, decimal MaxHeightCm, IReadOnlyCollection<string> AllowedPlacements, IReadOnlyCollection<string> AllowedThreadColors, bool AllowArtworkUpload, bool AllowTextEmbroidery);

public sealed class AdminProductRequest : IValidatableObject
{
    [Required(ErrorMessage = "نام محصول الزامی است.")]
    [StringLength(130, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression(SeoSlug.ValidationPattern, ErrorMessage = "Slug می‌تواند فارسی یا انگلیسی باشد؛ بین واژه‌ها فاصله یا خط تیره بگذارید.")]
    [StringLength(150)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [RegularExpression("^(TShirt|Hoodie|Sweatshirt|Polo|Jacket)$", ErrorMessage = "نوع لباس معتبر نیست.")]
    public string ApparelCategory { get; set; } = "TShirt";

    [Required]
    [StringLength(300, MinimumLength = 20)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(8000, MinimumLength = 80)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Material { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Fit { get; set; } = string.Empty;

    [Required]
    [StringLength(1200)]
    public string CareGuide { get; set; } = string.Empty;

    [StringLength(600)]
    public string SizeGuideUrl { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "حداقل یک تصویر برای محصول لازم است.")]
    public List<AdminProductImageRequest> Images { get; set; } = [];

    [MinLength(1, ErrorMessage = "حداقل یک واریانت قابل فروش لازم است.")]
    public List<AdminProductVariantRequest> Variants { get; set; } = [];

    public List<AdminProductSpecificationRequest> Specifications { get; set; } = [];

    public EmbroideryPolicyInput EmbroideryPolicy { get; set; } = new();

    public SeoInput Seo { get; set; } = new();

    [StringLength(500)]
    public string TagsCsv { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; }

    /// <summary>
    /// اگر false باشد محصول آماده و از قبل گلدوزی‌شده است؛ در سایت نباید وارد Studio شود.
    /// </summary>
    public bool SupportsEmbroidery { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryId == Guid.Empty)
        {
            yield return new ValidationResult("دسته‌بندی محصول را انتخاب کنید.", [nameof(CategoryId)]);
        }

        if (!string.IsNullOrWhiteSpace(SizeGuideUrl)
            && (!Uri.TryCreate(SizeGuideUrl.Trim(), UriKind.Absolute, out var sizeGuideUri)
                || (sizeGuideUri.Scheme != Uri.UriSchemeHttp && sizeGuideUri.Scheme != Uri.UriSchemeHttps)))
        {
            yield return new ValidationResult("آدرس راهنمای سایز باید یک URL کامل http یا https باشد.", [nameof(SizeGuideUrl)]);
        }

        if (Images.Count > 0 && Images.Count(x => x.IsPrimary) != 1)
        {
            yield return new ValidationResult("دقیقاً یک تصویر اصلی برای محصول انتخاب کنید.", [nameof(Images)]);
        }

        var duplicateSkus = Variants
            .Where(x => !string.IsNullOrWhiteSpace(x.Sku))
            .GroupBy(x => x.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateSkus.Length > 0)
        {
            yield return new ValidationResult($"SKU تکراری است: {string.Join("، ", duplicateSkus)}", [nameof(Variants)]);
        }

        if (Variants.Count > 0 && Variants.All(x => !x.IsActive))
        {
            yield return new ValidationResult("حداقل یک تنوع فعال و قابل فروش لازم است.", [nameof(Variants)]);
        }

        if (SupportsEmbroidery && (!EmbroideryPolicy.AllowArtworkUpload && !EmbroideryPolicy.AllowTextEmbroidery))
        {
            yield return new ValidationResult("برای محصول قابل شخصی‌سازی حداقل متن یا آپلود طرح را فعال کنید.", [nameof(EmbroideryPolicy)]);
        }
    }
}

public sealed class AdminProductImageRequest
{
    [Required]
    [Url]
    [StringLength(600)]
    public string Url { get; set; } = string.Empty;

    [Required]
    [StringLength(180, MinimumLength = 5)]
    public string AltText { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    [Range(0, 99)]
    public int SortOrder { get; set; }
}

public sealed class AdminProductVariantRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^[A-Z0-9-]{4,50}$", ErrorMessage = "SKU فقط شامل حروف بزرگ انگلیسی، عدد و خط تیره است.")]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Size { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string ColorName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "رنگ باید به شکل #RRGGBB باشد.")]
    public string ColorHex { get; set; } = "#111111";

    [Range(typeof(decimal), "0", "999999999")]
    public decimal RegularPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? SalePrice { get; set; }

    [Range(0, 999999)]
    public int StockQuantity { get; set; }

    [Range(0, 999999)]
    public int ReservedQuantity { get; set; }

    [Range(0, 999999)]
    public int LowStockThreshold { get; set; } = 3;

    [Url]
    [StringLength(600)]
    public string? ImageUrl { get; set; }

    [StringLength(120)]
    public string? Barcode { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalePrice.HasValue && SalePrice.Value >= RegularPrice)
        {
            yield return new ValidationResult("قیمت تخفیف باید از قیمت اصلی کمتر باشد.", [nameof(SalePrice), nameof(RegularPrice)]);
        }

        if (ReservedQuantity > StockQuantity)
        {
            yield return new ValidationResult("تعداد رزروشده نمی‌تواند از موجودی کل بیشتر باشد.", [nameof(ReservedQuantity), nameof(StockQuantity)]);
        }
    }
}

public sealed class AdminProductSpecificationRequest
{
    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Value { get; set; } = string.Empty;

    [Range(0, 99)]
    public int SortOrder { get; set; }
}

public sealed class EmbroideryPolicyInput
{
    [Range(typeof(decimal), "0", "999999999")]
    public decimal BasePrice { get; set; } = 85000m;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal PerThreadColorPrice { get; set; } = 12000m;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal PerSquareCentimeterPrice { get; set; } = 700m;

    [Range(1, 12)]
    public int MaxThreadColors { get; set; } = 6;

    [Range(typeof(decimal), "1", "40")]
    public decimal MaxWidthCm { get; set; } = 12m;

    [Range(typeof(decimal), "1", "40")]
    public decimal MaxHeightCm { get; set; } = 12m;

    [MinLength(1)]
    public List<string> AllowedPlacements { get; set; } = ["LeftChest", "CenterChest", "BackNeck", "LeftSleeve"];

    [MinLength(1)]
    public List<string> AllowedThreadColors { get; set; } = ["#FFFFFF", "#111111", "#E63946", "#F4A261", "#2A9D8F", "#457B9D"];

    public bool AllowArtworkUpload { get; set; } = true;
    public bool AllowTextEmbroidery { get; set; } = true;
}

public sealed record ProductFilterDto(
    IReadOnlyCollection<CategoryFilterDto> Categories,
    IReadOnlyCollection<SizeFilterDto> Sizes,
    IReadOnlyCollection<ColorFilterDto> Colors,
    decimal MinPrice,
    decimal MaxPrice,
    IReadOnlyCollection<string> Tags);

public sealed record CategoryFilterDto(Guid Id, string Name, string Slug, int ProductCount);
public sealed record SizeFilterDto(string Size, int ProductCount);
public sealed record ColorFilterDto(string Name, string Hex, int ProductCount);

public sealed record ProductListingPageDto(
    string Title,
    string Description,
    IReadOnlyCollection<ProductCardDto> Products,
    ProductFilterDto Filters,
    int Page,
    int PageSize,
    int TotalCount,
    SeoDto Seo);

public sealed record ProductOptionGroupDto(string Name, IReadOnlyCollection<ProductOptionDto> Options);
public sealed record ProductOptionDto(string Value, string Label, string? ColorHex, bool IsAvailable, decimal? PriceDelta);

public sealed record RelatedProductSectionDto(string Title, IReadOnlyCollection<ProductCardDto> Items);
