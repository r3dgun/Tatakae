using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tatakae.Domain.Enums;

namespace Tatakae.Infrastructure.Persistence.Models;

[Table("IranianProvinces")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Slug), IsUnique = true)]
public sealed class IranianProvinceDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(90)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(110)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneCode { get; set; }

    public bool IsActive { get; set; } = true;

    [InverseProperty(nameof(IranianCityDbRecord.Province))]
    public List<IranianCityDbRecord> Cities { get; set; } = [];
}

[Table("IranianCities")]
[Index(nameof(ProvinceId), nameof(Name), IsUnique = true)]
[Index(nameof(Slug))]
public sealed class IranianCityDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProvinceId { get; set; }

    [Required, MaxLength(90)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    public bool SupportsSameDayDelivery { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ProvinceId))]
    public IranianProvinceDbRecord? Province { get; set; }
}

[Table("Brands")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(IsActive), nameof(IsIranianBrand))]
public sealed class BrandDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(180)]
    public string? PersianName { get; set; }

    [MaxLength(1000), Url]
    public string? LogoUrl { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public bool IsIranianBrand { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("Sellers")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(Mobile), IsUnique = true)]
[Index(nameof(NationalId))]
[Index(nameof(EconomicCode))]
[Index(nameof(Status))]
public sealed class SellerDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(180)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public SellerType Type { get; set; } = SellerType.OwnStore;

    [Required, MaxLength(40)]
    public SellerStatus Status { get; set; } = SellerStatus.Active;

    [Required, MaxLength(20), Phone]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(260), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? NationalCode { get; set; }

    [MaxLength(20)]
    public string? NationalId { get; set; }

    [MaxLength(30)]
    public string? EconomicCode { get; set; }

    [MaxLength(30)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(1200)]
    public string? AddressLine { get; set; }

    [MaxLength(90)]
    public string? Province { get; set; }

    [MaxLength(90)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(34)]
    public string? Iban { get; set; }

    [MaxLength(30)]
    public string? BankCardNumber { get; set; }

    [Precision(5, 2)]
    public decimal CommissionPercent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [InverseProperty(nameof(ProductOfferDbRecord.Seller))]
    public List<ProductOfferDbRecord> Offers { get; set; } = [];
}

[Table("Warranties")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(Type), nameof(IsActive))]
public sealed class WarrantyDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public WarrantyType Type { get; set; } = WarrantyType.SellerWarranty;

    [Range(0, 120)]
    public int DurationMonths { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    public bool IsActive { get; set; } = true;
}

[Table("ProductOffers")]
[Index(nameof(ProductVariantId), nameof(SellerId), IsUnique = true)]
[Index(nameof(IsActive), nameof(IsBuyBoxWinner))]
[Index(nameof(Price), nameof(SalePrice))]
public sealed class ProductOfferDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid ProductVariantId { get; set; }

    [Required]
    public Guid SellerId { get; set; }

    public Guid? WarrantyId { get; set; }

    [Precision(18, 2)]
    public decimal Price { get; set; }

    [Precision(18, 2)]
    public decimal? SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, 90)]
    public int PreparationDays { get; set; }

    [MaxLength(120)]
    public string? SellerSku { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsBuyBoxWinner { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariantDbRecord? ProductVariant { get; set; }

    [ForeignKey(nameof(SellerId))]
    public SellerDbRecord? Seller { get; set; }

    [ForeignKey(nameof(WarrantyId))]
    public WarrantyDbRecord? Warranty { get; set; }
}

[Table("CustomerBankCards")]
[Index(nameof(CustomerId))]
[Index(nameof(MaskedCardNumber))]
public sealed class CustomerBankCardDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required, MaxLength(30)]
    public string MaskedCardNumber { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string OwnerName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? NationalCode { get; set; }

    public bool IsDefaultForRefund { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }
}

[Table("ApplicationUsers")]
[Index(nameof(Mobile), IsUnique = true)]
[Index(nameof(Email))]
public sealed class ApplicationUserDbRecord : BaseEntity<Guid>
{
    public Guid? CustomerId { get; set; }
    public Guid? SellerId { get; set; }

