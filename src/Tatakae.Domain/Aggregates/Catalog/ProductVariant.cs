using Tatakae.Domain.Common;

namespace Tatakae.Domain.Entities;

/// <summary>A sellable SKU. Pricing and inventory invariants are owned here.</summary>
public sealed class ProductVariant
{
    public ProductVariant(
        Guid id,
        string sku,
        string size,
        string colorName,
        string colorHex,
        decimal regularPrice,
        decimal? salePrice,
        int stockQuantity,
        bool isActive = true,
        int reservedQuantity = 0,
        int lowStockThreshold = 3,
        string? imageUrl = null,
        string? barcode = null)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه SKU معتبر نیست.");
        Sku = DomainGuard.Required(sku, nameof(sku), "SKU الزامی است.").ToUpperInvariant();
        Size = DomainGuard.Required(size, nameof(size), "سایز SKU الزامی است.");
        ColorName = DomainGuard.Required(colorName, nameof(colorName), "نام رنگ SKU الزامی است.");
        ColorHex = DomainGuard.Required(colorHex, nameof(colorHex), "کد رنگ SKU الزامی است.");
        RegularPrice = DomainGuard.NonNegative(regularPrice, nameof(regularPrice), "قیمت اصلی نمی‌تواند منفی باشد.");
        if (salePrice is < 0 || salePrice > regularPrice)
            throw new ArgumentOutOfRangeException(nameof(salePrice), salePrice, "قیمت تخفیف باید بین صفر و قیمت اصلی باشد.");

        StockQuantity = DomainGuard.NonNegative(stockQuantity, nameof(stockQuantity), "موجودی نمی‌تواند منفی باشد.");
        ReservedQuantity = DomainGuard.NonNegative(reservedQuantity, nameof(reservedQuantity), "موجودی رزروشده نمی‌تواند منفی باشد.");
        if (ReservedQuantity > StockQuantity)
            throw new ArgumentException("موجودی رزروشده نمی‌تواند از موجودی کل بیشتر باشد.", nameof(reservedQuantity));

        LowStockThreshold = DomainGuard.NonNegative(lowStockThreshold, nameof(lowStockThreshold), "آستانه موجودی کم نمی‌تواند منفی باشد.");
        SalePrice = salePrice;
        ImageUrl = DomainGuard.Optional(imageUrl);
        Barcode = DomainGuard.Optional(barcode);
        IsActive = isActive;
    }

    public Guid Id { get; }
    public string Sku { get; private set; }
    public string Size { get; private set; }
    public string ColorName { get; private set; }
    public string ColorHex { get; private set; }
    public decimal RegularPrice { get; private set; }
    public decimal? SalePrice { get; private set; }
    public decimal EffectivePrice => SalePrice ?? RegularPrice;
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => StockQuantity - ReservedQuantity;
    public int LowStockThreshold { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Barcode { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsInStock => IsActive && AvailableQuantity > 0;
    public bool IsLowStock => IsActive && AvailableQuantity > 0 && AvailableQuantity <= LowStockThreshold;

    public void AdjustStock(int delta)
    {
        var updatedStock = StockQuantity + delta;
        if (updatedStock < ReservedQuantity)
            throw new InvalidOperationException("کاهش موجودی باعث می‌شود موجودی کل از موجودی رزروشده کمتر شود.");

        StockQuantity = updatedStock;
    }

    public void Reserve(int quantity)
    {
        DomainGuard.Positive(quantity, nameof(quantity), "تعداد رزرو باید بیشتر از صفر باشد.");
        if (AvailableQuantity < quantity)
            throw new InvalidOperationException("موجودی قابل فروش برای رزرو کافی نیست.");

        ReservedQuantity += quantity;
    }

    public void ReleaseReservation(int quantity)
    {
        DomainGuard.Positive(quantity, nameof(quantity), "تعداد آزادسازی رزرو باید بیشتر از صفر باشد.");
        if (ReservedQuantity < quantity)
            throw new InvalidOperationException("تعداد آزادسازی از موجودی رزروشده بیشتر است.");

        ReservedQuantity -= quantity;
    }

    public void FulfillReservation(int quantity)
    {
        DomainGuard.Positive(quantity, nameof(quantity), "تعداد مصرف رزرو باید بیشتر از صفر باشد.");
        if (ReservedQuantity < quantity)
            throw new InvalidOperationException("رزرو کافی برای مصرف موجود نیست.");

        ReservedQuantity -= quantity;
        StockQuantity -= quantity;
    }

    public void UpdatePricing(decimal regularPrice, decimal? salePrice)
    {
        DomainGuard.NonNegative(regularPrice, nameof(regularPrice), "قیمت اصلی نمی‌تواند منفی باشد.");
        if (salePrice is < 0 || salePrice > regularPrice)
            throw new ArgumentOutOfRangeException(nameof(salePrice), salePrice, "قیمت تخفیف باید بین صفر و قیمت اصلی باشد.");

        RegularPrice = regularPrice;
        SalePrice = salePrice;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
