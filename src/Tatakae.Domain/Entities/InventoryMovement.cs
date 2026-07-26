using Tatakae.Domain.Common;
using Tatakae.Domain.Enums;

namespace Tatakae.Domain.Entities;

public sealed record InventoryMovement
{
    public InventoryMovement(
        Guid id,
        Guid productId,
        Guid variantId,
        InventoryChangeType type,
        int quantity,
        string? note,
        DateTimeOffset createdAt)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه گردش موجودی معتبر نیست.");
        ProductId = DomainGuard.NotEmpty(productId, nameof(productId), "شناسه محصول گردش موجودی معتبر نیست.");
        VariantId = DomainGuard.NotEmpty(variantId, nameof(variantId), "شناسه SKU گردش موجودی معتبر نیست.");
        if (quantity == 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "مقدار گردش موجودی نمی‌تواند صفر باشد.");
        Type = type;
        Quantity = quantity;
        Note = DomainGuard.Optional(note);
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid ProductId { get; }
    public Guid VariantId { get; }
    public InventoryChangeType Type { get; }
    public int Quantity { get; }
    public string? Note { get; }
    public DateTimeOffset CreatedAt { get; }
}
