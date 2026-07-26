using Microsoft.Extensions.Logging;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Services;

/// <summary>
/// Coordinates the payment use case. Zarinpal is an external adapter and the EF
/// repository only persists decisions made here and by the Order aggregate.
/// </summary>
public sealed class PaymentService(
    IZarinpalPaymentGateway gateway,
    IPaymentRepository payments,
    IOrderRepository orders,
    INotificationService notifications,
    ILogger<PaymentService> logger) : IPaymentService
{
    private static readonly TimeSpan PaymentRequestPreparationTimeout = TimeSpan.FromMinutes(5);

    public async Task<ResultDto<PaymentInitDto>> StartAsync(
        CreatePaymentRequest request,
        string? currentMobile,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<PaymentInitDto>();
        if (request is null)
            return result.ValidationFailed("اطلاعات پرداخت ارسال نشده است.", "payment_request_required");
        if (request.OrderId == Guid.Empty)
            return result.ValidationFailed("شناسه سفارش معتبر نیست.", "order_id_invalid");
        if (!TryParseMethod(request.Method, out var method))
            return result.ValidationFailed("روش پرداخت معتبر نیست.", "payment_method_invalid");

        try
        {
            var order = (await orders.GetByIdAsync(request.OrderId, cancellationToken)).RequireData();
            if (!OwnsOrder(order, currentMobile))
                return result.Forbidden("این سفارش متعلق به حساب فعلی نیست.", "payment_order_forbidden");
            if (order.PaymentStatus == PaymentStatus.Paid)
                return result.Conflict("این سفارش قبلاً پرداخت شده است.", "order_already_paid");
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
                return result.Conflict("برای سفارش لغوشده یا بازپرداخت‌شده نمی‌توان پرداخت جدید ایجاد کرد.", "order_not_payable");

            var existing = await payments.GetActiveForOrderAsync(order.Id, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.Method, method.ToString(), StringComparison.Ordinal))
                {
                    return result.Conflict(
                        "برای این سفارش یک روش پرداخت فعال دیگر ثبت شده است.",
                        "active_payment_method_mismatch");
                }

                var redirect = existing.Method == PaymentMethod.OnlineGateway.ToString() &&
                               !string.IsNullOrWhiteSpace(existing.GatewayAuthority)
                    ? gateway.GetRedirectUrl(existing.GatewayAuthority)
                    : null;

                if (existing.Method == PaymentMethod.OnlineGateway.ToString() &&
                    string.IsNullOrWhiteSpace(redirect))
                {
                    if (DateTimeOffset.UtcNow - existing.CreatedAt < PaymentRequestPreparationTimeout)
                    {
                        return result.Conflict(
                            "درخواست پرداخت این سفارش هنوز در حال آماده‌سازی است. چند لحظه دیگر دوباره تلاش کنید.",
                            "payment_request_in_progress");
                    }

                    await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                        existing.Id,
                        PaymentTransactionStatus.Failed,
                        existing.GatewayAuthority,
                        existing.ReferenceId,
                        existing.TraceNumber,
                        existing.MaskedCardNumber,
                        "درخواست پرداخت قبلی به علت تکمیل‌نشدن آماده‌سازی منقضی شد.",
                        "payment_initialization_timeout",
                        DateTimeOffset.UtcNow,
                        null,
                        "payment-service",
                        null,
                        null,
                        null,
                        null), cancellationToken);
                }
                else
                {
                    return result.Success(
                        "درخواست پرداخت فعال قبلی بازیابی شد.",
                        ToInit(existing, redirect));
                }
            }

            var now = DateTimeOffset.UtcNow;
            var creation = await payments.CreateAsync(new CreatePaymentRecord(
                Guid.NewGuid(),
                order.Id,
                method,
                method == PaymentMethod.OnlineGateway ? IranianPaymentGateway.Zarinpal : IranianPaymentGateway.None,
                order.Total,
                method == PaymentMethod.OnlineGateway
                    ? "درخواست پرداخت زرین‌پال ایجاد شد."
                    : "پرداخت دستی ثبت شد و باید توسط مدیر بررسی شود.",
                now), cancellationToken);
            var payment = creation.Payment;

            if (!creation.WasCreated)
            {
                var existingRedirect = payment.Method == PaymentMethod.OnlineGateway.ToString() &&
                                       !string.IsNullOrWhiteSpace(payment.GatewayAuthority)
                    ? gateway.GetRedirectUrl(payment.GatewayAuthority)
                    : null;

                if (payment.Method == PaymentMethod.OnlineGateway.ToString() &&
                    string.IsNullOrWhiteSpace(existingRedirect))
                {
                    return result.Conflict(
                        "درخواست پرداخت دیگری برای این سفارش در حال آماده‌سازی است. چند لحظه دیگر دوباره تلاش کنید.",
                        "payment_request_in_progress");
                }

                return result.Success(
                    "درخواست پرداخت فعال قبلی بازیابی شد.",
                    ToInit(payment, existingRedirect));
            }

            if (method != PaymentMethod.OnlineGateway)
                return result.Success("درخواست پرداخت دستی ثبت شد.", ToInit(payment, null));

            var gatewayResult = await gateway.RequestAsync(new ZarinpalPaymentRequest(
                payment.Id,
                payment.Amount,
                gateway.Currency,
                $"پرداخت سفارش {order.OrderNumber}",
                order.CustomerMobile,
                order.OrderNumber), cancellationToken);

            if (!gatewayResult.IsSuccessful || string.IsNullOrWhiteSpace(gatewayResult.Authority))
            {
                // Code 0 represents a timeout, network failure, malformed response, or
                // local configuration failure. For transport-level failures the provider
                // may still have created a payment session, so keep this payment active
                // briefly instead of creating duplicate Zarinpal requests immediately.
                var uncertain = gatewayResult.Code == 0 &&
                                gatewayResult.RawResponse is not (
                                    "zarinpal_configuration_error" or
                                    "zarinpal_invalid_request" or
                                    "zarinpal_amount_overflow");
                var failedPaymentStatus = uncertain
                    ? PaymentTransactionStatus.Pending
                    : PaymentTransactionStatus.Failed;

                await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                    payment.Id,
                    failedPaymentStatus,
                    null,
                    null,
                    null,
                    null,
                    gatewayResult.Message,
                    gatewayResult.RawResponse,
                    DateTimeOffset.UtcNow,
                    null,
                    "zarinpal-request",
                    null,
                    null,
                    null,
                    null), cancellationToken);

                logger.LogWarning(
                    "Zarinpal request failed or is uncertain. PaymentId={PaymentId} Code={Code} Message={Message}",
                    payment.Id,
                    gatewayResult.Code,
                    gatewayResult.Message);

                return result.Failed(
                    uncertain
                        ? "نتیجه ایجاد درخواست زرین‌پال قطعی نیست. چند لحظه دیگر دوباره وضعیت پرداخت را بررسی کنید."
                        : gatewayResult.Message,
                    ResultStatus.Failure,
                    uncertain
                        ? "zarinpal_request_uncertain"
                        : $"zarinpal_request_{gatewayResult.Code}");
            }

            var redirected = await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                payment.Id,
                PaymentTransactionStatus.RedirectedToGateway,
                gatewayResult.Authority,
                null,
                null,
                null,
                gatewayResult.Message,
                gatewayResult.RawResponse,
                DateTimeOffset.UtcNow,
                null,
                "zarinpal-request",
                null,
                null,
                null,
                null), cancellationToken);

            return result.Success(
                "درگاه زرین‌پال با موفقیت آماده شد.",
                ToInit(redirected, gatewayResult.RedirectUrl ?? gateway.GetRedirectUrl(gatewayResult.Authority)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ResultDtoException ex)
        {
            return result.Failed(ex);
        }
        catch (ArgumentException ex)
        {
            return result.ValidationFailed(ex.Message, "payment_start_validation_failed");
        }
        catch (InvalidOperationException ex)
        {
            return result.Conflict(ex.Message, "payment_start_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در ایجاد پرداخت سفارش {OrderId}", request.OrderId);
            return result.Failed("ارتباط با درگاه پرداخت برقرار نشد. لطفاً دوباره تلاش کنید.", ResultStatus.Failure, "payment_start_failed");
        }
    }

    public async Task<ResultDto<PaymentDto>> GetForOrderAsync(
        Guid orderId,
        string? currentMobile,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<PaymentDto>();
        if (orderId == Guid.Empty)
            return result.ValidationFailed("شناسه سفارش معتبر نیست.", "order_id_invalid");

        try
        {
            var order = (await orders.GetByIdAsync(orderId, cancellationToken)).RequireData();
            if (!OwnsOrder(order, currentMobile))
                return result.Forbidden("این سفارش متعلق به حساب فعلی نیست.", "payment_order_forbidden");

            var payment = await payments.GetForOrderAsync(orderId, cancellationToken);
            return payment is null
                ? result.NotFound("پرداختی برای این سفارش پیدا نشد.", "payment_not_found")
                : result.Success("اطلاعات پرداخت با موفقیت دریافت شد.", payment);
        }
        catch (OperationCanceledException) { throw; }
        catch (ResultDtoException ex) { return result.Failed(ex); }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت پرداخت سفارش {OrderId}", orderId);
            return result.Failed("خطایی در دریافت اطلاعات پرداخت رخ داده است.", ResultStatus.Failure, "payment_get_failed");
        }
    }

    public async Task<ResultDto<PaymentReceiptDto>> VerifyZarinpalAsync(
        Guid paymentId,
        string? authority,
        string? gatewayStatus,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<PaymentReceiptDto>();
        if (paymentId == Guid.Empty)
            return result.ValidationFailed("شناسه پرداخت معتبر نیست.", "payment_id_invalid");

        try
        {
            var payment = await payments.GetByIdAsync(paymentId, cancellationToken);
            if (payment is null)
                return result.NotFound("پرداخت پیدا نشد.", "payment_not_found");

            if (payment.Method != PaymentMethod.OnlineGateway.ToString() ||
                payment.Gateway != IranianPaymentGateway.Zarinpal.ToString())
            {
                return result.Conflict(
                    "این پرداخت متعلق به درگاه زرین‌پال نیست.",
                    "payment_gateway_mismatch");
            }

            if (string.IsNullOrWhiteSpace(authority))
                return result.ValidationFailed("Authority زرین‌پال ارسال نشده است.", "zarinpal_authority_required");

            if (string.IsNullOrWhiteSpace(payment.GatewayAuthority) ||
                !string.Equals(payment.GatewayAuthority, authority.Trim(), StringComparison.Ordinal))
            {
                return result.Conflict(
                    "Authority بازگشتی با پرداخت ثبت‌شده مطابقت ندارد.",
                    "zarinpal_authority_mismatch");
            }

            if (payment.Status is nameof(PaymentTransactionStatus.Verified) or nameof(PaymentTransactionStatus.Succeeded))
            {
                return result.Success(
                    "این پرداخت قبلاً تأیید شده است.",
                    ToReceipt(payment, "این پرداخت قبلاً تأیید شده است."));
            }

            var order = (await orders.GetByIdAsync(payment.OrderId, cancellationToken)).RequireData();
            var previousOrderStatus = order.Status;

            if (!string.Equals(gatewayStatus, "OK", StringComparison.OrdinalIgnoreCase))
            {
                if (order.PaymentStatus != PaymentStatus.Paid)
                    order.MarkPaymentFailed();

                var cancelled = await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                    payment.Id,
                    PaymentTransactionStatus.CancelledByUser,
                    payment.GatewayAuthority,
                    payment.ReferenceId,
                    payment.TraceNumber,
                    payment.MaskedCardNumber,
                    "پرداخت توسط کاربر لغو شد یا در درگاه تکمیل نشد.",
                    $"Callback Status={gatewayStatus}; Authority={authority}",
                    DateTimeOffset.UtcNow,
                    null,
                    "zarinpal-callback",
                    null,
                    null,
                    ToOrderPaymentState(order),
                    previousOrderStatus), cancellationToken);

                var cancelledReceipt = ToReceipt(cancelled, "پرداخت توسط کاربر لغو شد.");
                await TryQueueNotificationAsync(cancelledReceipt, cancelled.CustomerMobile, cancellationToken);
                return FailureWithReceipt(
                    cancelled,
                    "پرداخت توسط کاربر لغو شد.",
                    ResultStatus.Conflict,
                    "payment_cancelled_by_user");
            }

            var verification = await gateway.VerifyAsync(new ZarinpalVerifyRequest(
                payment.Amount,
                gateway.Currency,
                authority.Trim()), cancellationToken);

            if (!verification.IsSuccessful)
            {
                // Code 0 is produced by timeout, network, malformed response, or local
                // configuration errors. The payment outcome is unknown in these cases,
                // therefore it must remain retryable and the order must not be failed.
                if (verification.Code == 0)
                {
                    var uncertain = await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                        payment.Id,
                        PaymentTransactionStatus.RedirectedToGateway,
                        payment.GatewayAuthority,
                        payment.ReferenceId,
                        payment.TraceNumber,
                        payment.MaskedCardNumber,
                        verification.Message,
                        verification.RawResponse,
                        DateTimeOffset.UtcNow,
                        payment.PaidAt,
                        "zarinpal-verify",
                        null,
                        null,
                        null,
                        null), cancellationToken);

                    logger.LogWarning(
                        "Zarinpal verify outcome is uncertain. PaymentId={PaymentId} Message={Message}",
                        payment.Id,
                        verification.Message);

                    return FailureWithReceipt(
                        uncertain,
                        "نتیجه تأیید پرداخت هنوز قطعی نیست. چند لحظه دیگر وضعیت سفارش را بررسی کنید.",
                        ResultStatus.Failure,
                        "zarinpal_verify_uncertain");
                }

                if (order.PaymentStatus != PaymentStatus.Paid)
                    order.MarkPaymentFailed();

                var failed = await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                    payment.Id,
                    PaymentTransactionStatus.Failed,
                    payment.GatewayAuthority,
                    verification.ReferenceId?.ToString(),
                    null,
                    verification.CardPan,
                    verification.Message,
                    verification.RawResponse,
                    DateTimeOffset.UtcNow,
                    null,
                    "zarinpal-verify",
                    null,
                    null,
                    ToOrderPaymentState(order),
                    previousOrderStatus), cancellationToken);

                logger.LogWarning(
                    "Zarinpal verify failed. PaymentId={PaymentId} Code={Code} Message={Message}",
                    payment.Id,
                    verification.Code,
                    verification.Message);

                var failedReceipt = ToReceipt(failed, verification.Message);
                await TryQueueNotificationAsync(failedReceipt, failed.CustomerMobile, cancellationToken);
                return FailureWithReceipt(
                    failed,
                    verification.Message,
                    ResultStatus.Conflict,
                    $"zarinpal_verify_{verification.Code}");
            }

            order.MarkPaid();
            var verifiedAt = DateTimeOffset.UtcNow;
            var verified = await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                payment.Id,
                PaymentTransactionStatus.Verified,
                payment.GatewayAuthority,
                verification.ReferenceId?.ToString(),
                null,
                verification.CardPan,
                verification.WasAlreadyVerified
                    ? "پرداخت قبلاً توسط زرین‌پال تأیید شده بود."
                    : "پرداخت توسط زرین‌پال تأیید شد.",
                verification.RawResponse,
                verifiedAt,
                verifiedAt,
                "zarinpal-verify",
                previousOrderStatus == order.Status ? null : "پرداخت سفارش توسط زرین‌پال تأیید شد",
                verification.Message,
                ToOrderPaymentState(order),
                previousOrderStatus), cancellationToken);

            var verifiedReceipt = ToReceipt(verified, "پرداخت با موفقیت تأیید شد.");
            await TryQueueNotificationAsync(verifiedReceipt, verified.CustomerMobile, cancellationToken);
            return result.Success("پرداخت با موفقیت تأیید شد.", verifiedReceipt);
        }
        catch (OperationCanceledException) { throw; }
        catch (ResultDtoException ex) { return result.Failed(ex); }
        catch (ArgumentException ex) { return result.ValidationFailed(ex.Message, "payment_verify_validation_failed"); }
        catch (InvalidOperationException ex) { return result.Conflict(ex.Message, "payment_verify_conflict"); }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در تأیید پرداخت زرین‌پال {PaymentId}", paymentId);
            return result.Failed("تأیید پرداخت با خطا مواجه شد. وضعیت سفارش را از حساب کاربری بررسی کنید.", ResultStatus.Failure, "payment_verify_failed");
        }
    }

    public async Task<ResultDto<PaymentRefundDto>> RefundZarinpalAsync(
        Guid paymentId,
        CreateZarinpalRefundRequest request,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<PaymentRefundDto>();
        if (paymentId == Guid.Empty)
            return result.ValidationFailed("شناسه پرداخت معتبر نیست.", "payment_id_invalid");
        if (request is null)
            return result.ValidationFailed("اطلاعات Refund ارسال نشده است.", "refund_request_required");
        if (request.Amount <= 0)
            return result.ValidationFailed("مبلغ Refund باید بیشتر از صفر باشد.", "refund_amount_invalid");
        if (string.IsNullOrWhiteSpace(request.Description))
            return result.ValidationFailed("توضیح Refund الزامی است.", "refund_description_required");

        try
        {
            var payment = await payments.GetByIdAsync(paymentId, cancellationToken);
            if (payment is null)
                return result.NotFound("پرداخت پیدا نشد.", "payment_not_found");

            if (payment.Method != PaymentMethod.OnlineGateway.ToString() ||
                payment.Gateway != IranianPaymentGateway.Zarinpal.ToString())
            {
                return result.Conflict(
                    "Refund خودکار فقط برای پرداخت آنلاین زرین‌پال قابل انجام است.",
                    "zarinpal_refund_gateway_mismatch");
            }

            if (payment.Status != nameof(PaymentTransactionStatus.Verified) &&
                payment.Status != nameof(PaymentTransactionStatus.Succeeded) &&
                payment.Status != nameof(PaymentTransactionStatus.Refunded))
            {
                return result.Conflict(
                    "فقط پرداخت تأییدشده زرین‌پال قابل Refund است.",
                    "zarinpal_refund_payment_not_verified");
            }

            if (string.IsNullOrWhiteSpace(payment.GatewayAuthority))
            {
                return result.Conflict(
                    "SessionId/Authority پرداخت زرین‌پال برای Refund موجود نیست.",
                    "zarinpal_refund_session_missing");
            }

            if (request.Amount > payment.Amount)
            {
                return result.ValidationFailed(
                    "مبلغ Refund نمی‌تواند از مبلغ پرداخت بیشتر باشد.",
                    "refund_amount_exceeds_payment");
            }

            var description = request.Description.Trim();
            var createdAt = DateTimeOffset.UtcNow;
            var creation = await payments.CreateRefundAsync(new CreatePaymentRefundRecord(
                Guid.NewGuid(),
                payment.Id,
                payment.OrderId,
                request.Amount,
                payment.Amount,
                description,
                createdAt), cancellationToken);

            if (!creation.WasCreated)
            {
                return result.Success(
                    creation.Refund.Status == nameof(RefundStatus.PaidToBankCard)
                        ? "این Refund قبلاً تکمیل شده است."
                        : "درخواست Refund مشابه قبلاً ثبت شده و در حال پیگیری است.",
                    creation.Refund);
            }

            var provider = await gateway.RefundAsync(new ZarinpalRefundRequest(
                payment.GatewayAuthority,
                request.Amount,
                gateway.Currency,
                description), cancellationToken);

            if (!provider.IsSuccessful)
            {
                var uncertain = provider.ErrorCode is
                    "zarinpal_refund_timeout" or
                    "zarinpal_refund_connection_failed" or
                    "zarinpal_refund_invalid_response";

                var failedRefund = await payments.PersistRefundOutcomeAsync(
                    new PersistPaymentRefundOutcome(
                        creation.Refund.Id,
                        uncertain ? RefundStatus.Requested : RefundStatus.Rejected,
                        provider.Message,
                        provider.RefundId,
                        provider.RawResponse,
                        DateTimeOffset.UtcNow,
                        null,
                        null,
                        null,
                        null,
                        null,
                        string.IsNullOrWhiteSpace(changedBy) ? "admin" : changedBy.Trim()),
                    cancellationToken);

                logger.LogWarning(
                    "Zarinpal refund failed or uncertain. PaymentId={PaymentId} RefundId={RefundId} ErrorCode={ErrorCode} Message={Message}",
                    payment.Id,
                    failedRefund.Id,
                    provider.ErrorCode,
                    provider.Message);

                return new ResultDto<PaymentRefundDto>
                {
                    IsSuccess = false,
                    Status = uncertain ? ResultStatus.Failure : ResultStatus.Conflict,
                    Message = uncertain
                        ? "نتیجه Refund قطعی نیست؛ پیش از ثبت درخواست جدید، وضعیت را در پنل زرین‌پال بررسی کنید."
                        : provider.Message,
                    ErrorCode = provider.ErrorCode ?? "zarinpal_refund_failed",
                    Data = failedRefund
                };
            }

            if (provider.IsCompleted &&
                (provider.RefundedAmount is null || provider.RefundedAmount.Value != request.Amount))
            {
                var reconciliationMessage = provider.RefundedAmount is null
                    ? "زرین‌پال Refund را تکمیل‌شده اعلام کرد، اما مبلغ نهایی را برنگرداند. وضعیت باید در پنل زرین‌پال تطبیق داده شود."
                    : $"مبلغ تکمیل‌شده Refund ({provider.RefundedAmount.Value:N0}) با مبلغ درخواست ({request.Amount:N0}) یکسان نیست. وضعیت باید در پنل زرین‌پال تطبیق داده شود.";

                var reconciliationRefund = await payments.PersistRefundOutcomeAsync(
                    new PersistPaymentRefundOutcome(
                        creation.Refund.Id,
                        RefundStatus.Approved,
                        reconciliationMessage,
                        provider.RefundId,
                        provider.RawResponse,
                        DateTimeOffset.UtcNow,
                        null,
                        null,
                        null,
                        null,
                        null,
                        string.IsNullOrWhiteSpace(changedBy) ? "admin" : changedBy.Trim()),
                    cancellationToken);

                return new ResultDto<PaymentRefundDto>
                {
                    IsSuccess = false,
                    Status = ResultStatus.Conflict,
                    Message = reconciliationMessage,
                    ErrorCode = "zarinpal_refund_amount_mismatch",
                    Data = reconciliationRefund
                };
            }

            var completedAmountAfterThisRefund =
                creation.CompletedAmountBeforeCreation + request.Amount;
            var isFullCompletedRefund =
                provider.IsCompleted && completedAmountAfterThisRefund >= payment.Amount;
            OrderPaymentState? orderState = null;
            OrderStatus? previousOrderStatus = null;

            if (isFullCompletedRefund)
            {
                var order = (await orders.GetByIdAsync(payment.OrderId, cancellationToken)).RequireData();
                previousOrderStatus = order.Status;
                order.MarkRefunded(provider.Message);
                orderState = ToOrderPaymentState(order);
            }

            var persisted = await payments.PersistRefundOutcomeAsync(
                new PersistPaymentRefundOutcome(
                    creation.Refund.Id,
                    provider.IsCompleted ? RefundStatus.PaidToBankCard : RefundStatus.Approved,
                    provider.Message,
                    provider.RefundId,
                    provider.RawResponse,
                    DateTimeOffset.UtcNow,
                    provider.IsCompleted ? DateTimeOffset.UtcNow : null,
                    isFullCompletedRefund ? PaymentTransactionStatus.Refunded : null,
                    provider.IsCompleted ? PaymentTransactionStatus.Refunded : null,
                    orderState,
                    previousOrderStatus,
                    string.IsNullOrWhiteSpace(changedBy) ? "admin" : changedBy.Trim()),
                cancellationToken);

            return result.Success(
                provider.IsCompleted
                    ? isFullCompletedRefund
                        ? "Refund کامل زرین‌پال انجام شد و سفارش بازپرداخت‌شده ثبت شد."
                        : "Refund جزئی زرین‌پال با موفقیت تکمیل شد."
                    : "درخواست Refund توسط زرین‌پال پذیرفته شد و در انتظار تکمیل بانکی است.",
                persisted);
        }
        catch (OperationCanceledException) { throw; }
        catch (ResultDtoException ex) { return result.Failed(ex); }
        catch (ArgumentException ex) { return result.ValidationFailed(ex.Message, "zarinpal_refund_validation_failed"); }
        catch (InvalidOperationException ex) { return result.Conflict(ex.Message, "zarinpal_refund_conflict"); }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Refund پرداخت زرین‌پال {PaymentId}", paymentId);
            return result.Failed(
                "خطایی در ثبت Refund زرین‌پال رخ داده است.",
                ResultStatus.Failure,
                "zarinpal_refund_failed");
        }
    }

    public async Task<ResultDto<AdminPaymentsDto>> AdminListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return new ResultDto<AdminPaymentsDto>().Success(
                "فهرست پرداخت‌ها با موفقیت دریافت شد.",
                await payments.AdminListAsync(cancellationToken));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت فهرست پرداخت‌ها");
            return new ResultDto<AdminPaymentsDto>().Failed("خطایی در دریافت فهرست پرداخت‌ها رخ داده است.", ResultStatus.Failure, "admin_payments_get_failed");
        }
    }

    public async Task<ResultDto<PaymentDto>> AdminUpdateStatusAsync(
        Guid paymentId,
        UpdatePaymentStatusRequest request,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        var result = new ResultDto<PaymentDto>();
        if (paymentId == Guid.Empty)
            return result.ValidationFailed("شناسه پرداخت معتبر نیست.", "payment_id_invalid");
        if (request is null)
            return result.ValidationFailed("اطلاعات وضعیت پرداخت ارسال نشده است.", "payment_status_request_required");
        if (!Enum.TryParse<PaymentTransactionStatus>(request.Status, true, out var status))
            return result.ValidationFailed("وضعیت پرداخت معتبر نیست.", "payment_status_invalid");

        try
        {
            var payment = await payments.GetByIdAsync(paymentId, cancellationToken);
            if (payment is null)
                return result.NotFound("پرداخت پیدا نشد.", "payment_not_found");

            var order = (await orders.GetByIdAsync(payment.OrderId, cancellationToken)).RequireData();
            var previousOrderStatus = order.Status;

            if ((status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Verified) &&
                payment.Method == PaymentMethod.OnlineGateway.ToString() &&
                payment.Gateway == IranianPaymentGateway.Zarinpal.ToString())
            {
                return result.Conflict(
                    "پرداخت آنلاین زرین‌پال فقط از طریق callback و Verify سمت سرور قابل تأیید است.",
                    "zarinpal_manual_verification_forbidden");
            }

            if ((status is PaymentTransactionStatus.Failed or PaymentTransactionStatus.CancelledByUser) &&
                payment.Method == PaymentMethod.OnlineGateway.ToString() &&
                payment.Gateway == IranianPaymentGateway.Zarinpal.ToString())
            {
                return result.Conflict(
                    "وضعیت پرداخت آنلاین زرین‌پال فقط از callback یا عملیات تطبیق با درگاه قابل تغییر است.",
                    "zarinpal_manual_failure_forbidden");
            }

            var effectiveGatewayMessage = string.IsNullOrWhiteSpace(request.GatewayMessage)
                ? StatusLabel(status)
                : request.GatewayMessage.Trim();
            var rawGatewayResponse = request.GatewayMessage;

            switch (status)
            {
                case PaymentTransactionStatus.Succeeded:
                case PaymentTransactionStatus.Verified:
                    order.MarkPaid();
                    break;
                case PaymentTransactionStatus.Failed:
                case PaymentTransactionStatus.CancelledByUser:
                    order.MarkPaymentFailed();
                    break;
                case PaymentTransactionStatus.Reversed:
                {
                    if (payment.Status == nameof(PaymentTransactionStatus.Reversed))
                    {
                        return result.Success(
                            "این تراکنش قبلاً در زرین‌پال برگشت داده شده است.",
                            payment);
                    }

                    if (payment.Gateway != IranianPaymentGateway.Zarinpal.ToString() ||
                        payment.Method != PaymentMethod.OnlineGateway.ToString())
                    {
                        return result.Conflict(
                            "برگشت خودکار فقط برای پرداخت آنلاین زرین‌پال قابل انجام است.",
                            "zarinpal_reverse_gateway_mismatch");
                    }

                    if (payment.Status != nameof(PaymentTransactionStatus.Verified) &&
                        payment.Status != nameof(PaymentTransactionStatus.Succeeded))
                    {
                        return result.Conflict(
                            "فقط تراکنش تأییدشده زرین‌پال قابل برگشت است.",
                            "zarinpal_reverse_payment_not_verified");
                    }

                    if (string.IsNullOrWhiteSpace(payment.GatewayAuthority))
                    {
                        return result.Conflict(
                            "Authority پرداخت زرین‌پال برای برگشت موجود نیست.",
                            "zarinpal_reverse_authority_missing");
                    }

                    var reversal = await gateway.ReverseAsync(
                        new ZarinpalReverseRequest(payment.GatewayAuthority),
                        cancellationToken);

                    if (!reversal.IsSuccessful)
                    {
                        var reverseErrorCode = reversal.Code == 0
                            ? "zarinpal_reverse_uncertain"
                            : $"zarinpal_reverse_{reversal.Code}";
                        var reverseStatus = reversal.Code == 0
                            ? ResultStatus.Failure
                            : ResultStatus.Conflict;

                        logger.LogWarning(
                            "Zarinpal reverse failed or uncertain. PaymentId={PaymentId} Code={Code} Message={Message}",
                            payment.Id,
                            reversal.Code,
                            reversal.Message);

                        return result.Failed(
                            reversal.Code == 0
                                ? "نتیجه برگشت تراکنش قطعی نیست؛ پیش از تلاش دوباره وضعیت را در پنل زرین‌پال بررسی کنید."
                                : reversal.Message,
                            reverseStatus,
                            reverseErrorCode);
                    }

                    effectiveGatewayMessage = reversal.WasAlreadyReversed
                        ? "تراکنش قبلاً در زرین‌پال برگشت داده شده بود."
                        : reversal.Message;
                    rawGatewayResponse = reversal.RawResponse;
                    order.MarkRefunded(effectiveGatewayMessage);
                    break;
                }
                case PaymentTransactionStatus.Pending:
                case PaymentTransactionStatus.RedirectedToGateway:
                    if (order.PaymentStatus == PaymentStatus.Paid)
                        return result.Conflict("پرداخت تأییدشده را نمی‌توان به وضعیت در انتظار بازگرداند.", "verified_payment_cannot_be_pending");
                    break;
            }

            var now = DateTimeOffset.UtcNow;
            var updated = await payments.PersistOutcomeAsync(new PersistPaymentOutcome(
                payment.Id,
                status,
                payment.GatewayAuthority,
                string.IsNullOrWhiteSpace(request.RefId) ? payment.ReferenceId : request.RefId.Trim(),
                string.IsNullOrWhiteSpace(request.TraceNumber) ? payment.TraceNumber : request.TraceNumber.Trim(),
                payment.MaskedCardNumber,
                effectiveGatewayMessage,
                rawGatewayResponse,
                now,
                status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Verified ? now : payment.PaidAt,
                string.IsNullOrWhiteSpace(changedBy) ? "admin" : changedBy.Trim(),
                previousOrderStatus == order.Status ? null : "وضعیت سفارش بر اساس پرداخت توسط مدیر تغییر کرد",
                effectiveGatewayMessage,
                ToOrderPaymentState(order),
                previousOrderStatus), cancellationToken);

            if (status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Verified or PaymentTransactionStatus.Failed or PaymentTransactionStatus.CancelledByUser or PaymentTransactionStatus.Reversed)
                await TryQueueNotificationAsync(ToReceipt(updated, updated.GatewayMessage ?? updated.StatusLabel), updated.CustomerMobile, cancellationToken);

            return result.Success("وضعیت پرداخت با موفقیت به‌روزرسانی شد.", updated);
        }
        catch (OperationCanceledException) { throw; }
        catch (ResultDtoException ex) { return result.Failed(ex); }
        catch (ArgumentException ex) { return result.ValidationFailed(ex.Message, "payment_status_validation_failed"); }
        catch (InvalidOperationException ex) { return result.Conflict(ex.Message, "payment_status_conflict"); }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در تغییر وضعیت پرداخت {PaymentId}", paymentId);
            return result.Failed("خطایی در به‌روزرسانی وضعیت پرداخت رخ داده است.", ResultStatus.Failure, "payment_status_update_failed");
        }
    }


    private async Task TryQueueNotificationAsync(
        PaymentReceiptDto receipt,
        string customerMobile,
        CancellationToken cancellationToken)
    {
        try
        {
            await notifications.QueuePaymentResultAsync(receipt, customerMobile, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ثبت اعلان پرداخت {PaymentId} ناموفق بود.", receipt.PaymentId);
        }
    }

    private static ResultDto<PaymentReceiptDto> FailureWithReceipt(
        PaymentDto payment,
        string message,
        ResultStatus status,
        string errorCode)
        => new()
        {
            IsSuccess = false,
            Status = status,
            Message = message,
            ErrorCode = errorCode,
            Data = ToReceipt(payment, message)
        };

    private static OrderPaymentState ToOrderPaymentState(Order order)
        => new(
            order.Id,
            order.Status,
            order.PaymentStatus,
            order.TrackingCode,
            order.AdminNote);

    private PaymentInitDto ToInit(PaymentDto payment, string? redirectUrl)
        => new(
            payment.Id,
            payment.OrderId,
            payment.OrderNumber,
            payment.Method,
            payment.Gateway,
            payment.Amount,
            gateway.Currency,
            redirectUrl,
            payment.Status,
            payment.StatusLabel,
            payment.Method != PaymentMethod.OnlineGateway.ToString());

    private static PaymentReceiptDto ToReceipt(PaymentDto payment, string message)
        => new(
            payment.Id,
            payment.OrderId,
            payment.OrderNumber,
            payment.Status,
            payment.StatusLabel,
            payment.ReferenceId,
            payment.TraceNumber,
            payment.Amount,
            payment.PaidAt,
            message);

    private static bool OwnsOrder(Order order, string? currentMobile)
        => !string.IsNullOrWhiteSpace(currentMobile) &&
           string.Equals(NormalizeMobile(order.CustomerMobile), NormalizeMobile(currentMobile), StringComparison.OrdinalIgnoreCase);

    private static bool TryParseMethod(string? value, out PaymentMethod method)
    {
        if (!Enum.TryParse(value, true, out method))
            return false;
        return method is PaymentMethod.OnlineGateway or PaymentMethod.CardToCard or PaymentMethod.CashOnDelivery;
    }

    private static string NormalizeMobile(string? mobile)
        => (mobile ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("+98", "0");

    public static string StatusLabel(PaymentTransactionStatus status) => status switch
    {
        PaymentTransactionStatus.Pending => "در انتظار پرداخت",
        PaymentTransactionStatus.RedirectedToGateway => "ارسال به درگاه",
        PaymentTransactionStatus.Succeeded => "موفق",
        PaymentTransactionStatus.Verified => "تأیید شده",
        PaymentTransactionStatus.Failed => "ناموفق",
        PaymentTransactionStatus.CancelledByUser => "لغو توسط مشتری",
        PaymentTransactionStatus.Reversed => "برگشت خورده",
        PaymentTransactionStatus.Refunded => "Refund شده",
        _ => status.ToString()
    };

    public static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.OnlineGateway => "پرداخت آنلاین",
        PaymentMethod.CardToCard => "کارت به کارت",
        PaymentMethod.CashOnDelivery => "پرداخت هنگام تحویل",
        PaymentMethod.Wallet => "کیف پول",
        PaymentMethod.BankTransfer => "حواله بانکی",
        _ => method.ToString()
    };
}
