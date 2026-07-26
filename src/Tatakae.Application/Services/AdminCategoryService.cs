using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Categories;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Seo;
using Tatakae.Domain.Entities;

namespace Tatakae.Application.Services;

public sealed partial class AdminCategoryService(
    ICategoryRepository categories, IProductRepository products,
    ILogger<AdminCategoryService>? logger = null) : IAdminCategoryService
{
    private readonly ILogger<AdminCategoryService> _logger = logger ?? NullLogger<AdminCategoryService>.Instance;
    public async Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var allProducts = (await products.GetAllAsync(cancellationToken)).RequireData();
        return (await categories.GetAllAsync(cancellationToken)).RequireData().OrderBy(x => x.SortOrder).Select(x => CatalogService.Category(x, allProducts.Count(product => product.CategoryId == x.Id))).ToArray();
    }

    public async Task<CategoryDto> CreateAsync(AdminCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSlugAsync(request.Slug, null, cancellationToken);
        var category = Build(Guid.NewGuid(), request);
        (await categories.UpsertAsync(category, cancellationToken)).EnsureSuccess();
        return CatalogService.Category(category, 0);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, AdminCategoryRequest request, CancellationToken cancellationToken = default)
    {
        (await categories.GetByIdAsync(id, cancellationToken)).RequireData();
        await EnsureSlugAsync(request.Slug, id, cancellationToken);
        var category = Build(id, request);
        (await categories.UpsertAsync(category, cancellationToken)).EnsureSuccess();
        var count = (await products.GetAllAsync(cancellationToken)).RequireData().Count(x => x.CategoryId == id);
        return CatalogService.Category(category, count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => (await categories.DeleteAsync(id, cancellationToken)).EnsureSuccess();

    private static Category Build(Guid id, AdminCategoryRequest request)
    {
        var slug = SeoSlug.Normalize(request.Slug);
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug دسته‌بندی معتبر نیست.");
        var canonical = SeoSlug.NormalizeCanonicalPath(request.Seo.CanonicalPath, $"/category/{slug}");
        return new Category(id, request.Name, slug, request.Description, request.CoverImageUrl,
            new SeoMetadata(request.Seo.MetaTitle.Trim(), request.Seo.MetaDescription.Trim(), canonical, request.Seo.OpenGraphImageUrl ?? request.CoverImageUrl, request.Seo.AllowIndex, request.Seo.AllowFollow), null, request.SortOrder, request.IsActive);
    }

    private async Task EnsureSlugAsync(string slug, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalizedSlug = SeoSlug.Normalize(slug);
        var duplicate = (await categories.GetAllAsync(cancellationToken)).RequireData().Any(x => x.Id != currentId && string.Equals(x.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));
        if (duplicate) throw new ArgumentException("Slug دسته‌بندی تکراری است.");
    }
}
