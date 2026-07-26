using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Seo;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Services;

public sealed partial class AdminCatalogService(
    IProductRepository products, ICategoryRepository categories,
    ILogger<AdminCatalogService>? logger = null) : IAdminCatalogService
{
    private readonly ILogger<AdminCatalogService> _logger = logger ?? NullLogger<AdminCatalogService>.Instance;
    public async Task<IReadOnlyCollection<ProductDetailDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categoryMap = (await categories.GetAllAsync(cancellationToken)).RequireData().ToDictionary(x => x.Id);
        return (await products.GetAllAsync(cancellationToken)).RequireData().OrderByDescending(x => x.IsFeatured).ThenBy(x => x.Name)
            .Select(x => CatalogService.Detail(x, categoryMap.TryGetValue(x.CategoryId, out var category) ? category : null)).ToArray();
    }

    public async Task<ProductDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = (await products.GetByIdAsync(id, cancellationToken)).DataOrDefault();
        var category = product is null ? null : (await categories.GetByIdAsync(product.CategoryId, cancellationToken)).DataOrDefault();
        return product is null ? null : CatalogService.Detail(product, category);
    }

    public async Task<ProductDetailDto> CreateAsync(AdminProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await BuildAsync(Guid.NewGuid(), request, null, cancellationToken);
        await EnsureSlugUniqueAsync(product.Slug, null, cancellationToken);
        (await products.UpsertAsync(product, cancellationToken)).EnsureSuccess();
        var category = (await categories.GetByIdAsync(product.CategoryId, cancellationToken)).DataOrDefault();
        return CatalogService.Detail(product, category);
    }

    public async Task<ProductDetailDto> UpdateAsync(Guid id, AdminProductRequest request, CancellationToken cancellationToken = default)
    {
        var existing = (await products.GetByIdAsync(id, cancellationToken)).RequireData();
        var product = await BuildAsync(id, request, existing, cancellationToken);
        await EnsureSlugUniqueAsync(product.Slug, id, cancellationToken);
        (await products.UpsertAsync(product, cancellationToken)).EnsureSuccess();
        var category = (await categories.GetByIdAsync(product.CategoryId, cancellationToken)).DataOrDefault();
        return CatalogService.Detail(product, category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => (await products.DeleteAsync(id, cancellationToken)).EnsureSuccess();

    private async Task<Product> BuildAsync(Guid id, AdminProductRequest request, Product? existing, CancellationToken cancellationToken)
    {
        var slug = SeoSlug.Normalize(request.Slug);
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug محصول معتبر نیست.");
        if (!Enum.TryParse<ApparelCategory>(request.ApparelCategory, true, out var apparelCategory)) throw new ArgumentException("نوع لباس معتبر نیست.");
        if ((await categories.GetByIdAsync(request.CategoryId, cancellationToken)).DataOrDefault() is null) throw new ArgumentException("دسته‌بندی انتخاب‌شده وجود ندارد.");
        if (request.Variants.Any(x => x.SalePrice is not null && x.SalePrice > x.RegularPrice)) throw new ArgumentException("قیمت فروش ویژه نمی‌تواند از قیمت اصلی بیشتر باشد.");
        if (request.Variants.Any(x => x.ReservedQuantity > x.StockQuantity)) throw new ArgumentException("موجودی رزروشده نمی‌تواند از موجودی کل بیشتر باشد.");
        if (request.Variants.Select(x => x.Sku.Trim().ToUpperInvariant()).Distinct().Count() != request.Variants.Count) throw new ArgumentException("SKU تکراری داخل همین محصول مجاز نیست.");
        if (request.Images.Count(x => x.IsPrimary) > 1) throw new ArgumentException("فقط یک تصویر اصلی انتخاب کنید.");

        var images = request.Images.Select((x, index) => new ProductImage(Guid.NewGuid(), x.Url, x.AltText, x.IsPrimary || (index == 0 && request.Images.All(i => !i.IsPrimary)), x.SortOrder)).ToArray();
        var variants = request.Variants.Select(x => new ProductVariant(Guid.NewGuid(), x.Sku, x.Size, x.ColorName, x.ColorHex, x.RegularPrice, x.SalePrice, x.StockQuantity, x.IsActive, x.ReservedQuantity, x.LowStockThreshold, x.ImageUrl, x.Barcode)).ToArray();
        var specs = request.Specifications.Select(x => new ProductSpecification(x.Name, x.Value, x.SortOrder)).ToArray();
        var placements = request.EmbroideryPolicy.AllowedPlacements.Select(x => Enum.TryParse<EmbroideryPlacement>(x, true, out var value) ? value : throw new ArgumentException("محل گلدوزی نامعتبر است.")).ToArray();
        var policy = new EmbroideryPolicy(request.EmbroideryPolicy.BasePrice, request.EmbroideryPolicy.PerThreadColorPrice, request.EmbroideryPolicy.PerSquareCentimeterPrice, request.EmbroideryPolicy.MaxThreadColors, request.EmbroideryPolicy.MaxWidthCm, request.EmbroideryPolicy.MaxHeightCm, placements, request.EmbroideryPolicy.AllowedThreadColors.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), request.EmbroideryPolicy.AllowArtworkUpload, request.EmbroideryPolicy.AllowTextEmbroidery);
        var canonical = SeoSlug.NormalizeCanonicalPath(request.Seo.CanonicalPath, $"/product/{slug}");
        var seo = new SeoMetadata(request.Seo.MetaTitle.Trim(), request.Seo.MetaDescription.Trim(), canonical, request.Seo.OpenGraphImageUrl ?? images.First().Url, request.Seo.AllowIndex, request.Seo.AllowFollow);
        var tags = request.TagsCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var now = DateTimeOffset.UtcNow;
        return existing is null
            ? Product.Create(id, request.Name, slug, apparelCategory, request.CategoryId, request.ShortDescription, request.Description, request.Material, request.Fit, request.CareGuide, request.SizeGuideUrl, seo, policy, images, variants, specs, tags, request.IsPublished, request.IsFeatured, request.SupportsEmbroidery, now)
            : Product.Rehydrate(id, request.Name, slug, apparelCategory, request.CategoryId, request.ShortDescription, request.Description, request.Material, request.Fit, request.CareGuide, request.SizeGuideUrl, seo, policy, images, variants, specs, tags, request.IsPublished, request.IsFeatured, request.SupportsEmbroidery, existing.CreatedAt, now);
    }

    private async Task EnsureSlugUniqueAsync(string slug, Guid? currentId, CancellationToken cancellationToken)
    {
        var duplicate = (await products.GetAllAsync(cancellationToken)).RequireData().Any(x => x.Id != currentId && string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (duplicate) throw new ArgumentException("Slug محصول تکراری است.");
    }
}
