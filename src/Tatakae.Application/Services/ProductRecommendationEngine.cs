using Tatakae.Domain.Entities;

namespace Tatakae.Application.Services;

public static class ProductRecommendationEngine
{
    public static int Score(Product candidate, IReadOnlyCollection<Product> likedProducts, Product? contextProduct = null)
    {
        if (!candidate.IsPublished || !candidate.IsInStock) return -1000;

        var score = 0;
        if (candidate.IsFeatured) score += 18;
        if (candidate.SupportsEmbroidery) score += 5;
        if (candidate.Variants.Any(x => x.IsActive && x.SalePrice is not null && x.SalePrice < x.RegularPrice)) score += 7;

        var likedCategoryIds = likedProducts.Select(x => x.CategoryId).ToHashSet();
        var likedTags = likedProducts.SelectMany(x => x.Tags).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (likedCategoryIds.Contains(candidate.CategoryId)) score += 25;
        score += candidate.Tags.Count(tag => likedTags.Contains(tag)) * 8;

        if (contextProduct is not null)
        {
            if (candidate.CategoryId == contextProduct.CategoryId) score += 30;
            score += candidate.Tags.Intersect(contextProduct.Tags, StringComparer.OrdinalIgnoreCase).Count() * 10;
            if (candidate.SupportsEmbroidery == contextProduct.SupportsEmbroidery) score += 5;
        }

        score += Math.Max(0, 20 - (int)(candidate.StartingPrice / 250_000m));
        return score;
    }

    public static string Reason(Product candidate, IReadOnlyCollection<Product> likedProducts, Product? contextProduct = null)
    {
        if (contextProduct is not null && candidate.CategoryId == contextProduct.CategoryId) return "مشابه همین دسته";
        if (contextProduct is not null && candidate.Tags.Intersect(contextProduct.Tags, StringComparer.OrdinalIgnoreCase).Any()) return "مشابه از نظر سبک و تگ";
        if (likedProducts.Any(x => x.CategoryId == candidate.CategoryId)) return "بر اساس علاقه‌مندی‌های شما";
        if (candidate.Variants.Any(x => x.IsActive && x.SalePrice is not null && x.SalePrice < x.RegularPrice)) return "پیشنهاد تخفیف‌خورده";
        if (candidate.IsFeatured) return "پیشنهاد منتخب فروشگاه";
        return "پیشنهاد برای شما";
    }
}
