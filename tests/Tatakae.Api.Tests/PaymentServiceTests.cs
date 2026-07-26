using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces.Gateways;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Services;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Gateways;
using Tatakae.Infrastructure.Persistence;
using Tatakae.Infrastructure.Persistence.Models;
using Tatakae.Infrastructure.Persistence.Repositories;

namespace Tatakae.Api.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task StartAsync_RequestsZarinpalAndPersistsAuthority()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 1_250_000m);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);

        var result = await service.StartAsync(new CreatePaymentRequest
        {
            OrderId = order.Id,
            Method = "OnlineGateway"
        }, currentMobile: "09123456789");

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Data!.OrderId);
        Assert.Equal(1_250_000m, result.Data.Amount);
        Assert.Equal("RedirectedToGateway", result.Data.Status);
        Assert.Equal("IRT", result.Data.CurrencyCode);
        Assert.Equal("https://sandbox.zarinpal.com/pg/StartPay/S000000000000000000000000000001", result.Data.RedirectUrl);
        Assert.Equal(1, gateway.RequestCount);

        var payment = await db.Payments.Include(x => x.Transactions).SingleAsync(x => x.Id == result.Data.PaymentId);
        Assert.Equal(PaymentTransactionStatus.RedirectedToGateway, payment.Status);
        Assert.Equal("S000000000000000000000000000001", payment.GatewayAuthority);
        Assert.Equal(2, payment.Transactions.Count);
    }

    [Fact]
    public async Task StartAsync_WhenLegacyLineBreakdownDiffers_UsesPersistedOrderTotal()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 850_000m, lineUnitPrice: 900_000m);
        var service = CreateService(db, new FakeZarinpalGateway());

        var result = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.Message}");
        Assert.Equal(850_000m, result.Data!.Amount);
    }

    [Fact]
    public async Task StartAsync_WhenActivePaymentExists_IsIdempotent()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 900_000m);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);

        var first = await service.StartAsync(new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" }, "09123456789");
        var second = await service.StartAsync(new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" }, "09123456789");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.PaymentId, second.Data!.PaymentId);
        Assert.Equal(1, gateway.RequestCount);
        Assert.Equal(1, await db.Payments.CountAsync(x => x.OrderId == order.Id));
    }

    [Fact]
    public async Task StartAsync_WhenMobileDoesNotOwnOrder_ReturnsForbidden()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, customerMobile: "09123456789");
        var service = CreateService(db, new FakeZarinpalGateway());

        var result = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            currentMobile: "09999999999");

        Assert.False(result.IsSuccess);
        Assert.Equal(Tatakae.Application.Contracts.Common.ResultStatus.Forbidden, result.Status);
        Assert.Equal("payment_order_forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task StartAsync_WhenProviderRequestOutcomeIsUncertain_DoesNotCreateImmediateDuplicate()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 900_000m);
        var gateway = new FakeZarinpalGateway
        {
            RequestResult = new ZarinpalRequestResult(
                false,
                0,
                "ارتباط با زرین‌پال برقرار نشد.",
                null,
                null,
                "network-timeout")
        };
        var service = CreateService(db, gateway);

        var first = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");
        var second = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");

        Assert.False(first.IsSuccess);
        Assert.Equal("zarinpal_request_uncertain", first.ErrorCode);
        Assert.False(second.IsSuccess);
        Assert.Equal("payment_request_in_progress", second.ErrorCode);
        Assert.Equal(1, gateway.RequestCount);
        Assert.Equal(1, await db.Payments.CountAsync(x => x.OrderId == order.Id));
        Assert.Equal(
            PaymentTransactionStatus.Pending,
            (await db.Payments.SingleAsync(x => x.OrderId == order.Id)).Status);
    }

    [Fact]
    public async Task VerifyZarinpalAsync_MarksOrderPaidAndStoresReference()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 1_500_000m);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" }, "09123456789");

        var result = await service.VerifyZarinpalAsync(
            init.Data!.PaymentId,
            gateway.Authority,
            "OK");

        Assert.True(result.IsSuccess);
        Assert.Equal("Verified", result.Data!.Status);
        Assert.Equal("123456789", result.Data.RefId);
        Assert.Equal(1, gateway.VerifyCount);

        var updatedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
        Assert.Equal(PaymentStatus.Paid, updatedOrder.PaymentStatus);

        var payment = await db.Payments.Include(x => x.Transactions).SingleAsync(x => x.Id == init.Data.PaymentId);
        Assert.Equal(PaymentTransactionStatus.Verified, payment.Status);
        Assert.Equal("123456789", payment.ReferenceId);
        Assert.Equal("6219-****-****-1234", payment.MaskedCardNumber);
        Assert.Equal(3, payment.Transactions.Count);
        Assert.Equal(1, await db.OrderStatusHistory.CountAsync(x => x.OrderId == order.Id && x.ToStatus == OrderStatus.Paid));
    }

    [Fact]
    public async Task VerifyZarinpalAsync_WhenCallbackIsNok_MarksPaymentCancelled()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" }, "09123456789");

        var result = await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "NOK");

        Assert.False(result.IsSuccess);
        Assert.Equal("CancelledByUser", result.Data!.Status);
        Assert.Equal(0, gateway.VerifyCount);

        var updatedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(OrderStatus.PendingPayment, updatedOrder.Status);
        Assert.Equal(PaymentStatus.Failed, updatedOrder.PaymentStatus);
    }


    [Fact]
    public async Task VerifyZarinpalAsync_WhenAuthorityDoesNotMatch_DoesNotMutatePaymentOrOrder()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");

        var result = await service.VerifyZarinpalAsync(
            init.Data!.PaymentId,
            "S-WRONG-AUTHORITY",
            "NOK");

        Assert.False(result.IsSuccess);
        Assert.Equal("zarinpal_authority_mismatch", result.ErrorCode);
        Assert.Equal(0, gateway.VerifyCount);

        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(PaymentTransactionStatus.RedirectedToGateway, storedPayment.Status);
        Assert.Equal(PaymentStatus.Pending, storedOrder.PaymentStatus);
        Assert.Equal(OrderStatus.PendingPayment, storedOrder.Status);
    }

    [Fact]
    public async Task VerifyZarinpalAsync_WhenGatewayOutcomeIsUncertain_RemainsRetryable()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway
        {
            VerifyResult = new ZarinpalVerifyResult(
                false,
                false,
                0,
                "ارتباط با زرین‌پال برقرار نشد.",
                null,
                null,
                null,
                "zarinpal_timeout")
        };
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");

        var result = await service.VerifyZarinpalAsync(
            init.Data!.PaymentId,
            gateway.Authority,
            "OK");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.Equal("zarinpal_verify_uncertain", result.ErrorCode);

        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(PaymentTransactionStatus.RedirectedToGateway, storedPayment.Status);
        Assert.Equal(PaymentStatus.Pending, storedOrder.PaymentStatus);
        Assert.Equal(OrderStatus.PendingPayment, storedOrder.Status);
    }

    [Fact]
    public async Task VerifyZarinpalAsync_WhenCallbackRepeats_IsIdempotent()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");

        var first = await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "OK");
        var second = await service.VerifyZarinpalAsync(init.Data.PaymentId, gateway.Authority, "OK");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, gateway.VerifyCount);
        Assert.Equal(
            3,
            await db.PaymentTransactions.CountAsync(x => x.PaymentId == init.Data.PaymentId));
    }

    [Fact]
    public async Task AdminUpdateStatusAsync_AppliesDomainTransitionBeforePersistence()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 1_100_000m);
        var service = CreateService(db, new FakeZarinpalGateway());
        var init = await service.StartAsync(new CreatePaymentRequest { OrderId = order.Id, Method = "CardToCard" }, "09123456789");

        var result = await service.AdminUpdateStatusAsync(init.Data!.PaymentId, new UpdatePaymentStatusRequest
        {
            Status = "Verified",
            RefId = "ADMIN-REF-1",
            TraceNumber = "TRACE-1",
            GatewayMessage = "رسید کارت‌به‌کارت تأیید شد"
        }, changedBy: "admin-test");

        Assert.True(result.IsSuccess);
        Assert.Equal("Verified", result.Data!.Status);
        Assert.Equal("ADMIN-REF-1", result.Data.ReferenceId);

        var updatedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
        Assert.Equal(PaymentStatus.Paid, updatedOrder.PaymentStatus);
    }


    [Fact]
    public async Task AdminUpdateStatusAsync_CannotManuallyVerifyOnlineZarinpalPayment()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");

        var result = await service.AdminUpdateStatusAsync(
            init.Data!.PaymentId,
            new UpdatePaymentStatusRequest { Status = "Verified" },
            changedBy: "admin-test");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("zarinpal_manual_verification_forbidden", result.ErrorCode);
        Assert.Equal(0, gateway.VerifyCount);

        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(PaymentTransactionStatus.RedirectedToGateway, storedPayment.Status);
        Assert.Equal(OrderStatus.PendingPayment, storedOrder.Status);
        Assert.Equal(PaymentStatus.Pending, storedOrder.PaymentStatus);
    }


    [Fact]
    public async Task AdminUpdateStatusAsync_CannotManuallyFailOnlineZarinpalPayment()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");

        var result = await service.AdminUpdateStatusAsync(
            init.Data!.PaymentId,
            new UpdatePaymentStatusRequest { Status = "Failed" },
            changedBy: "admin-test");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("zarinpal_manual_failure_forbidden", result.ErrorCode);

        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(PaymentTransactionStatus.RedirectedToGateway, storedPayment.Status);
        Assert.Equal(PaymentStatus.Pending, storedOrder.PaymentStatus);
    }


    [Fact]
    public async Task RefundZarinpalAsync_WhenProviderCompletesFullRefund_UpdatesRefundPaymentAndOrder()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 1_250_000m);
        var gateway = new FakeZarinpalGateway
        {
            RefundResult = new ZarinpalRefundResult(
                true,
                true,
                null,
                "Refund completed",
                "REFUND-1001",
                1_250_000m,
                "PAID",
                "{\"data\":{\"resource\":{\"id\":\"REFUND-1001\"}}}")
        };
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");
        await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "OK");

        var result = await service.RefundZarinpalAsync(
            init.Data.PaymentId,
            new CreateZarinpalRefundRequest
            {
                Amount = 1_250_000m,
                Description = "Refund کامل تست"
            },
            changedBy: "admin-test");

        Assert.True(result.IsSuccess);
        Assert.Equal("PaidToBankCard", result.Data!.Status);
        Assert.Equal("REFUND-1001", result.Data.ReferenceNumber);
        Assert.Equal(1, gateway.RefundCount);

        var storedRefund = await db.Refunds.SingleAsync(x => x.Id == result.Data.Id);
        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(RefundStatus.PaidToBankCard, storedRefund.Status);
        Assert.Equal(PaymentTransactionStatus.Refunded, storedPayment.Status);
        Assert.Equal(OrderStatus.Refunded, storedOrder.Status);
        Assert.Equal(PaymentStatus.Refunded, storedOrder.PaymentStatus);
    }

    [Fact]
    public async Task RefundZarinpalAsync_WhenProviderAcceptsPendingRefund_IsIdempotentAndKeepsOrderPaid()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 800_000m);
        var gateway = new FakeZarinpalGateway
        {
            RefundResult = new ZarinpalRefundResult(
                true,
                false,
                null,
                "Refund accepted",
                "REFUND-PENDING-1",
                400_000m,
                "PENDING",
                "{\"data\":{\"resource\":{\"id\":\"REFUND-PENDING-1\"}}}")
        };
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");
        await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "OK");
        var request = new CreateZarinpalRefundRequest
        {
            Amount = 400_000m,
            Description = "Refund جزئی تست"
        };

        var first = await service.RefundZarinpalAsync(init.Data.PaymentId, request, "admin-test");
        var second = await service.RefundZarinpalAsync(init.Data.PaymentId, request, "admin-test");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Equal("Approved", first.Data.Status);
        Assert.Equal(1, gateway.RefundCount);

        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(PaymentTransactionStatus.Verified, storedPayment.Status);
        Assert.Equal(OrderStatus.Paid, storedOrder.Status);
        Assert.Equal(PaymentStatus.Paid, storedOrder.PaymentStatus);
    }



    [Fact]
    public async Task RefundZarinpalAsync_WhenCompletedPartialRefundsReachPaymentTotal_MarksOrderRefunded()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 800_000m);
        var gateway = new FakeZarinpalGateway();
        gateway.RefundResults.Enqueue(new ZarinpalRefundResult(
            true,
            true,
            null,
            "First partial refund completed",
            "REFUND-PART-1",
            300_000m,
            "PAID",
            "{\"data\":{\"resource\":{\"id\":\"REFUND-PART-1\"}}}"));
        gateway.RefundResults.Enqueue(new ZarinpalRefundResult(
            true,
            true,
            null,
            "Second partial refund completed",
            "REFUND-PART-2",
            500_000m,
            "PAID",
            "{\"data\":{\"resource\":{\"id\":\"REFUND-PART-2\"}}}"));
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");
        await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "OK");

        var first = await service.RefundZarinpalAsync(
            init.Data.PaymentId,
            new CreateZarinpalRefundRequest
            {
                Amount = 300_000m,
                Description = "Refund جزئی اول"
            },
            "admin-test");

        Assert.True(first.IsSuccess);
        Assert.Equal(PaymentTransactionStatus.Verified,
            (await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId)).Status);
        Assert.Equal(OrderStatus.Paid,
            (await db.Orders.SingleAsync(x => x.Id == order.Id)).Status);

        var second = await service.RefundZarinpalAsync(
            init.Data.PaymentId,
            new CreateZarinpalRefundRequest
            {
                Amount = 500_000m,
                Description = "Refund جزئی دوم"
            },
            "admin-test");

        Assert.True(second.IsSuccess);
        Assert.Equal(PaymentTransactionStatus.Refunded,
            (await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId)).Status);
        Assert.Equal(OrderStatus.Refunded,
            (await db.Orders.SingleAsync(x => x.Id == order.Id)).Status);
        Assert.Equal(PaymentStatus.Refunded,
            (await db.Orders.SingleAsync(x => x.Id == order.Id)).PaymentStatus);
        Assert.Equal(2, gateway.RefundCount);
    }

    [Fact]
    public async Task RefundZarinpalAsync_WhenProviderAmountDoesNotMatch_DoesNotMutatePaymentOrOrder()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db, total: 800_000m);
        var gateway = new FakeZarinpalGateway
        {
            RefundResult = new ZarinpalRefundResult(
                true,
                true,
                null,
                "Refund completed with unexpected amount",
                "REFUND-MISMATCH",
                399_999m,
                "PAID",
                "{\"data\":{\"resource\":{\"id\":\"REFUND-MISMATCH\"}}}")
        };
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");
        await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "OK");

        var result = await service.RefundZarinpalAsync(
            init.Data.PaymentId,
            new CreateZarinpalRefundRequest
            {
                Amount = 400_000m,
                Description = "Refund mismatch test"
            },
            "admin-test");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("zarinpal_refund_amount_mismatch", result.ErrorCode);
        Assert.Equal("Approved", result.Data!.Status);
        Assert.Equal(PaymentTransactionStatus.Verified,
            (await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId)).Status);
        Assert.Equal(OrderStatus.Paid,
            (await db.Orders.SingleAsync(x => x.Id == order.Id)).Status);
        Assert.Equal(PaymentStatus.Paid,
            (await db.Orders.SingleAsync(x => x.Id == order.Id)).PaymentStatus);
    }


    [Fact]
    public async Task AdminUpdateStatusAsync_Reversed_UsesOfficialZarinpalReverseBeforeDomainMutation()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway();
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");
        await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "OK");

        var result = await service.AdminUpdateStatusAsync(
            init.Data.PaymentId,
            new UpdatePaymentStatusRequest
            {
                Status = "Reversed",
                GatewayMessage = "برگشت کامل سفارش"
            },
            changedBy: "admin-test");

        Assert.True(result.IsSuccess);
        Assert.Equal("Reversed", result.Data!.Status);
        Assert.Equal(1, gateway.ReverseCount);

        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(PaymentTransactionStatus.Reversed, storedPayment.Status);
        Assert.Equal(OrderStatus.Refunded, storedOrder.Status);
        Assert.Equal(PaymentStatus.Refunded, storedOrder.PaymentStatus);
    }

    [Fact]
    public async Task AdminUpdateStatusAsync_WhenReverseOutcomeIsUncertain_DoesNotMarkOrderRefunded()
    {
        await using var db = CreateDbContext();
        var order = SeedOrder(db);
        var gateway = new FakeZarinpalGateway
        {
            ReverseResult = new ZarinpalReverseResult(
                false,
                false,
                0,
                "ارتباط با زرین‌پال برقرار نشد.",
                "zarinpal_reverse_timeout")
        };
        var service = CreateService(db, gateway);
        var init = await service.StartAsync(
            new CreatePaymentRequest { OrderId = order.Id, Method = "OnlineGateway" },
            "09123456789");
        await service.VerifyZarinpalAsync(init.Data!.PaymentId, gateway.Authority, "OK");

        var result = await service.AdminUpdateStatusAsync(
            init.Data.PaymentId,
            new UpdatePaymentStatusRequest { Status = "Reversed" },
            changedBy: "admin-test");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.Equal("zarinpal_reverse_uncertain", result.ErrorCode);

        var storedPayment = await db.Payments.SingleAsync(x => x.Id == init.Data.PaymentId);
        var storedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(PaymentTransactionStatus.Verified, storedPayment.Status);
        Assert.Equal(OrderStatus.Paid, storedOrder.Status);
        Assert.Equal(PaymentStatus.Paid, storedOrder.PaymentStatus);
    }

    private static IPaymentService CreateService(TatakaeDbContext db, IZarinpalPaymentGateway gateway)
    {
        var orderRepository = new SqlOrderRepository(db);
        var paymentRepository = new EfPaymentRepository(db);
        INotificationService notificationService = new NotificationService(
            new SqlNotificationRepository(db),
            new SqlCustomerRepository(db),
            NullLogger<NotificationService>.Instance);

        return new PaymentService(
            gateway,
            paymentRepository,
            orderRepository,
            notificationService,
            NullLogger<PaymentService>.Instance);
    }

    private static TatakaeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TatakaeDbContext>()
            .UseInMemoryDatabase($"tatakae-payments-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;
        return new TatakaeDbContext(options);
    }

    private static OrderDbRecord SeedOrder(
        TatakaeDbContext db,
        decimal total = 1_250_000m,
        string customerMobile = "09123456789",
        decimal? lineUnitPrice = null)
    {
        var customerId = Guid.NewGuid();
        db.Customers.Add(new CustomerDbRecord
        {
            Id = customerId,
            FullName = "کاربر تست",
            Mobile = customerMobile,
            Email = "customer@example.com",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var order = new OrderDbRecord
        {
            Id = Guid.NewGuid(),
            OrderNumber = CreateOrderNumber(),
            CustomerId = customerId,
            CustomerName = "کاربر تست",
            CustomerMobile = customerMobile,
            ShippingRecipientName = "کاربر تست",
            ShippingMobile = customerMobile,
            ShippingProvince = "تهران",
            ShippingCity = "تهران",
            ShippingPostalCode = "1234567890",
            ShippingAddressLine = "خیابان تست، پلاک ۱",
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Pending,
            Subtotal = total,
            ShippingAmount = 0m,
            ShippingMethodCode = "manual",
            ShippingMethodTitle = "ارسال دستی",
            DiscountAmount = 0m,
            Total = total
        };

        order.Lines.Add(new OrderLineDbRecord
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            ProductName = "محصول تست",
            ProductSlug = "test-product",
            ProductImageUrl = "https://example.com/product.jpg",
            Sku = "TEST-SKU",
            Size = "L",
            ColorName = "مشکی",
            ColorHex = "#111827",
            Quantity = 1,
            UnitGarmentPrice = lineUnitPrice ?? total,
            EmbroideryId = Guid.NewGuid(),
            EmbroideryPlacement = EmbroideryPlacement.LeftChest,
            EmbroideryWidthCm = 5,
            EmbroideryHeightCm = 5,
            EmbroideryThreadColorCount = 1,
            EmbroideryThreadColorHexesCsv = "#FFFFFF",
            EmbroideryCalculatedPrice = 0,
            EmbroideryGarmentType = "TShirt",
            EmbroideryGarmentSize = "L",
            EmbroideryGarmentColorHex = "#111827",
            EmbroideryDesignSource = "Motif",
            EmbroideryMotifKey = "dragon",
            EmbroideryScalePercent = 100,
            EmbroideryOpacityPercent = 100
        });

        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }


    private static string CreateOrderNumber()
    {
        var value = $"EMB-TEST-{Guid.NewGuid():N}".ToUpperInvariant();
        return value[..Math.Min(30, value.Length)];
    }

    private sealed class FakeZarinpalGateway : IZarinpalPaymentGateway
    {
        public string Authority { get; } = "S000000000000000000000000000001";
        public string Currency => "IRT";
        public int RequestCount { get; private set; }
        public int VerifyCount { get; private set; }
        public int ReverseCount { get; private set; }
        public int RefundCount { get; private set; }
        public ZarinpalRequestResult? RequestResult { get; init; }
        public ZarinpalVerifyResult? VerifyResult { get; init; }
        public ZarinpalReverseResult? ReverseResult { get; init; }
        public ZarinpalRefundResult? RefundResult { get; init; }
        public Queue<ZarinpalRefundResult> RefundResults { get; } = new();

        public string GetRedirectUrl(string authority)
            => $"https://sandbox.zarinpal.com/pg/StartPay/{authority}";

        public Task<ZarinpalRequestResult> RequestAsync(ZarinpalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return Task.FromResult(RequestResult ?? new ZarinpalRequestResult(
                true,
                100,
                "درخواست موفق",
                Authority,
                GetRedirectUrl(Authority),
                "{\"data\":{\"code\":100}}"));
        }

        public Task<ZarinpalVerifyResult> VerifyAsync(ZarinpalVerifyRequest request, CancellationToken cancellationToken = default)
        {
            VerifyCount++;
            return Task.FromResult(VerifyResult ?? new ZarinpalVerifyResult(
                true,
                false,
                100,
                "پرداخت تأیید شد",
                123456789,
                "6219-****-****-1234",
                0,
                "{\"data\":{\"code\":100,\"ref_id\":123456789}}"));
        }

        public Task<ZarinpalReverseResult> ReverseAsync(
            ZarinpalReverseRequest request,
            CancellationToken cancellationToken = default)
        {
            ReverseCount++;
            return Task.FromResult(ReverseResult ?? new ZarinpalReverseResult(
                true,
                false,
                100,
                "تراکنش با موفقیت برگشت داده شد.",
                "{\"data\":{\"code\":100}}"));
        }

        public Task<ZarinpalRefundResult> RefundAsync(
            ZarinpalRefundRequest request,
            CancellationToken cancellationToken = default)
        {
            RefundCount++;
            if (RefundResults.Count > 0)
                return Task.FromResult(RefundResults.Dequeue());

            return Task.FromResult(RefundResult ?? new ZarinpalRefundResult(
                true,
                false,
                null,
                "درخواست Refund پذیرفته شد.",
                "REFUND-DEFAULT",
                request.Amount,
                "PENDING",
                "{\"data\":{\"resource\":{\"id\":\"REFUND-DEFAULT\"}}}"));
        }
    }
}