    [Required, MaxLength(20), Phone]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(260), EmailAddress]
    public string? Email { get; set; }

    [Required, MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    public bool MobileConfirmed { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [ForeignKey(nameof(SellerId))]
    public SellerDbRecord? Seller { get; set; }

    [InverseProperty(nameof(ApplicationUserRoleDbRecord.User))]
    public List<ApplicationUserRoleDbRecord> Roles { get; set; } = [];
}

[Table("ApplicationUserRoles")]
[Index(nameof(UserId), nameof(Role), IsUnique = true)]
public sealed class ApplicationUserRoleDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(40)]
    public UserRoleName Role { get; set; } = UserRoleName.Customer;

    [ForeignKey(nameof(UserId))]
    public ApplicationUserDbRecord? User { get; set; }
}

[Table("OtpCodes")]
[Index(nameof(Mobile), nameof(ExpiresAt))]
public sealed class OtpCodeDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(20), Phone]
    public string Mobile { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string CodeHash { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public IranianAuthProvider Provider { get; set; } = IranianAuthProvider.SmsOtp;

    [Range(0, 10)]
    public int TryCount { get; set; }

    public bool IsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

[Table("Wallets")]
[Index(nameof(CustomerId), IsUnique = true)]
public sealed class WalletDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid CustomerId { get; set; }

    [Precision(18, 2)]
    public decimal Balance { get; set; }

    [Precision(18, 2)]
    public decimal BlockedBalance { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [InverseProperty(nameof(WalletTransactionDbRecord.Wallet))]
    public List<WalletTransactionDbRecord> Transactions { get; set; } = [];
}

[Table("WalletTransactions")]
[Index(nameof(WalletId), nameof(CreatedAt))]
[Index(nameof(OrderId))]
public sealed class WalletTransactionDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid WalletId { get; set; }

    public Guid? OrderId { get; set; }

    [Required, MaxLength(40)]
    public WalletTransactionType Type { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(18, 2)]
    public decimal BalanceAfter { get; set; }

    [MaxLength(120)]
    public string? ReferenceNumber { get; set; }

    [Required, MaxLength(700)]
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(WalletId))]
    public WalletDbRecord? Wallet { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }
}

[Table("Payments")]
[Index(nameof(OrderId))]
[Index(nameof(Method), nameof(Gateway))]
[Index(nameof(Status))]
public sealed class PaymentDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid OrderId { get; set; }

    [Required, MaxLength(40)]
    public PaymentMethod Method { get; set; } = PaymentMethod.OnlineGateway;

    [Required, MaxLength(60)]
    public IranianPaymentGateway Gateway { get; set; } = IranianPaymentGateway.Zarinpal;

    [Required, MaxLength(40)]
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [MaxLength(120)]
    public string? GatewayAuthority { get; set; }

    [MaxLength(120)]
    public string? ReferenceId { get; set; }

    [MaxLength(120)]
    public string? TraceNumber { get; set; }

    [MaxLength(30)]
    public string? MaskedCardNumber { get; set; }

    [MaxLength(1000)]
    public string? GatewayMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }

    [InverseProperty(nameof(PaymentTransactionDbRecord.Payment))]
    public List<PaymentTransactionDbRecord> Transactions { get; set; } = [];

    [InverseProperty(nameof(RefundDbRecord.Payment))]
    public List<RefundDbRecord> Refunds { get; set; } = [];
}

[Table("PaymentTransactions")]
[Index(nameof(PaymentId), nameof(CreatedAt))]
[Index(nameof(Status))]
public sealed class PaymentTransactionDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid PaymentId { get; set; }

    [Required, MaxLength(40)]
    public PaymentTransactionStatus Status { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [MaxLength(120)]
    public string? GatewayReference { get; set; }

    [MaxLength(2000)]
    public string? RawGatewayResponse { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public PaymentDbRecord? Payment { get; set; }
}

[Table("Refunds")]
[Index(nameof(OrderId))]
[Index(nameof(Status))]
public sealed class RefundDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid OrderId { get; set; }

    public Guid? PaymentId { get; set; }

    [Required, MaxLength(40)]
    public RefundStatus Status { get; set; } = RefundStatus.Requested;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Required, MaxLength(700)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? DestinationMaskedCardNumber { get; set; }

    [MaxLength(120)]
    public string? ReferenceNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public PaymentDbRecord? Payment { get; set; }
}

[Table("ShippingMethods")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Carrier), nameof(IsActive))]
[Index(nameof(IsActive), nameof(SortOrder))]
public sealed class ShippingMethodDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(60)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(600)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public ShippingCarrier Carrier { get; set; } = ShippingCarrier.Post;

    [Precision(18, 2)]
    public decimal BasePrice { get; set; }

    [Precision(18, 2)]
    public decimal? FreeShippingThreshold { get; set; }

    [Range(0, 30)]
    public int MinDeliveryDays { get; set; }

    [Range(0, 60)]
    public int MaxDeliveryDays { get; set; }

    public bool SupportsCashOnDelivery { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    [Range(0, 9999)]
    public int SortOrder { get; set; }

    [MaxLength(1200)]
    public string? AdminNote { get; set; }
}

