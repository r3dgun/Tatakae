using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Seo;
using Tatakae.Domain.Entities;
using Tatakae.Infrastructure.Persistence.Mappers;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlProductRepository(
    TatakaeDbContext db,
    ILogger<SqlProductRepository>? logger = null) : IProductRepository
{
    private readonly ILogger<SqlProductRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlProductRepository>.Instance;

    private async Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await Query().OrderByDescending(x => x.IsFeatured).ThenBy(x => x.Name).ToListAsync(cancellationToken)).Select(x => x.ToDomain()).ToArray();

    private async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => (await Query().SingleOrDefaultAsync(x => x.Id == id, cancellationToken))?.ToDomain();

    private async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = SeoSlug.Normalize(slug);
        return (await Query().SingleOrDefaultAsync(x => x.Slug == normalizedSlug, cancellationToken))?.ToDomain();
    }

    private async Task UpsertAsync(Product product, CancellationToken cancellationToken = default)
    {
        var incoming = product.ToRecord();
        var existing = await db.Products
            .IgnoreQueryFilters()
            .Include(x => x.Images)
            .Include(x => x.Variants)
            .Include(x => x.Specifications)
            .Include(x => x.Tags)
            .Include(x => x.EmbroideryPolicy)!.ThenInclude(x => x.AllowedPlacements)
            .Include(x => x.EmbroideryPolicy)!.ThenInclude(x => x.AllowedThreadColors)
            .SingleOrDefaultAsync(x => x.Id == product.Id, cancellationToken);

        if (existing is null)
        {
            db.Products.Add(incoming);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.Name = incoming.Name;
        existing.Slug = incoming.Slug;
        existing.ApparelCategory = incoming.ApparelCategory;
        existing.CategoryId = incoming.CategoryId;
        existing.BrandId = incoming.BrandId;
        existing.ShortDescription = incoming.ShortDescription;
        existing.Description = incoming.Description;
        existing.Material = incoming.Material;
        existing.Fit = incoming.Fit;
        existing.CareGuide = incoming.CareGuide;
        existing.SizeGuideUrl = incoming.SizeGuideUrl;
        existing.SeoMetaTitle = incoming.SeoMetaTitle;
        existing.SeoMetaDescription = incoming.SeoMetaDescription;
        existing.SeoCanonicalPath = incoming.SeoCanonicalPath;
        existing.SeoOpenGraphImageUrl = incoming.SeoOpenGraphImageUrl;
        existing.SeoAllowIndex = incoming.SeoAllowIndex;
        existing.SeoAllowFollow = incoming.SeoAllowFollow;
        existing.IsPublished = incoming.IsPublished;
        existing.IsFeatured = incoming.IsFeatured;
        existing.SupportsEmbroidery = incoming.SupportsEmbroidery;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        db.Restore(existing);

        SyncImages(existing.Images, incoming.Images);
        SyncVariants(existing.Variants, incoming.Variants);
        SyncSpecifications(existing.Specifications, incoming.Specifications);
        SyncTags(existing.Tags, incoming.Tags);
        SyncEmbroideryPolicy(existing, incoming.EmbroideryPolicy!);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) throw new KeyNotFoundException("محصول پیدا نشد.");

        var removedAt = DateTime.Now;
        var policyIds = await db.ProductEmbroideryPolicies
            .Where(x => x.ProductId == id)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (policyIds.Length > 0)
        {
            db.SoftDeleteRange(await db.ProductAllowedPlacements
                .Where(x => policyIds.Contains(x.ProductEmbroideryPolicyId))
                .ToListAsync(cancellationToken), removedAt);
            db.SoftDeleteRange(await db.ProductAllowedThreadColors
                .Where(x => policyIds.Contains(x.ProductEmbroideryPolicyId))
                .ToListAsync(cancellationToken), removedAt);
            db.SoftDeleteRange(await db.ProductEmbroideryPolicies
                .Where(x => x.ProductId == id)
                .ToListAsync(cancellationToken), removedAt);
        }

        db.SoftDeleteRange(await db.CartItems.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.Wishlists.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.ProductOffers.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.ProductReviews.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.ProductQuestions.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.ProductImages.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.ProductVariants.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.ProductSpecifications.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDeleteRange(await db.ProductTags.Where(x => x.ProductId == id).ToListAsync(cancellationToken), removedAt);
        db.SoftDelete(product, removedAt);

        await db.SaveChangesAsync(cancellationToken);
    }

    private void SyncImages(ICollection<ProductImageDbRecord> stored, IEnumerable<ProductImageDbRecord> incoming)
    {
        var incomingRows = incoming.ToArray();
        var incomingIds = incomingRows.Select(x => x.Id).ToHashSet();
        foreach (var row in stored.Where(x => !incomingIds.Contains(x.Id))) db.SoftDelete(row);

        foreach (var row in incomingRows)
        {
            var current = stored.SingleOrDefault(x => x.Id == row.Id);
            if (current is null)
            {
                stored.Add(row);
                continue;
            }

            current.Url = row.Url;
            current.AltText = row.AltText;
            current.IsPrimary = row.IsPrimary;
            current.SortOrder = row.SortOrder;
            db.Restore(current);
        }
    }

    private void SyncVariants(ICollection<ProductVariantDbRecord> stored, IEnumerable<ProductVariantDbRecord> incoming)
    {
        var incomingRows = incoming.ToArray();
        var incomingIds = incomingRows.Select(x => x.Id).ToHashSet();
        foreach (var row in stored.Where(x => !incomingIds.Contains(x.Id))) db.SoftDelete(row);

        foreach (var row in incomingRows)
        {
            var current = stored.SingleOrDefault(x => x.Id == row.Id);
            if (current is null)
            {
                stored.Add(row);
                continue;
            }

            current.Sku = row.Sku;
            current.Size = row.Size;
            current.ColorName = row.ColorName;
            current.ColorHex = row.ColorHex;
            current.RegularPrice = row.RegularPrice;
            current.SalePrice = row.SalePrice;
            current.StockQuantity = row.StockQuantity;
            current.ReservedQuantity = row.ReservedQuantity;
            current.LowStockThreshold = row.LowStockThreshold;
            current.ImageUrl = row.ImageUrl;
            current.Barcode = row.Barcode;
            current.IsActive = row.IsActive;
            db.Restore(current);
        }
    }

    private void SyncSpecifications(ICollection<ProductSpecificationDbRecord> stored, IEnumerable<ProductSpecificationDbRecord> incoming)
    {
        var incomingRows = incoming.ToArray();
        var incomingIds = incomingRows.Select(x => x.Id).ToHashSet();
        foreach (var row in stored.Where(x => !incomingIds.Contains(x.Id))) db.SoftDelete(row);

        foreach (var row in incomingRows)
        {
            var current = stored.SingleOrDefault(x => x.Id == row.Id);
            if (current is null)
            {
                stored.Add(row);
                continue;
            }

            current.Name = row.Name;
            current.Value = row.Value;
            current.SortOrder = row.SortOrder;
            db.Restore(current);
        }
    }

    private void SyncTags(ICollection<ProductTagDbRecord> stored, IEnumerable<ProductTagDbRecord> incoming)
    {
        var incomingRows = incoming.ToArray();
        var incomingIds = incomingRows.Select(x => x.Id).ToHashSet();
        foreach (var row in stored.Where(x => !incomingIds.Contains(x.Id))) db.SoftDelete(row);

        foreach (var row in incomingRows)
        {
            var current = stored.SingleOrDefault(x => x.Id == row.Id);
            if (current is null)
            {
                stored.Add(row);
                continue;
            }

            current.Value = row.Value;
            db.Restore(current);
        }
    }

    private void SyncEmbroideryPolicy(ProductDbRecord product, ProductEmbroideryPolicyDbRecord incoming)
    {
        if (product.EmbroideryPolicy is null)
        {
            product.EmbroideryPolicy = incoming;
            return;
        }

        var stored = product.EmbroideryPolicy;
        stored.BasePrice = incoming.BasePrice;
        stored.PerThreadColorPrice = incoming.PerThreadColorPrice;
        stored.PerSquareCentimeterPrice = incoming.PerSquareCentimeterPrice;
        stored.MaxThreadColors = incoming.MaxThreadColors;
        stored.MaxWidthCm = incoming.MaxWidthCm;
        stored.MaxHeightCm = incoming.MaxHeightCm;
        stored.AllowArtworkUpload = incoming.AllowArtworkUpload;
        stored.AllowTextEmbroidery = incoming.AllowTextEmbroidery;
        db.Restore(stored);

        var placementRows = incoming.AllowedPlacements.ToArray();
        var placementIds = placementRows.Select(x => x.Id).ToHashSet();
        foreach (var row in stored.AllowedPlacements.Where(x => !placementIds.Contains(x.Id))) db.SoftDelete(row);
        foreach (var row in placementRows)
        {
            var current = stored.AllowedPlacements.SingleOrDefault(x => x.Id == row.Id);
            if (current is null) stored.AllowedPlacements.Add(row);
            else
            {
                current.Placement = row.Placement;
                db.Restore(current);
            }
        }

        var colorRows = incoming.AllowedThreadColors.ToArray();
        var colorIds = colorRows.Select(x => x.Id).ToHashSet();
        foreach (var row in stored.AllowedThreadColors.Where(x => !colorIds.Contains(x.Id))) db.SoftDelete(row);
        foreach (var row in colorRows)
        {
            var current = stored.AllowedThreadColors.SingleOrDefault(x => x.Id == row.Id);
            if (current is null) stored.AllowedThreadColors.Add(row);
            else
            {
                current.ColorHex = row.ColorHex;
                db.Restore(current);
            }
        }
    }

    private IQueryable<ProductDbRecord> Query() => db.Products.AsNoTracking()
        .Include(x => x.Images)
        .Include(x => x.Variants)
        .Include(x => x.Specifications)
        .Include(x => x.Tags)
        .Include(x => x.EmbroideryPolicy)!.ThenInclude(x => x.AllowedPlacements)
        .Include(x => x.EmbroideryPolicy)!.ThenInclude(x => x.AllowedThreadColors);
}
