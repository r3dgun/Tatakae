using Tatakae.Domain.Common;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Entities;

/// <summary>Product aggregate root for the embroidery apparel catalog.</summary>
public sealed class Product
{
    private readonly IReadOnlyCollection<ProductImage> _images;
    private readonly IReadOnlyCollection<ProductVariant> _variants;
    private readonly IReadOnlyCollection<ProductSpecification> _specifications;
    private readonly IReadOnlyCollection<string> _tags;

    private Product(
        Guid id,
        string name,
        string slug,
        ApparelCategory apparelCategory,
        Guid categoryId,
        string shortDescription,
        string description,
        string material,
        string fit,
        string careGuide,
        string? sizeGuideUrl,
        SeoMetadata seo,
        EmbroideryPolicy embroideryPolicy,
        IReadOnlyCollection<ProductImage> images,
        IReadOnlyCollection<ProductVariant> variants,
        IReadOnlyCollection<ProductSpecification> specifications,
        IReadOnlyCollection<string> tags,
        bool isPublished,
        bool isFeatured,
        bool supportsEmbroidery,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه محصول معتبر نیست.");
        Name = DomainGuard.Required(name, nameof(name), "نام محصول الزامی است.");
        Slug = DomainGuard.Required(slug, nameof(slug), "اسلاگ محصول الزامی است.").ToLowerInvariant();
        ApparelCategory = apparelCategory;
        CategoryId = DomainGuard.NotEmpty(categoryId, nameof(categoryId), "شناسه دسته‌بندی محصول معتبر نیست.");
        ShortDescription = DomainGuard.Required(shortDescription, nameof(shortDescription), "توضیح کوتاه محصول الزامی است.");
        Description = DomainGuard.Required(description, nameof(description), "توضیحات محصول الزامی است.");
        Material = DomainGuard.Required(material, nameof(material), "جنس محصول الزامی است.");
        Fit = DomainGuard.Required(fit, nameof(fit), "نوع فیت محصول الزامی است.");
        CareGuide = DomainGuard.Required(careGuide, nameof(careGuide), "راهنمای نگهداری محصول الزامی است.");
        SizeGuideUrl = DomainGuard.Optional(sizeGuideUrl) ?? string.Empty;
        Seo = seo ?? throw new ArgumentNullException(nameof(seo));
        EmbroideryPolicy = embroideryPolicy ?? throw new ArgumentNullException(nameof(embroideryPolicy));

        _images = DomainGuard.NotEmpty(images, nameof(images), "محصول باید حداقل یک تصویر داشته باشد.")
            .OrderBy(x => x.SortOrder)
            .ToArray();
        _variants = DomainGuard.NotEmpty(variants, nameof(variants), "محصول باید حداقل یک SKU قابل فروش داشته باشد.")
            .ToArray();
        _specifications = (specifications ?? []).OrderBy(x => x.SortOrder).ToArray();
        _tags = (tags ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (_images.Count(x => x.IsPrimary) != 1)
            throw new ArgumentException("محصول باید دقیقاً یک تصویر اصلی داشته باشد.", nameof(images));

        var duplicateSku = _variants
            .GroupBy(x => x.Sku, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1)?.Key;
        if (duplicateSku is not null)
            throw new ArgumentException($"SKU تکراری در محصول مجاز نیست: {duplicateSku}", nameof(variants));

        IsPublished = isPublished;
        IsFeatured = isFeatured;
        SupportsEmbroidery = supportsEmbroidery;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt < createdAt ? createdAt : updatedAt;
    }


    public Guid Id { get; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public ApparelCategory ApparelCategory { get; private set; }
    public Guid CategoryId { get; private set; }
    public string ShortDescription { get; private set; }
    public string Description { get; private set; }
    public string Material { get; private set; }
    public string Fit { get; private set; }
    public string CareGuide { get; private set; }
    public string SizeGuideUrl { get; private set; }
    public SeoMetadata Seo { get; private set; }
    public EmbroideryPolicy EmbroideryPolicy { get; private set; }
    public IReadOnlyCollection<ProductImage> Images => _images;
    public IReadOnlyCollection<ProductVariant> Variants => _variants;
    public IReadOnlyCollection<ProductSpecification> Specifications => _specifications;
    public IReadOnlyCollection<string> Tags => _tags;
    public bool IsPublished { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool SupportsEmbroidery { get; private set; }
    public bool IsReadyMade => !SupportsEmbroidery;
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public decimal StartingPrice => _variants
        .Where(x => x.IsActive)
        .Select(x => x.EffectivePrice)
        .DefaultIfEmpty(0m)
        .Min();

    public bool IsInStock => _variants.Any(x => x.IsInStock);
    public bool IsAvailableForSale => IsPublished && IsInStock;

    public static Product Create(
        Guid id,
        string name,
        string slug,
        ApparelCategory apparelCategory,
        Guid categoryId,
        string shortDescription,
        string description,
        string material,
        string fit,
        string careGuide,
        string? sizeGuideUrl,
        SeoMetadata seo,
        EmbroideryPolicy embroideryPolicy,
        IReadOnlyCollection<ProductImage> images,
        IReadOnlyCollection<ProductVariant> variants,
        IReadOnlyCollection<ProductSpecification> specifications,
        IReadOnlyCollection<string> tags,
        bool isPublished,
        bool isFeatured,
        bool supportsEmbroidery,
        DateTimeOffset createdAt)
        => new(
            id,
            name,
            slug,
            apparelCategory,
            categoryId,
            shortDescription,
            description,
            material,
            fit,
            careGuide,
            sizeGuideUrl,
            seo,
            embroideryPolicy,
            images,
            variants,
            specifications,
            tags,
            isPublished,
            isFeatured,
            supportsEmbroidery,
            createdAt,
            createdAt);

    public static Product Rehydrate(
        Guid id,
        string name,
        string slug,
        ApparelCategory apparelCategory,
        Guid categoryId,
        string shortDescription,
        string description,
        string material,
        string fit,
        string careGuide,
        string? sizeGuideUrl,
        SeoMetadata seo,
        EmbroideryPolicy embroideryPolicy,
        IReadOnlyCollection<ProductImage> images,
        IReadOnlyCollection<ProductVariant> variants,
        IReadOnlyCollection<ProductSpecification> specifications,
        IReadOnlyCollection<string> tags,
        bool isPublished,
        bool isFeatured,
        bool supportsEmbroidery,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(
            id,
            name,
            slug,
            apparelCategory,
            categoryId,
            shortDescription,
            description,
            material,
            fit,
            careGuide,
            sizeGuideUrl,
            seo,
            embroideryPolicy,
            images,
            variants,
            specifications,
            tags,
            isPublished,
            isFeatured,
            supportsEmbroidery,
            createdAt,
            updatedAt);

    public void Publish(DateTimeOffset changedAt)
    {
        IsPublished = true;
        Touch(changedAt);
    }

    public void Unpublish(DateTimeOffset changedAt)
    {
        IsPublished = false;
        Touch(changedAt);
    }

    public void SetFeatured(bool isFeatured, DateTimeOffset changedAt)
    {
        IsFeatured = isFeatured;
        Touch(changedAt);
    }

    private void Touch(DateTimeOffset changedAt)
        => UpdatedAt = changedAt < CreatedAt ? CreatedAt : changedAt;
}