[Table("ShippingZones")]
[Index(nameof(ShippingMethodId), nameof(Province), nameof(City))]
public sealed class ShippingZoneDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ShippingMethodId { get; set; }

    [Required, MaxLength(90)]
    public string Province { get; set; } = string.Empty;

    [MaxLength(90)]
    public string? City { get; set; }

    [Precision(18, 2)]
    public decimal ExtraPrice { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ShippingMethodId))]
    public ShippingMethodDbRecord? ShippingMethod { get; set; }
}

[Table("Shipments")]
[Index(nameof(OrderId))]
[Index(nameof(TrackingCode))]
[Index(nameof(Status))]
public sealed class ShipmentDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid OrderId { get; set; }

    public Guid? ShippingMethodId { get; set; }

    [Required, MaxLength(40)]
    public ShippingCarrier Carrier { get; set; } = ShippingCarrier.Post;

    [Required, MaxLength(40)]
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Draft;

    [MaxLength(120)]
    public string? TrackingCode { get; set; }

    [MaxLength(120)]
    public string? TrackingUrl { get; set; }

    [Precision(18, 2)]
    public decimal ShippingCost { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }

    [ForeignKey(nameof(ShippingMethodId))]
    public ShippingMethodDbRecord? ShippingMethod { get; set; }

    [InverseProperty(nameof(ShipmentEventDbRecord.Shipment))]
    public List<ShipmentEventDbRecord> Events { get; set; } = [];
}

[Table("ShipmentEvents")]
[Index(nameof(ShipmentId), nameof(EventAt))]
public sealed class ShipmentEventDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ShipmentId { get; set; }

    [Required, MaxLength(40)]
    public ShipmentStatus Status { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(90)]
    public string? City { get; set; }

    public DateTimeOffset EventAt { get; set; }

    [ForeignKey(nameof(ShipmentId))]
    public ShipmentDbRecord? Shipment { get; set; }
}

[Table("Invoices")]
[Index(nameof(OrderId), IsUnique = true)]
[Index(nameof(InvoiceNumber), IsUnique = true)]
[Index(nameof(Status))]
public sealed class InvoiceDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid OrderId { get; set; }

    [Required, MaxLength(40)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public InvoiceType Type { get; set; } = InvoiceType.Informal;

    [Required, MaxLength(40)]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

    [MaxLength(180)]
    public string? BuyerLegalName { get; set; }

    [MaxLength(20)]
    public string? BuyerNationalId { get; set; }

    [MaxLength(30)]
    public string? BuyerEconomicCode { get; set; }

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(18, 2)]
    public decimal VatAmount { get; set; }

    [Precision(18, 2)]
    public decimal ShippingAmount { get; set; }

    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }

    [InverseProperty(nameof(InvoiceLineDbRecord.Invoice))]
    public List<InvoiceLineDbRecord> Lines { get; set; } = [];
}

[Table("InvoiceLines")]
[Index(nameof(InvoiceId))]
public sealed class InvoiceLineDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required, MaxLength(240)]
    public string Title { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(18, 2)]
    public decimal VatAmount { get; set; }

    [Precision(18, 2)]
    public decimal LineTotal { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public InvoiceDbRecord? Invoice { get; set; }
}

[Table("ReturnRequests")]
[Index(nameof(OrderId))]
[Index(nameof(Status))]
[Index(nameof(RequestNumber), IsUnique = true)]
public sealed class ReturnRequestDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(40)]
    public string RequestNumber { get; set; } = string.Empty;

    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required, MaxLength(40)]
    public ReturnRequestStatus Status { get; set; } = ReturnRequestStatus.Requested;

    [Required, MaxLength(40)]
    public ReturnReason Reason { get; set; } = ReturnReason.Other;

    [Required, MaxLength(1400)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? ReturnTrackingCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [InverseProperty(nameof(ReturnRequestLineDbRecord.ReturnRequest))]
    public List<ReturnRequestLineDbRecord> Lines { get; set; } = [];
}

