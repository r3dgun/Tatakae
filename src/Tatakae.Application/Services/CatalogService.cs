using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Seo;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Services;

public sealed partial class CatalogService(
    IProductRepository products, ICategoryRepository categories,
    ILogger<CatalogService>? logger = null) : ICatalogService
{
    private readonly ILogger<CatalogService> _logger = logger ?? NullLogger<CatalogService>.Instance;
    public async Task<PagedResult<ProductCardDto>> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var allProducts = (await products.GetAllAsync(cancellationToken)).RequireData().Where(x => x.IsPublished);
        var allCategories = (await categories.GetAllAsync(cancellationToken)).RequireData();
        var categoryMap = allCategories.ToDictionary(x => x.Id);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categorySlug = SeoSlug.Normalize(query.Category);
            var category = allCategories.SingleOrDefault(x => string.Equals(x.Slug, categorySlug, StringComparison.OrdinalIgnoreCase));
            allProducts = category is null ? Enumerable.Empty<Product>() : allProducts.Where(x => x.CategoryId == category.Id);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            allProducts = allProducts.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.ShortDescription.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Material.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(query.Size))
        {
            var size = query.Size.Trim();
            allProducts = allProducts.Where(x => x.Variants.Any(v => v.IsActive && v.Size.Equals(size, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(query.Color))
        {
            var color = query.Color.Trim();
            allProducts = allProducts.Where(x => x.Variants.Any(v => v.IsActive && v.ColorHex.Equals(color, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.MinPrice is not null)
        {
            allProducts = allProducts.Where(x => x.Variants.Any(v => v.IsActive && v.EffectivePrice >= query.MinPrice.Value));
        }

        if (query.MaxPrice is not null)
        {
            allProducts = allProducts.Where(x => x.Variants.Any(v => v.IsActive && v.EffectivePrice <= query.MaxPrice.Value));
        }

        if (query.InStockOnly)
        {
            allProducts = allProducts.Where(x => x.IsInStock);
        }

        if (query.FeaturedOnly)
        {
            allProducts = allProducts.Where(x => x.IsFeatured);
        }

        if (query.SaleOnly)
        {
            allProducts = allProducts.Where(x => x.Variants.Any(v => v.IsActive && v.SalePrice is not null && v.SalePrice < v.RegularPrice));
        }

        if (query.ReadyMadeOnly)
        {
            allProducts = allProducts.Where(x => !x.SupportsEmbroidery);
        }

        if (query.CustomizableOnly)
        {
            allProducts = allProducts.Where(x => x.SupportsEmbroidery);
        }

        allProducts = query.Sort switch
        {
            "newest" => allProducts.OrderByDescending(x => x.CreatedAt),
            "price-asc" => allProducts.OrderBy(x => x.StartingPrice),
            "price-desc" => allProducts.OrderByDescending(x => x.StartingPrice),
            _ => allProducts.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.Name)
        };

        var materialized = allProducts.ToArray();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 6, 48);
        var pageItems = materialized.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(product => Card(product, categoryMap.TryGetValue(product.CategoryId, out var category) ? category : null))
            .ToArray();

        return new PagedResult<ProductCardDto>(pageItems, page, pageSize, materialized.Length);
    }


    public async Task<ProductFilterDto> GetFiltersAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var allProducts = (await products.GetAllAsync(cancellationToken)).RequireData().Where(x => x.IsPublished).ToArray();
        var allCategories = (await categories.GetAllAsync(cancellationToken)).RequireData().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToArray();

        var categoryFilters = allCategories
            .Select(category => new CategoryFilterDto(category.Id, category.Name, category.Slug, allProducts.Count(product => product.CategoryId == category.Id && product.IsInStock)))
            .Where(x => x.ProductCount > 0)
            .ToArray();

        var activeVariants = allProducts.SelectMany(x => x.Variants.Where(v => v.IsActive)).ToArray();
        var sizeFilters = activeVariants
            .GroupBy(x => x.Size, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SizeFilterDto(x.Key, x.Count()))
            .OrderBy(x => SizeRank(x.Size))
            .ThenBy(x => x.Size)
            .ToArray();

        var colorFilters = activeVariants
            .GroupBy(x => x.ColorHex, StringComparer.OrdinalIgnoreCase)
            .Select(x => new ColorFilterDto(x.First().ColorName, x.Key, x.Count()))
            .OrderByDescending(x => x.ProductCount)
            .ThenBy(x => x.Name)
            .ToArray();

        var prices = activeVariants.Select(x => x.EffectivePrice).ToArray();
        var tags = allProducts.SelectMany(x => x.Tags).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();

        return new ProductFilterDto(
            categoryFilters,
            sizeFilters,
            colorFilters,
            prices.DefaultIfEmpty(0m).Min(),
            prices.DefaultIfEmpty(0m).Max(),
            tags);
    }

    private static int SizeRank(string size) => size.ToUpperInvariant() switch
    {
        "XS" => 1,
        "S" => 2,
        "M" => 3,
        "L" => 4,
        "XL" => 5,
        "2XL" => 6,
        "XXL" => 6,
        "3XL" => 7,
        "XXXL" => 7,
        _ => 99
    };

    public async Task<ProductDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = (await products.GetBySlugAsync(slug, cancellationToken)).DataOrDefault();
        if (product is null || !product.IsPublished) return null;
        var category = (await categories.GetByIdAsync(product.CategoryId, cancellationToken)).DataOrDefault();
        return Detail(product, category);
    }

    public async Task<IReadOnlyCollection<CategoryDto>> GetNavigationCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categoriesData = (await categories.GetAllAsync(cancellationToken)).RequireData().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToArray();
        var productData = (await products.GetAllAsync(cancellationToken)).RequireData();
        return categoriesData.Select(category => Category(category, productData.Count(product => product.IsPublished && product.CategoryId == category.Id))).ToArray();
    }

    public static ProductCardDto Card(Product product, Category? category)
    {
        var primary = product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).FirstOrDefault();
        var imageUrl = primary?.Url ?? "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=900&h=1100&fit=crop";
        var imageAlt = primary?.AltText ?? product.Name;
        var prices = product.Variants.Where(x => x.IsActive).Select(x => x.RegularPrice).ToArray();
        var effectivePrices = product.Variants.Where(x => x.IsActive).Select(x => x.EffectivePrice).ToArray();
        var starting = effectivePrices.DefaultIfEmpty(0m).Min();
        var regular = prices.DefaultIfEmpty(starting).Min();
        return new ProductCardDto(product.Id, product.Name, product.Slug, category?.Name ?? "پوشاک", category?.Slug ?? "apparel", imageUrl, imageAlt,
            product.ShortDescription, starting, regular > starting ? regular : null, product.IsInStock, product.IsFeatured, product.SupportsEmbroidery, product.Tags);
    }

    public static ProductDetailDto Detail(Product product, Category? category) => new(
        product.Id,
        product.Name,
        product.Slug,
        category?.Name ?? "پوشاک",
        category?.Slug ?? "apparel",
        product.ApparelCategory.ToString(),
        product.ShortDescription,
        product.Description,
        product.Material,
        product.Fit,
        product.CareGuide,
        product.SizeGuideUrl,
        product.IsInStock,
        product.IsFeatured,
        product.SupportsEmbroidery,
        product.Images.Select(x => new ProductImageDto(x.Id, x.Url, x.AltText, x.IsPrimary, x.SortOrder)).ToArray(),
        product.Variants.Select(x => new ProductVariantDto(x.Id, x.Sku, x.Size, x.ColorName, x.ColorHex, x.RegularPrice, x.SalePrice, x.EffectivePrice, x.StockQuantity, x.IsActive, x.IsInStock) { ReservedQuantity = x.ReservedQuantity, AvailableQuantity = x.AvailableQuantity, LowStockThreshold = x.LowStockThreshold, IsLowStock = x.IsLowStock, ImageUrl = x.ImageUrl, Barcode = x.Barcode }).ToArray(),
        product.Specifications.Select(x => new ProductSpecificationDto(x.Name, x.Value, x.SortOrder)).ToArray(),
        product.Tags,
        new EmbroideryPolicyDto(product.EmbroideryPolicy.BasePrice, product.EmbroideryPolicy.PerThreadColorPrice, product.EmbroideryPolicy.PerSquareCentimeterPrice, product.EmbroideryPolicy.MaxThreadColors, product.EmbroideryPolicy.MaxWidthCm, product.EmbroideryPolicy.MaxHeightCm, product.EmbroideryPolicy.AllowedPlacements.Select(x => x.ToString()).ToArray(), product.EmbroideryPolicy.AllowedThreadColors, product.EmbroideryPolicy.AllowArtworkUpload, product.EmbroideryPolicy.AllowTextEmbroidery),
        new SeoDto(product.Seo.MetaTitle, product.Seo.MetaDescription, product.Seo.CanonicalPath, product.Seo.OpenGraphImageUrl, product.Seo.AllowIndex, product.Seo.AllowFollow));

    public static CategoryDto Category(Category category, int productCount) => new(category.Id, category.Name, category.Slug, category.Description, category.CoverImageUrl, productCount, category.IsActive, category.SortOrder, new SeoDto(category.Seo.MetaTitle, category.Seo.MetaDescription, category.Seo.CanonicalPath, category.Seo.OpenGraphImageUrl, category.Seo.AllowIndex, category.Seo.AllowFollow));
}
