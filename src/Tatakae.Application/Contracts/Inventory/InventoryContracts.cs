using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Inventory;

public sealed class InventoryAdjustmentRequest : IValidatableObject
{
    [Required]
    public Guid VariantId { get; set; }

    [Range(-999999, 999999)]
    public int QuantityDelta { get; set; }

    [Required, RegularExpression("^(ManualCorrection|Restock|OrderReservation|OrderRelease|Damage|Return)$")]
    public string Reason { get; set; } = "ManualCorrection";

    [StringLength(500, ErrorMessage = "یادداشت اصلاح موجودی حداکثر ۵۰۰ کاراکتر است.")]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VariantId == Guid.Empty)
        {
            yield return new ValidationResult("واریانت محصول معتبر نیست.", [nameof(VariantId)]);
        }
        if (QuantityDelta == 0)
        {
            yield return new ValidationResult("مقدار اصلاح موجودی نمی‌تواند صفر باشد.", [nameof(QuantityDelta)]);
        }
    }
}

public sealed record InventoryVariantDto(Guid VariantId, Guid ProductId, string ProductName, string Sku, string Size, string ColorName, string ColorHex, int StockQuantity, int ReservedQuantity, int AvailableQuantity, bool IsLowStock, bool IsActive);

public sealed record InventoryMovementDto(Guid Id, Guid VariantId, string Sku, string ChangeType, int QuantityDelta, int QuantityAfter, string? ReferenceNumber, string? Note, DateTimeOffset CreatedAt);

public sealed record InventoryReservationSnapshot(
    Guid OrderId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int TotalQuantity)
{
    public bool IsActive(DateTimeOffset now)
        => string.Equals(Status, "Reserved", StringComparison.OrdinalIgnoreCase) && ExpiresAt > now;
}
