using Tatakae.Application.Contracts.Notifications;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Payments;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class NotificationServiceTests
{
    private static readonly Guid CustomerId = Guid.Parse("41000000-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("42000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task AdminCreateAsync_WithValidManualNotification_CreatesSentNotification()
    {
        var service = CreateService(out var repo);

        var item = await service.AdminCreateAsync(new CreateNotificationRequest
        {
            CustomerMobile = "09120000000",
            Channel = "InApp",
            Type = "Manual",
            Recipient = "09120000000",
            Subject = "پیام تست",
            Body = "متن پیام تست",
            MarkAsSent = true
        });

        Assert.Equal("Sent", item.Status);
        Assert.Equal("Manual", item.Type);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task QueueOrderStatusChangedAsync_WhenTrackingCodeExists_UsesShipmentTrackingType()
    {
        var service = CreateService(out _);
        var order = FakeOrder(trackingCode: "TRK-123");

        var item = await service.QueueOrderStatusChangedAsync(order);

        Assert.Equal("ShipmentTrackingAdded", item.Type);
        Assert.Contains("TRK-123", item.Body);
        Assert.Equal(OrderId, item.RelatedOrderId);
    }

    [Fact]
    public async Task QueuePaymentResultAsync_ForVerifiedPayment_CreatesPaymentSucceededNotification()
    {
        var service = CreateService(out _);
        var receipt = new PaymentReceiptDto(Guid.NewGuid(), OrderId, "TK-1001", "Verified", "تأیید شده", "REF-1", "TRC-1", 980000m, DateTimeOffset.UtcNow, "موفق");

        var item = await service.QueuePaymentResultAsync(receipt, "09120000000");

        Assert.Equal("PaymentSucceeded", item.Type);
        Assert.Contains("TK-1001", item.Body);
        Assert.Equal("/account/orders", item.ActionUrl);
    }

    [Fact]
    public async Task MarkReadAsync_OnlyMarksCurrentCustomersNotification()
    {
        var service = CreateService(out var repo);
        var created = await service.AdminCreateAsync(new CreateNotificationRequest
        {
            CustomerMobile = "09120000000",
            Channel = "InApp",
            Type = "Manual",
            Subject = "خواندن",
            Body = "تست خواندن"
        });

        var updated = await service.MarkReadAsync("09120000000", created.Id);

        Assert.NotNull(updated);
        Assert.True(updated!.IsRead);
        Assert.Equal(0, await service.CountUnreadAsync("09120000000"));
    }

    [Fact]
    public async Task AdminCreateAsync_WithoutRecipientForCustomerNotification_RejectsRequest()
    {
        var service = CreateService(out _);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AdminCreateAsync(new CreateNotificationRequest
        {
            Channel = "InApp",
            Type = "Manual",
            Subject = "بدون گیرنده",
            Body = "نباید ثبت شود"
        }));

        Assert.Contains("شناسه مشتری", ex.Message);
    }

    private static NotificationService CreateService(out FakeNotificationRepository repo)
    {
        repo = new FakeNotificationRepository();
        return new NotificationService(repo, new FakeCustomerRepository());
    }

    private static OrderDto FakeOrder(string? trackingCode = null) => new(
        OrderId,
        "TK-1001",
        "مشتری تست",
        "09120000000",
        DateTimeOffset.UtcNow,
        "Shipped",
        "ارسال شده",
        "Paid",
        900000m,
        50000m,
        0m,
        950000m,
        trackingCode,
        null,
        "post-standard",
        "پست پیشتاز",
        new OrderAddressDto("مشتری تست", "09120000000", "تهران", "تهران", "1234567890", "آدرس تست", null, null),
        Array.Empty<OrderLineDto>());

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<NotificationDto> Items { get; } = [];

        public Task<ResultDto<NotificationDto>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
        {
            var item = new NotificationDto(
                Guid.NewGuid(),
                CustomerId,
                request.Channel,
                NotificationService.ChannelLabel(Enum.Parse<NotificationChannel>(request.Channel)),
                request.Type,
                NotificationService.TypeLabel(Enum.Parse<NotificationType>(request.Type)),
                request.MarkAsSent ? "Sent" : "Queued",
                NotificationService.StatusLabel(request.MarkAsSent ? NotificationStatus.Sent : NotificationStatus.Queued),
                request.Recipient ?? request.CustomerMobile ?? "09120000000",
                request.Subject,
                request.Body,
                request.RelatedOrderId,
                request.RelatedOrderNumber,
                request.RelatedProductId,
                request.ActionUrl,
                false,
                DateTimeOffset.UtcNow,
                request.MarkAsSent ? DateTimeOffset.UtcNow : null,
                null,
                null);
            Items.Add(item);
            return Task.FromResult(new ResultDto<NotificationDto>().Success("اعلان ایجاد شد.", item));
        }

        public Task<ResultDto<IReadOnlyCollection<NotificationDto>>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<NotificationDto>>().Success("اعلان‌ها دریافت شدند.", Items.Where(x => x.CustomerId == customerId).ToArray()));

        public Task<ResultDto<IReadOnlyCollection<NotificationDto>>> GetForAdminAsync(AdminNotificationFilter filter, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<NotificationDto>>().Success("اعلان‌ها دریافت شدند.", Items));

        public Task<ResultDto<int>> CountUnreadAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<int>().Success("تعداد اعلان‌ها دریافت شد.", Items.Count(x => x.CustomerId == customerId && !x.IsRead)));

        public Task<ResultDto<NotificationDto>> MarkReadAsync(Guid customerId, Guid notificationId, CancellationToken cancellationToken = default)
        {
            var item = Items.SingleOrDefault(x => x.Id == notificationId && x.CustomerId == customerId);
            if (item is null) return Task.FromResult(new ResultDto<NotificationDto>().NotFound("اعلان پیدا نشد."));
            var updated = item with { IsRead = true, ReadAt = DateTimeOffset.UtcNow };
            Items.Remove(item);
            Items.Add(updated);
            return Task.FromResult(new ResultDto<NotificationDto>().Success("اعلان خوانده شد.", updated));
        }

        public Task<ResultDto> MarkAllReadAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < Items.Count; i++)
            {
                if (Items[i].CustomerId == customerId)
                    Items[i] = Items[i] with { IsRead = true, ReadAt = DateTimeOffset.UtcNow };
            }
            return Task.FromResult(new ResultDto().Success("اعلان‌ها خوانده شدند."));
        }

        public Task<ResultDto<NotificationDto>> UpdateStatusAsync(Guid id, NotificationStatus status, string? failureReason, CancellationToken cancellationToken = default)
        {
            var item = Items.SingleOrDefault(x => x.Id == id);
            if (item is null) return Task.FromResult(new ResultDto<NotificationDto>().NotFound("اعلان پیدا نشد."));
            var updated = item with { Status = status.ToString(), StatusLabel = NotificationService.StatusLabel(status), FailureReason = failureReason };
            Items.Remove(item);
            Items.Add(updated);
            return Task.FromResult(new ResultDto<NotificationDto>().Success("وضعیت اعلان به‌روزرسانی شد.", updated));
        }
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Customer _customer = Customer.Create(CustomerId, "مشتری تست", "09120000000", "customer@example.com", DateTimeOffset.UnixEpoch);
        public Task<ResultDto<Customer>> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default)
            => Task.FromResult(mobile == _customer.Mobile ? new ResultDto<Customer>().Success("مشتری دریافت شد.", _customer) : new ResultDto<Customer>().NotFound("مشتری پیدا نشد."));
        public Task<ResultDto<Customer>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == _customer.Id ? new ResultDto<Customer>().Success("مشتری دریافت شد.", _customer) : new ResultDto<Customer>().NotFound("مشتری پیدا نشد."));
        public Task<ResultDto<IReadOnlyCollection<Customer>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Customer>>().Success("مشتریان دریافت شدند.", [_customer]));
        public Task<ResultDto<Customer>> UpsertAsync(Customer customer, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Customer>().Success("مشتری ذخیره شد.", customer));
        public Task<ResultDto<IReadOnlyCollection<Address>>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Address>>().Success("آدرس‌ها دریافت شدند.", Array.Empty<Address>()));
        public Task<ResultDto<Address>> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Address>().NotFound("آدرس پیدا نشد."));
        public Task<ResultDto<Address>> UpsertAddressAsync(Guid customerId, Address address, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Address>().Success("آدرس ذخیره شد.", address));
        public Task<ResultDto> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("آدرس حذف شد."));
    }
}
