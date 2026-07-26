using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Tests;

public sealed class EmbroideryArtworkServiceTests
{
    private static readonly Guid CustomerId = Guid.Parse("31000000-0000-0000-0000-000000000001");
    private static readonly Guid MediaId = Guid.Parse("32000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task SubmitAsync_WithValidArtwork_CreatesPendingReviewArtwork()
    {
        var repo = new FakeArtworkRepository();
        var service = new EmbroideryArtworkService(repo, new FakeCustomerRepository());

        var result = await service.SubmitAsync("09120000000", new SubmitEmbroideryArtworkRequest
        {
            MediaAssetId = MediaId,
            WidthCm = 8,
            HeightCm = 9,
            ThreadColorCount = 3,
            CustomerNote = "طرح روی سینه چپ اجرا شود."
        });

        Assert.NotNull(result);
        Assert.Equal("PendingReview", result!.Status);
        Assert.Equal("در انتظار بررسی", result.StatusLabel);
        Assert.Equal(CustomerId, result.CustomerId);
    }

    [Fact]
    public async Task SubmitAsync_WithTooManyThreadColors_RejectsArtwork()
    {
        var service = new EmbroideryArtworkService(new FakeArtworkRepository(), new FakeCustomerRepository());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync("09120000000", new SubmitEmbroideryArtworkRequest
        {
            MediaAssetId = MediaId,
            WidthCm = 8,
            HeightCm = 9,
            ThreadColorCount = 24
        }));

        Assert.Contains("رنگ", ex.Message);
    }

    [Fact]
    public async Task AdminModerateAsync_WhenNeedsRevisionWithoutReason_RejectsRequest()
    {
        var service = new EmbroideryArtworkService(new FakeArtworkRepository(), new FakeCustomerRepository());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AdminModerateAsync(Guid.NewGuid(), new AdminArtworkModerationRequest
        {
            Status = "NeedsRevision"
        }));

        Assert.Contains("دلیل", ex.Message);
    }

    [Fact]
    public async Task AdminModerateAsync_WhenApproved_ChangesStatus()
    {
        var repo = new FakeArtworkRepository();
        var service = new EmbroideryArtworkService(repo, new FakeCustomerRepository());
        var submitted = await service.SubmitAsync("09120000000", new SubmitEmbroideryArtworkRequest
        {
            MediaAssetId = MediaId,
            WidthCm = 8,
            HeightCm = 9,
            ThreadColorCount = 2
        });

        var approved = await service.AdminModerateAsync(submitted!.Id, new AdminArtworkModerationRequest
        {
            Status = "Approved",
            AdminNote = "برای تولید آماده است.",
            ProductionFileExtension = "DST"
        });

        Assert.Equal("Approved", approved.Status);
        Assert.Equal("تأیید شده", approved.StatusLabel);
        Assert.Equal("DST", approved.ProductionFileExtension);
    }

    private sealed class FakeArtworkRepository : IEmbroideryArtworkRepository
    {
        private readonly List<EmbroideryArtworkDto> _items = [];

        public Task<ResultDto<EmbroideryArtworkDto>> SubmitAsync(Guid? customerId, SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default)
        {
            var item = new EmbroideryArtworkDto(
                Guid.NewGuid(),
                request.MediaAssetId,
                customerId,
                request.ProductId,
                request.OrderId,
                request.OrderLineId,
                "logo.dst",
                "application/x-dst",
                "https://example.com/uploads/logo.dst",
                2500,
                "PendingReview",
                "در انتظار بررسی",
                request.WidthCm,
                request.HeightCm,
                request.ThreadColorCount,
                request.CustomerNote,
                null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow,
                null);
            _items.Add(item);
            return Task.FromResult(new ResultDto<EmbroideryArtworkDto>().Success("طرح ثبت شد.", item));
        }

        public Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>().Success("طرح‌ها دریافت شدند.", _items.Where(x => x.CustomerId == customerId).ToArray()));

        public Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> GetForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>().Success("طرح‌ها دریافت شدند.", _items.Where(x => string.IsNullOrWhiteSpace(status) || x.Status == status).ToArray()));

        public Task<ResultDto<EmbroideryArtworkDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = _items.SingleOrDefault(x => x.Id == id);
            var result = new ResultDto<EmbroideryArtworkDto>();
            return Task.FromResult(item is null ? result.NotFound("طرح پیدا نشد.") : result.Success("طرح دریافت شد.", item));
        }

        public Task<ResultDto<EmbroideryArtworkDto>> ModerateAsync(Guid id, EmbroideryArtworkStatus status, string? adminNote, string? rejectionReason, string? previewImageUrl, string? productionFileExtension, CancellationToken cancellationToken = default)
        {
            var item = _items.SingleOrDefault(x => x.Id == id);
            if (item is null) return Task.FromResult(new ResultDto<EmbroideryArtworkDto>().NotFound("طرح پیدا نشد."));
            var updated = item with
            {
                Status = status.ToString(),
                StatusLabel = status switch
                {
                    EmbroideryArtworkStatus.Approved => "تأیید شده",
                    EmbroideryArtworkStatus.Rejected => "رد شده",
                    EmbroideryArtworkStatus.NeedsRevision => "نیازمند اصلاح",
                    EmbroideryArtworkStatus.Archived => "آرشیو شده",
                    _ => "در انتظار بررسی"
                },
                AdminNote = adminNote,
                RejectionReason = rejectionReason,
                PreviewImageUrl = previewImageUrl,
                ProductionFileExtension = productionFileExtension,
                ReviewedAt = DateTimeOffset.UtcNow
            };
            _items.Remove(item);
            _items.Add(updated);
            return Task.FromResult(new ResultDto<EmbroideryArtworkDto>().Success("طرح بررسی شد.", updated));
        }
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Customer _customer = Customer.Create(CustomerId, "مشتری تست", "09120000000", null, DateTimeOffset.UnixEpoch);

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
