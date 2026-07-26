using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Contracts.Embroidery;

namespace Tatakae.Application.Contracts.Cart;

public sealed class AddToCartRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid VariantId { get; set; }

    [Range(1, 20, ErrorMessage = "تعداد باید بین ۱ تا ۲۰ باشد.")]
    public int Quantity { get; set; } = 1;

    [Required]
    public EmbroideryCustomizationRequest Embroidery { get; set; } = new();
}


public sealed class MergeCartRequest
{
    [Required]
    public List<AddToCartRequest> Items { get; set; } = [];
}

public sealed record CartMergeResultDto(int ItemCount, DateTimeOffset UpdatedAt);

public sealed class UpdateCartLineRequest
{
    [Required]
    public Guid LineId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
}

public sealed class ApplyCouponToCartRequest
{
    [Required, StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "کد تخفیف معتبر نیست.")]
    public string CouponCode { get; set; } = string.Empty;
}

public sealed record CartDto(
    Guid Id,
    IReadOnlyCollection<CartLineDto> Lines,
    decimal Subtotal,
    decimal ShippingEstimate,
    decimal DiscountAmount,
    decimal Total,
    string? CouponCode,
    bool IsReadyForCheckout,
    IReadOnlyCollection<string> Warnings);

public sealed record CartLineDto(
    Guid LineId,
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string ProductSlug,
    string ProductImageUrl,
    string Sku,
    string Size,
    string ColorName,
    string ColorHex,
    int Quantity,
    decimal GarmentUnitPrice,
    decimal EmbroideryUnitPrice,
    decimal UnitPrice,
    decimal LineTotal,
    EmbroideryConfigurationDto Embroidery);

/// <summary>
/// User identity data required by cart persistence without exposing ClaimsPrincipal.
/// </summary>
public sealed record CartCustomerContext(
    Guid? IdentityUserId,
    string? Mobile,
    string FullName,
    string? Email);
