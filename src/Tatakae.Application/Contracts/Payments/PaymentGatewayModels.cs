using Tatakae.Domain.Enums;

namespace Tatakae.Application.Contracts.Payments;

/// <summary>
/// Data sent by the payment use case to the external Zarinpal adapter.
/// The adapter owns HTTP, JSON and endpoint details; Application owns the use case.
/// </summary>
public sealed record ZarinpalPaymentRequest(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Description,
    string CustomerMobile,
    string OrderNumber);

public sealed record ZarinpalRequestResult(
    bool IsSuccessful,
    int Code,
    string Message,
    string? Authority,
    string? RedirectUrl,
    string RawResponse);

public sealed record ZarinpalVerifyRequest(
    decimal Amount,
    string Currency,
    string Authority);

public sealed record ZarinpalVerifyResult(
    bool IsSuccessful,
    bool WasAlreadyVerified,
    int Code,
    string Message,
    long? ReferenceId,
    string? CardPan,
    long? Fee,
    string RawResponse);

public sealed record ZarinpalReverseRequest(string Authority);

public sealed record ZarinpalReverseResult(
    bool IsSuccessful,
    bool WasAlreadyReversed,
    int Code,
    string Message,
    string RawResponse);

public sealed record ZarinpalRefundRequest(
    string SessionId,
    decimal Amount,
    string Currency,
    string Description,
    string Reason = "CUSTOMER_REQUEST");

public sealed record ZarinpalRefundResult(
    bool IsSuccessful,
    bool IsCompleted,
    string? ErrorCode,
    string Message,
    string? RefundId,
    decimal? RefundedAmount,
    string? ProviderStatus,
    string RawResponse);

/// <summary>
/// Persistence commands contain decisions already made by Application/Domain.
/// Infrastructure may store these values, but must not decide order transitions.
/// </summary>
public sealed record CreatePaymentRecord(
    Guid PaymentId,
    Guid OrderId,
    PaymentMethod Method,
    IranianPaymentGateway Gateway,
    decimal Amount,
    string Message,
    DateTimeOffset CreatedAt);

public sealed record CreatePaymentResult(
    PaymentDto Payment,
    bool WasCreated);

public sealed record OrderPaymentState(
    Guid OrderId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string? TrackingCode,
    string? AdminNote);

public sealed record PersistPaymentOutcome(
    Guid PaymentId,
    PaymentTransactionStatus Status,
    string? Authority,
    string? ReferenceId,
    string? TraceNumber,
    string? MaskedCardNumber,
    string Message,
    string? RawGatewayResponse,
    DateTimeOffset OccurredAt,
    DateTimeOffset? PaidAt,
    string ChangedBy,
    string? OrderHistoryTitle,
    string? OrderHistoryNote,
    OrderPaymentState? OrderState,
    OrderStatus? PreviousOrderStatus);

public sealed record CreatePaymentRefundRecord(
    Guid RefundId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    decimal MaximumTotalRefundAmount,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record CreatePaymentRefundResult(
    PaymentRefundDto Refund,
    bool WasCreated,
    decimal CompletedAmountBeforeCreation);

public sealed record PersistPaymentRefundOutcome(
    Guid RefundId,
    RefundStatus Status,
    string Message,
    string? ReferenceNumber,
    string? RawGatewayResponse,
    DateTimeOffset OccurredAt,
    DateTimeOffset? PaidAt,
    PaymentTransactionStatus? NewPaymentStatus,
    PaymentTransactionStatus? TransactionStatus,
    OrderPaymentState? OrderState,
    OrderStatus? PreviousOrderStatus,
    string ChangedBy);
