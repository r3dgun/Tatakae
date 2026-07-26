using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Payments;

public sealed class CreatePaymentRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required, RegularExpression("^(OnlineGateway|CardToCard|CashOnDelivery)$")]
    public string Method { get; set; } = "OnlineGateway";

}


public sealed class CreateZarinpalRefundRequest
{
    [Range(typeof(decimal), "1", "9999999999999999")]
    public decimal Amount { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = "بازپرداخت سفارش مشتری";
}

public sealed record PaymentRefundDto(
    Guid Id,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status,
    string StatusLabel,
    string Reason,
    string? ReferenceNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);

public sealed class UpdatePaymentStatusRequest
{
    [Required, RegularExpression("^(Pending|Succeeded|Failed|CancelledByUser|Verified|Reversed)$")]
    public string Status { get; set; } = "Verified";

    [StringLength(120)]
    public string? RefId { get; set; }

    [StringLength(120)]
    public string? TraceNumber { get; set; }

    [StringLength(1000)]
    public string? GatewayMessage { get; set; }
}

public sealed record PaymentInitDto(
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    string Method,
    string Gateway,
    decimal Amount,
    string CurrencyCode,
    string? RedirectUrl,
    string Status,
    string StatusLabel,
    bool RequiresManualReview);

public sealed record PaymentReceiptDto(
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string StatusLabel,
    string? RefId,
    string? TraceNumber,
    decimal Amount,
    DateTimeOffset? PaidAt,
    string Message);

public sealed record PaymentTransactionDto(
    Guid Id,
    string Status,
    string StatusLabel,
    decimal Amount,
    string? GatewayReference,
    string? RawGatewayResponse,
    DateTimeOffset CreatedAt);

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    string CustomerMobile,
    string Method,
    string MethodLabel,
    string Gateway,
    string Status,
    string StatusLabel,
    decimal Amount,
    string? GatewayAuthority,
    string? ReferenceId,
    string? TraceNumber,
    string? MaskedCardNumber,
    string? GatewayMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    IReadOnlyCollection<PaymentTransactionDto> Transactions,
    IReadOnlyCollection<PaymentRefundDto> Refunds);

public sealed record AdminPaymentSummaryDto(
    int PendingCount,
    int VerifiedCount,
    int FailedCount,
    decimal VerifiedAmount,
    decimal PendingAmount,
    decimal RefundedAmount);

public sealed record AdminPaymentsDto(AdminPaymentSummaryDto Summary, IReadOnlyCollection<PaymentDto> Payments);

public sealed record ZarinpalConfigurationStatusDto(
    bool Enabled,
    string Mode,
    bool MerchantConfigured,
    bool RequestReady,
    bool RefundEnabled,
    bool RefundReady,
    string Currency,
    string CallbackUrl,
    string ApiHost,
    string StartPayHost,
    IReadOnlyCollection<string> Issues);