[Table("ReturnRequestLines")]
[Index(nameof(ReturnRequestId))]
[Index(nameof(OrderLineId))]
public sealed class ReturnRequestLineDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ReturnRequestId { get; set; }

    [Required]
    public Guid OrderLineId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Precision(18, 2)]
    public decimal RequestedRefundAmount { get; set; }

    [ForeignKey(nameof(ReturnRequestId))]
    public ReturnRequestDbRecord? ReturnRequest { get; set; }

    [ForeignKey(nameof(OrderLineId))]
    public OrderLineDbRecord? OrderLine { get; set; }
}

[Table("Warehouses")]
[Index(nameof(Code), IsUnique = true)]
public sealed class WarehouseDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(90)]
    public string Province { get; set; } = string.Empty;

    [Required, MaxLength(90)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(900)]
    public string AddressLine { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

[Table("StockItems")]
[Index(nameof(WarehouseId), nameof(ProductVariantId), IsUnique = true)]
public sealed class StockItemDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid WarehouseId { get; set; }

    [Required]
    public Guid ProductVariantId { get; set; }

    [Range(0, int.MaxValue)]
    public int OnHandQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReservedQuantity { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public WarehouseDbRecord? Warehouse { get; set; }

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariantDbRecord? ProductVariant { get; set; }
}

[Table("InventoryTransactions")]
[Index(nameof(ProductVariantId), nameof(CreatedAt))]
[Index(nameof(OrderId))]
public sealed class InventoryTransactionDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductVariantId { get; set; }

    public Guid? WarehouseId { get; set; }
    public Guid? OrderId { get; set; }

    [Required, MaxLength(40)]
    public StockTransactionType Type { get; set; }

    public int QuantityDelta { get; set; }

    [MaxLength(700)]
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariantDbRecord? ProductVariant { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public WarehouseDbRecord? Warehouse { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }
}

[Table("InventoryReservations")]
[Index(nameof(OrderId))]
[Index(nameof(ProductVariantId))]
[Index(nameof(Status), nameof(ExpiresAt))]
public sealed class InventoryReservationDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductVariantId { get; set; }

    [Required]
    public Guid OrderId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, MaxLength(40)]
    public InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.Reserved;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariantDbRecord? ProductVariant { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }
}

[Table("MediaAssets")]
[Index(nameof(UsageType))]
[Index(nameof(OwnerEntityId))]
public sealed class MediaAssetDbRecord : BaseEntity<Guid>
{
    public Guid? OwnerEntityId { get; set; }

    [Required, MaxLength(40)]
    public MediaUsageType UsageType { get; set; } = MediaUsageType.ProductImage;

