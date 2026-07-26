using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Wishlist;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Entities;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class WishlistService(
    IWishlistRepository wishlists, ICustomerRepository customers, IProductRepository products, ICategoryRepository categories,
    ILogger<WishlistService>? logger = null) : IWishlistService
{
    private readonly ILogger<WishlistService> _logger = logger ?? NullLogger<WishlistService>.Instance;
    public async Task<WishlistDto> GetAsync(string mobile, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return new WishlistDto(Guid.Empty, Array.Empty<Tatakae.Application.Contracts.Products.ProductCardDto>(), 0, null);

        var entries = (await wishlists.GetByCustomerAsync(customer.Id, cancellationToken)).RequireData();
        var productData = (await products.GetAllAsync(cancellationToken)).RequireData();
        var categoryMap = (await categories.GetAllAsync(cancellationToken)).RequireData().ToDictionary(x => x.Id);
        var entryMap = entries.ToDictionary(x => x.ProductId);

        var cards = productData
            .Where(x => entryMap.ContainsKey(x.Id) && x.IsPublished)
            .OrderByDescending(x => entryMap[x.Id].CreatedAt)
            .Select(product => CatalogService.Card(product, categoryMap.TryGetValue(product.CategoryId, out var category) ? category : null))
            .ToArray();

        return new WishlistDto(customer.Id, cards, cards.Length, entries.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.CreatedAt);
    }

    public async Task<bool> IsWishlistedAsync(string mobile, Guid productId, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        return customer is not null && (await wishlists.ExistsAsync(customer.Id, productId, cancellationToken)).RequireData();
    }

    public async Task<WishlistToggleResultDto?> ToggleAsync(string mobile, Guid productId, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return null;

        var product = (await products.GetByIdAsync(productId, cancellationToken)).DataOrDefault();
        if (product is null || !product.IsPublished) return null;

        var exists = (await wishlists.ExistsAsync(customer.Id, productId, cancellationToken)).RequireData();
        if (exists)
        {
            (await wishlists.RemoveAsync(customer.Id, productId, cancellationToken)).EnsureSuccess();
        }
        else
        {
            (await wishlists.AddAsync(customer.Id, productId, cancellationToken)).EnsureSuccess();
        }

        var count = (await wishlists.GetByCustomerAsync(customer.Id, cancellationToken)).RequireData().Count;
        return new WishlistToggleResultDto(productId, !exists, count, exists ? "از علاقه‌مندی‌ها حذف شد." : "به علاقه‌مندی‌ها اضافه شد.");
    }

    public async Task<bool> RemoveAsync(string mobile, Guid productId, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return false;
        (await wishlists.RemoveAsync(customer.Id, productId, cancellationToken)).EnsureSuccess();
        return true;
    }

    public async Task<IReadOnlyCollection<ProductRecommendationDto>> RecommendationsAsync(string mobile, RecommendationQuery query, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        var allProducts = (await products.GetAllAsync(cancellationToken)).RequireData().Where(x => x.IsPublished).ToArray();
        var categoryMap = (await categories.GetAllAsync(cancellationToken)).RequireData().ToDictionary(x => x.Id);
        var wishlistEntries = customer is null ? Array.Empty<WishlistEntry>() : (await wishlists.GetByCustomerAsync(customer.Id, cancellationToken)).RequireData().ToArray();
        var wishlistProductIds = wishlistEntries.Select(x => x.ProductId).ToHashSet();
        var likedProducts = allProducts.Where(x => wishlistProductIds.Contains(x.Id)).ToArray();
        var context = string.IsNullOrWhiteSpace(query.ContextSlug) ? null : allProducts.FirstOrDefault(x => x.Slug.Equals(query.ContextSlug.Trim(), StringComparison.OrdinalIgnoreCase));

        var recommendations = allProducts
            .Where(x => !wishlistProductIds.Contains(x.Id))
            .Where(x => context is null || x.Id != context.Id)
            .Select(product => new { Product = product, Score = ProductRecommendationEngine.Score(product, likedProducts, context) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.StartingPrice)
            .Take(Math.Clamp(query.Take, 1, 24))
            .Select(x => new ProductRecommendationDto(
                CatalogService.Card(x.Product, categoryMap.TryGetValue(x.Product.CategoryId, out var category) ? category : null),
                ProductRecommendationEngine.Reason(x.Product, likedProducts, context),
                x.Score))
            .ToArray();

        return recommendations;
    }

    public async Task<IReadOnlyCollection<ProductRecommendationDto>> SimilarAsync(string slug, int take = 6, CancellationToken cancellationToken = default)
    {
        var allProducts = (await products.GetAllAsync(cancellationToken)).RequireData().Where(x => x.IsPublished).ToArray();
        var context = allProducts.FirstOrDefault(x => x.Slug.Equals(slug.Trim(), StringComparison.OrdinalIgnoreCase));
        if (context is null) return Array.Empty<ProductRecommendationDto>();

        var categoryMap = (await categories.GetAllAsync(cancellationToken)).RequireData().ToDictionary(x => x.Id);
        return allProducts
            .Where(x => x.Id != context.Id)
            .Select(product => new { Product = product, Score = ProductRecommendationEngine.Score(product, Array.Empty<Product>(), context) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(Math.Clamp(take, 1, 12))
            .Select(x => new ProductRecommendationDto(
                CatalogService.Card(x.Product, categoryMap.TryGetValue(x.Product.CategoryId, out var category) ? category : null),
                ProductRecommendationEngine.Reason(x.Product, Array.Empty<Product>(), context),
                x.Score))
            .ToArray();
    }
}
