using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Contracts.Products;

namespace Tatakae.Application.Contracts.Wishlist;

public sealed class ToggleWishlistRequest
{
    [Required]
    public Guid ProductId { get; set; }
}

public sealed record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string PrimaryImageUrl,
    string CategoryName,
    decimal StartingPrice,
    bool IsInStock,
    DateTimeOffset CreatedAt);

public sealed record WishlistDto(
    Guid CustomerId,
    IReadOnlyCollection<ProductCardDto> Items,
    int Count,
    DateTimeOffset? LastUpdatedAt);

public sealed record WishlistToggleResultDto(
    Guid ProductId,
    bool IsWishlisted,
    int Count,
    string Message);

public sealed record ProductRecommendationDto(
    ProductCardDto Product,
    string Reason,
    int Score);

public sealed record CustomerEngagementSummaryDto(
    WishlistDto Wishlist,
    IReadOnlyCollection<ProductRecommendationDto> Recommendations);

public sealed class RecommendationQuery
{
    [Range(1, 24)]
    public int Take { get; set; } = 8;

    [StringLength(150)]
    public string? ContextSlug { get; set; }
}