    [Required, MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    [Required, MaxLength(1200), Url]
    public string Url { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? AltText { get; set; }

    [Range(0, long.MaxValue)]
    public long SizeBytes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}


[Table("EmbroideryArtworks")]
[Index(nameof(CustomerId))]
[Index(nameof(ProductId))]
[Index(nameof(OrderId))]
[Index(nameof(OrderLineId))]
[Index(nameof(Status))]
[Index(nameof(MediaAssetId), IsUnique = true)]
public sealed class EmbroideryArtworkDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid MediaAssetId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? OrderLineId { get; set; }

    [Required, MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    [Required, MaxLength(1200), Url]
    public string FileUrl { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long SizeBytes { get; set; }

    [Required, MaxLength(40)]
    public EmbroideryArtworkStatus Status { get; set; } = EmbroideryArtworkStatus.PendingReview;

    [Precision(9, 2)]
    public decimal? WidthCm { get; set; }

    [Precision(9, 2)]
    public decimal? HeightCm { get; set; }

    [Range(1, 24)]
    public int? ThreadColorCount { get; set; }

    [MaxLength(1200)]
    public string? CustomerNote { get; set; }

    [MaxLength(1200)]
    public string? AdminNote { get; set; }

    [MaxLength(1200)]
    public string? RejectionReason { get; set; }

    [MaxLength(1200), Url]
    public string? PreviewImageUrl { get; set; }

    [MaxLength(20)]
    public string? ProductionFileExtension { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    [ForeignKey(nameof(MediaAssetId))]
    public MediaAssetDbRecord? MediaAsset { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderDbRecord? Order { get; set; }

    [ForeignKey(nameof(OrderLineId))]
    public OrderLineDbRecord? OrderLine { get; set; }
}

[Table("ProductReviews")]
[Index(nameof(ProductId), nameof(Status))]
[Index(nameof(CustomerId))]
public sealed class ProductReviewDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    public Guid? OrderLineId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(3000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(1500)]
    public string? PositivePointsCsv { get; set; }

    [MaxLength(1500)]
    public string? NegativePointsCsv { get; set; }

    [MaxLength(1500)]
    public string? AdminReply { get; set; }

    [MaxLength(700)]
    public string? ModerationNote { get; set; }

    public DateTimeOffset? RepliedAt { get; set; }

    [Required, MaxLength(40)]
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    public bool RecommendProduct { get; set; } = true;
    public bool IsBuyer { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [ForeignKey(nameof(OrderLineId))]
    public OrderLineDbRecord? OrderLine { get; set; }
}

[Table("ProductQuestions")]
[Index(nameof(ProductId), nameof(Status))]
[Index(nameof(CustomerId))]
public sealed class ProductQuestionDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid ProductId { get; set; }

    public Guid? CustomerId { get; set; }

    [Required, MaxLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Mobile { get; set; }

    [Required, MaxLength(1200)]
    public string QuestionText { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? AnswerText { get; set; }

    public Guid? AnsweredByUserId { get; set; }

    [Required, MaxLength(40)]
    public QuestionStatus Status { get; set; } = QuestionStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }

    [MaxLength(700)]
    public string? ModerationNote { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }
}

[Table("Carts")]
[Index(nameof(CustomerId))]
[Index(nameof(AnonymousId))]
public sealed class CartDbRecord : BaseEntity<Guid>
{
    public Guid? CustomerId { get; set; }

    [MaxLength(120)]
    public string? AnonymousId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [InverseProperty(nameof(CartItemDbRecord.Cart))]
    public List<CartItemDbRecord> Items { get; set; } = [];
}

[Table("CartItems")]
[Index(nameof(CartId))]
[Index(nameof(ProductVariantId))]
public sealed class CartItemDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid CartId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid ProductVariantId { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; }

    [MaxLength(4000)]
    public string? EmbroideryConfigurationJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(CartId))]
    public CartDbRecord? Cart { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }

    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariantDbRecord? ProductVariant { get; set; }
}

[Table("Wishlists")]
[Index(nameof(CustomerId), nameof(ProductId), IsUnique = true)]
public sealed class WishlistDbRecord : BaseEntity<Guid>
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerDbRecord? Customer { get; set; }

    [ForeignKey(nameof(ProductId))]
    public ProductDbRecord? Product { get; set; }
}

[Table("DiscountCampaigns")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(IsActive), nameof(StartsAt), nameof(EndsAt))]
public sealed class DiscountCampaignDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("SeoRedirects")]
[Index(nameof(FromPath), IsUnique = true)]
public sealed class SeoRedirectDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(700)]
    public string FromPath { get; set; } = string.Empty;

    [Required, MaxLength(700)]
    public string ToPath { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public SeoRedirectType Type { get; set; } = SeoRedirectType.Permanent301;

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

[Table("UrlSlugHistories")]
[Index(nameof(EntityType), nameof(OldSlug), IsUnique = true)]
public sealed class UrlSlugHistoryDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public Guid EntityId { get; set; }

    [Required, MaxLength(260)]
    public string OldSlug { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string NewSlug { get; set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; set; }
}

[Table("AuditLogs")]
[Index(nameof(EntityType), nameof(EntityId))]
[Index(nameof(ActorUserId), nameof(CreatedAt))]
public sealed class AuditLogDbRecord : BaseEntity<Guid>
{
    public Guid? ActorUserId { get; set; }

    [Required, MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    [Required, MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? BeforeJson { get; set; }

    [MaxLength(4000)]
    public string? AfterJson { get; set; }

    [MaxLength(60)]
    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}


[Table("StorePolicyPages")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(IsPublished), nameof(SortOrder))]
public sealed class StorePolicyPageDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Summary { get; set; } = string.Empty;

    [Required, MaxLength(12000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(180)]
    public string? SeoTitle { get; set; }

    [MaxLength(320)]
    public string? SeoDescription { get; set; }

    public bool IsPublished { get; set; } = true;

    [Range(0, 9999)]
    public int SortOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}


[Table("ContactMessages")]
[Index(nameof(Status), nameof(CreatedAt))]
[Index(nameof(Mobile))]
public sealed class ContactMessageDbRecord : BaseEntity<Guid>
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(260), EmailAddress]
    public string? Email { get; set; }

    [Required, MaxLength(160)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Status { get; set; } = "new";

    [MaxLength(1000)]
    public string? AdminNote { get; set; }

    [MaxLength(60)]
    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SeenAt { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
}
