using System.ComponentModel.DataAnnotations;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Contracts.IranCommerce;

public sealed record IranianProvinceDto(Guid Id, string Name, string Slug, bool IsActive, IReadOnlyList<IranianCityDto> Cities);
public sealed record IranianCityDto(Guid Id, Guid ProvinceId, string Name, string Slug, bool SupportsSameDayDelivery, bool IsActive);

public sealed class UpsertIranianProvinceRequest
{
    [Required, StringLength(90, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(110, MinimumLength = 2), RegularExpression("^[a-z0-9-]+$")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneCode { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class UpsertIranianCityRequest
{
    [Required]
    public Guid ProvinceId { get; set; }

    [Required, StringLength(90, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2), RegularExpression("^[a-z0-9-]+$")]
    public string Slug { get; set; } = string.Empty;

    public bool SupportsSameDayDelivery { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record BrandDto(Guid Id, string Name, string Slug, string? PersianName, string? LogoUrl, bool IsIranianBrand, bool IsActive);

public sealed class UpsertBrandRequest
{
    [Required, StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(220, MinimumLength = 2), RegularExpression("^[a-z0-9-]+$")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(180)]
    public string? PersianName { get; set; }

    [Url, StringLength(1000)]
    public string? LogoUrl { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    public bool IsIranianBrand { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record SellerDto(
    Guid Id,
    string DisplayName,
    string Slug,
    SellerType Type,
    SellerStatus Status,
    string Mobile,
    string? Email,
    string? NationalCode,
    string? NationalId,
    string? EconomicCode,
    decimal CommissionPercent);

public sealed class UpsertSellerRequest
{
    [Required, StringLength(180, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(220, MinimumLength = 2), RegularExpression("^[a-z0-9-]+$")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public SellerType Type { get; set; } = SellerType.OwnStore;

    [Required]
    public SellerStatus Status { get; set; } = SellerStatus.Active;

    [Required, Phone, StringLength(20), RegularExpression("^09[0-9]{9}$")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress, StringLength(260)]
    public string? Email { get; set; }

    [StringLength(10, MinimumLength = 10), RegularExpression("^[0-9]{10}$")]
    public string? NationalCode { get; set; }

    [StringLength(20)]
    public string? NationalId { get; set; }

    [StringLength(30)]
    public string? EconomicCode { get; set; }

    [StringLength(30)]
    public string? RegistrationNumber { get; set; }

    [StringLength(1200)]
    public string? AddressLine { get; set; }

    [StringLength(90)]
    public string? Province { get; set; }

    [StringLength(90)]
    public string? City { get; set; }

    [StringLength(10, MinimumLength = 10), RegularExpression("^[0-9]{10}$")]
    public string? PostalCode { get; set; }

    [StringLength(34), RegularExpression("^IR[0-9]{24}$")]
    public string? Iban { get; set; }

    [StringLength(16, MinimumLength = 16), RegularExpression("^[0-9]{16}$")]
    public string? BankCardNumber { get; set; }

    [Range(0, 100)]
    public decimal CommissionPercent { get; set; }
}

public sealed record WarrantyDto(Guid Id, string Title, string Slug, WarrantyType Type, int DurationMonths, string? Terms, bool IsActive);

public sealed class UpsertWarrantyRequest
{
    [Required, StringLength(180, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(220, MinimumLength = 2), RegularExpression("^[a-z0-9-]+$")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public WarrantyType Type { get; set; } = WarrantyType.SellerWarranty;

    [Range(0, 120)]
    public int DurationMonths { get; set; }

    [StringLength(2000)]
    public string? Terms { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record ProductOfferDto(
    Guid Id,
    Guid ProductId,
    Guid ProductVariantId,
    Guid SellerId,
    string SellerName,
    Guid? WarrantyId,
    string? WarrantyTitle,
    decimal Price,
    decimal? SalePrice,
    int StockQuantity,
    int PreparationDays,
    bool IsActive,
    bool IsBuyBoxWinner);

public sealed class UpsertProductOfferRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid ProductVariantId { get; set; }

    [Required]
    public Guid SellerId { get; set; }

    public Guid? WarrantyId { get; set; }

    [Range(0, 999999999999)]
    public decimal Price { get; set; }

    [Range(0, 999999999999)]
    public decimal? SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, 90)]
    public int PreparationDays { get; set; }

    [StringLength(120)]
    public string? SellerSku { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsBuyBoxWinner { get; set; }
}

public sealed record IranianAddressDto(
    string RecipientName,
    string Mobile,
    string Province,
    string City,
    string PostalCode,
    string AddressLine,
    string? Plaque,
    string? Unit);

public sealed class IranianAddressRequest
{
    [Required, StringLength(180, MinimumLength = 2)]
    public string RecipientName { get; set; } = string.Empty;

    [Required, Phone, StringLength(20), RegularExpression("^09[0-9]{9}$")]
    public string Mobile { get; set; } = string.Empty;

    [Required, StringLength(90, MinimumLength = 2)]
    public string Province { get; set; } = string.Empty;

    [Required, StringLength(90, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(10, MinimumLength = 10), RegularExpression("^[0-9]{10}$")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(900, MinimumLength = 10)]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Plaque { get; set; }

    [StringLength(30)]
    public string? Unit { get; set; }
}

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    PaymentMethod Method,
    IranianPaymentGateway Gateway,
    PaymentTransactionStatus Status,
    decimal Amount,
    string? GatewayAuthority,
    string? ReferenceId,
    string? TraceNumber,
    string? MaskedCardNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);

public sealed class StartIranianPaymentRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public PaymentMethod Method { get; set; } = PaymentMethod.OnlineGateway;

    [Required]
    public IranianPaymentGateway Gateway { get; set; } = IranianPaymentGateway.Zarinpal;

    [Range(1000, 999999999999)]
    public decimal Amount { get; set; }

    [Url, StringLength(1000)]
    public string? CallbackUrl { get; set; }
}

public sealed class VerifyIranianPaymentRequest
{
    [Required]
    public Guid PaymentId { get; set; }

    [Required, StringLength(120)]
    public string GatewayAuthority { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ReferenceId { get; set; }

    [StringLength(120)]
    public string? TraceNumber { get; set; }
}

public sealed record ShippingMethodDto(Guid Id, string Title, ShippingCarrier Carrier, decimal BasePrice, decimal? FreeShippingThreshold, int MinDeliveryDays, int MaxDeliveryDays, bool SupportsCashOnDelivery, bool IsActive);

public sealed class UpsertShippingMethodRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public ShippingCarrier Carrier { get; set; } = ShippingCarrier.Post;

    [Range(0, 999999999)]
    public decimal BasePrice { get; set; }

    [Range(0, 999999999)]
    public decimal? FreeShippingThreshold { get; set; }

    [Range(0, 30)]
    public int MinDeliveryDays { get; set; }

    [Range(0, 60)]
    public int MaxDeliveryDays { get; set; }

    public bool SupportsCashOnDelivery { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record ShipmentDto(Guid Id, Guid OrderId, ShippingCarrier Carrier, ShipmentStatus Status, string? TrackingCode, string? TrackingUrl, decimal ShippingCost, DateTimeOffset CreatedAt, DateTimeOffset? ShippedAt, DateTimeOffset? DeliveredAt);

public sealed class CreateShipmentRequest
{
    [Required]
    public Guid OrderId { get; set; }

    public Guid? ShippingMethodId { get; set; }

    [Required]
    public ShippingCarrier Carrier { get; set; } = ShippingCarrier.Post;

    [Range(0, 999999999)]
    public decimal ShippingCost { get; set; }

    [StringLength(120)]
    public string? TrackingCode { get; set; }

    [Url, StringLength(120)]
    public string? TrackingUrl { get; set; }
}

public sealed class UpdateShipmentStatusRequest
{
    [Required]
    public ShipmentStatus Status { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [StringLength(90)]
    public string? City { get; set; }
}

public sealed record InvoiceDto(Guid Id, Guid OrderId, string InvoiceNumber, InvoiceType Type, InvoiceStatus Status, decimal Subtotal, decimal DiscountAmount, decimal VatAmount, decimal ShippingAmount, decimal TotalAmount, DateTimeOffset IssuedAt);

public sealed class IssueIranianInvoiceRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public InvoiceType Type { get; set; } = InvoiceType.Informal;

    [StringLength(180)]
    public string? BuyerLegalName { get; set; }

    [StringLength(20)]
    public string? BuyerNationalId { get; set; }

    [StringLength(30)]
    public string? BuyerEconomicCode { get; set; }

    [Range(0, 100)]
    public decimal VatPercent { get; set; }
}

public sealed record ReturnRequestDto(Guid Id, string RequestNumber, Guid OrderId, Guid CustomerId, ReturnRequestStatus Status, ReturnReason Reason, string Description, string? ReturnTrackingCode, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt);

public sealed class CreateReturnRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public ReturnReason Reason { get; set; } = ReturnReason.Other;

    [Required, StringLength(1400, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [MinLength(1)]
    public List<CreateReturnLineRequest> Lines { get; set; } = [];
}

public sealed class CreateReturnLineRequest
{
    [Required]
    public Guid OrderLineId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, 999999999)]
    public decimal RequestedRefundAmount { get; set; }
}

public sealed record WarehouseDto(Guid Id, string Code, string Name, string Province, string City, bool IsActive);

public sealed class UpsertWarehouseRequest
{
    [Required, StringLength(40, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(90, MinimumLength = 2)]
    public string Province { get; set; } = string.Empty;

    [Required, StringLength(90, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(900, MinimumLength = 10)]
    public string AddressLine { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class AdjustInventoryRequest
{
    [Required]
    public Guid ProductVariantId { get; set; }

    public Guid? WarehouseId { get; set; }

    [Required]
    public StockTransactionType Type { get; set; } = StockTransactionType.ManualAdjustment;

    public int QuantityDelta { get; set; }

    [StringLength(700)]
    public string? Note { get; set; }
}

public sealed record MediaAssetDto(Guid Id, MediaUsageType UsageType, string FileName, string ContentType, string Url, string? AltText, long SizeBytes, DateTimeOffset CreatedAt);

public sealed class CreateMediaAssetRequest
{
    public Guid? OwnerEntityId { get; set; }

    [Required]
    public MediaUsageType UsageType { get; set; } = MediaUsageType.ProductImage;

    [Required, StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = string.Empty;

    [Required, Url, StringLength(1200)]
    public string Url { get; set; } = string.Empty;

    [StringLength(260)]
    public string? AltText { get; set; }

    [Range(0, long.MaxValue)]
    public long SizeBytes { get; set; }
}

public sealed class CreateProductQuestionRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required, StringLength(1200, MinimumLength = 5)]
    public string QuestionText { get; set; } = string.Empty;
}

public sealed class AnswerProductQuestionRequest
{
    [Required]
    public Guid QuestionId { get; set; }

    [Required]
    public Guid AnsweredByUserId { get; set; }

    [Required, StringLength(2000, MinimumLength = 2)]
    public string AnswerText { get; set; } = string.Empty;
}

public sealed class CreateProductReviewRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    public Guid? OrderLineId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(180, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(3000, MinimumLength = 10)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(10)]
    public List<string> PositivePoints { get; set; } = [];

    [MaxLength(10)]
    public List<string> NegativePoints { get; set; } = [];
}

public sealed class CreateSeoRedirectRequest
{
    [Required, StringLength(700, MinimumLength = 2)]
    public string FromPath { get; set; } = string.Empty;

    [Required, StringLength(700, MinimumLength = 2)]
    public string ToPath { get; set; } = string.Empty;

    [Required]
    public SeoRedirectType Type { get; set; } = SeoRedirectType.Permanent301;

    public bool IsActive { get; set; } = true;
}
