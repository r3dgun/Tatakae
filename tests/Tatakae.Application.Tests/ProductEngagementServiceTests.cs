using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Tests;

public sealed class ProductEngagementServiceTests
{
    private static readonly Guid CustomerId = Guid.Parse("21000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId = Guid.Parse("22000000-0000-0000-0000-000000000001");
    private static readonly Guid VariantId = Guid.Parse("23000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task CreateReviewAsync_WhenOrderIsNotDelivered_RejectsReview()
    {
        var service = CreateService(orderStatus: OrderStatus.Paid);
        var request = new CreateProductReviewRequest { ProductId = ProductId, Rating = 5, Title = "کیفیت خوب", Body = "کیفیت پارچه و گلدوزی برای تست مناسب بود." };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateReviewAsync("09120000000", request));

        Assert.Contains("تحویل", ex.Message);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenDelivered_CreatesPendingBuyerReview()
    {
        var service = CreateService(orderStatus: OrderStatus.Delivered);
        var request = new CreateProductReviewRequest { ProductId = ProductId, Rating = 5, Title = "عالی", Body = "گلدوزی خیلی تمیز بود و بعد از شستشو خراب نشد." };

        var review = await service.CreateReviewAsync("09120000000", request);

        Assert.NotNull(review);
        Assert.Equal("Pending", review!.Status);
        Assert.True(review.IsBuyer);
    }

    [Fact]
    public async Task SubmitQuestionAsync_CreatesPendingQuestion()
    {
        var service = CreateService(orderStatus: OrderStatus.Delivered);

        var question = await service.SubmitQuestionAsync(new SubmitProductQuestionRequest { ProductId = ProductId, AuthorName = "مشتری", QuestionText = "برای گلدوزی لوگو مناسب است؟" });

        Assert.NotNull(question);
        Assert.False(question!.IsAnswered);
    }

    [Fact]
    public async Task ModerateQuestionAsync_WhenAnswered_ReturnsAnsweredQuestion()
    {
        var repo = new FakeEngagementRepository();
        var service = CreateService(orderStatus: OrderStatus.Delivered, engagement: repo);
        var question = await service.SubmitQuestionAsync(new SubmitProductQuestionRequest { ProductId = ProductId, AuthorName = "مشتری", QuestionText = "سوال تستی درباره محصول" });

        var moderated = await service.ModerateQuestionAsync(question!.Id, new AdminQuestionModerationRequest { Status = "Answered", AnswerText = "بله، مناسب است." });

        Assert.NotNull(moderated);
        Assert.Equal("Answered", moderated!.Status);
        Assert.Equal("بله، مناسب است.", moderated.AnswerText);
    }

    private static ProductEngagementService CreateService(OrderStatus orderStatus, FakeEngagementRepository? engagement = null)
    {
        var product = Product();
        var customer = Customer.Create(CustomerId, "مشتری تست", "09120000000", null, DateTimeOffset.UnixEpoch);
        var order = Order.Rehydrate(Guid.NewGuid(), "EMB-TEST-1001", CustomerId, customer.FullName, customer.Mobile,
            new Address(Guid.NewGuid(), customer.FullName, customer.Mobile, "تهران", "تهران", "1234567890", "آدرس تست", null, null, true),
            [new OrderLine(ProductId, VariantId, product.Name, product.Slug, "https://example.com/p.jpg", "TT-BLK-M", "M", "مشکی", "#111111", 1, 900000m, new EmbroideryConfiguration(Guid.NewGuid(), EmbroideryPlacement.LeftChest, 8, 8, 1, ["#111111"], null, null, null, null, null, 0, "TShirt", "M", "#111111", "Motif", "dragon", 50, 50, 100, 0, 100))],
            0, 0, "post", "پست", DateTimeOffset.UtcNow, orderStatus, PaymentStatus.Paid, 900000m, 900000m, orderStatus is OrderStatus.Shipped or OrderStatus.Delivered ? "TRK-TEST" : null, null);

        return new ProductEngagementService(
            engagement ?? new FakeEngagementRepository(),
            new FakeProductRepository([product]),
            new FakeCustomerRepository(customer),
            new FakeOrderRepository([order]));
    }

    private static Product Product() => Tatakae.Domain.Entities.Product.Create(
        ProductId,
        "تی‌شرت تست",
        "test-tee",
        ApparelCategory.TShirt,
        Guid.NewGuid(),
        "توضیح کوتاه محصول تستی برای بررسی نظر",
        "توضیح کامل محصول تستی برای بررسی نظر و پرسش و پاسخ.",
        "پنبه",
        "Regular",
        "شستشو با آب سرد",
        "",
        new SeoMetadata("title", "description", null, null, true, true),
        new EmbroideryPolicy(0, 0, 0, 8, 20, 20, [EmbroideryPlacement.LeftChest], ["#111111"]),
        [new ProductImage(Guid.NewGuid(), "https://example.com/p.jpg", "محصول", true, 0)],
        [new ProductVariant(VariantId, "TT-BLK-M", "M", "مشکی", "#111111", 900000m, null, 5)],
        Array.Empty<ProductSpecification>(),
        ["تست"],
        true,
        false,
        true,
        DateTimeOffset.UnixEpoch);

    private sealed class FakeEngagementRepository : IProductEngagementRepository
    {
        private readonly List<ProductReviewDto> _reviews = [];
        private readonly List<AdminProductQuestionDto> _questions = [];

        public Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> GetApprovedReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<ProductReviewDto>>().Success("نظرها دریافت شدند.", _reviews.Where(x => x.ProductId == productId && x.Status == "Approved").ToArray()));

        public Task<ResultDto<ProductRatingSummaryDto>> GetRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<ProductRatingSummaryDto>().Success("خلاصه امتیاز دریافت شد.", new ProductRatingSummaryDto(productId, 0, 0, new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 }, 0, 0)));

        public Task<ResultDto<IReadOnlyCollection<AdminProductReviewDto>>> GetReviewsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<AdminProductReviewDto>>().Success("نظرها دریافت شدند.", Array.Empty<AdminProductReviewDto>()));

        public Task<ResultDto<ProductReviewDto>> AddReviewAsync(ProductReviewSubmission submission, CancellationToken cancellationToken = default)
        {
            var review = new ProductReviewDto(Guid.NewGuid(), submission.ProductId, "تی‌شرت تست", "مشتری", submission.Rating, submission.Title, submission.Body, submission.RecommendProduct, submission.IsBuyer, "Pending", "در انتظار بررسی", Array.Empty<string>(), Array.Empty<string>(), null, null, DateTimeOffset.UtcNow);
            _reviews.Add(review);
            return Task.FromResult(new ResultDto<ProductReviewDto>().Success("نظر ثبت شد.", review));
        }

        public Task<ResultDto<bool>> HasCustomerReviewedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<bool>().Success("وضعیت نظر دریافت شد.", _reviews.Any(x => x.ProductId == productId)));

        public Task<ResultDto<AdminProductReviewDto>> ModerateReviewAsync(Guid reviewId, ReviewStatus status, string? adminReply, string? moderationNote, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<AdminProductReviewDto>().NotFound("نظر پیدا نشد."));

        public Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> GetPublicQuestionsAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<ProductQuestionDto>>().Success("پرسش‌ها دریافت شدند.", Array.Empty<ProductQuestionDto>()));

        public Task<ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>> GetQuestionsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>().Success("پرسش‌ها دریافت شدند.", _questions));

        public Task<ResultDto<ProductQuestionDto>> AddQuestionAsync(ProductQuestionSubmission submission, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            _questions.Add(new AdminProductQuestionDto(id, submission.ProductId, "تی‌شرت تست", submission.CustomerId, submission.AuthorName, submission.Mobile, submission.QuestionText, null, "Pending", "در انتظار پاسخ", null, DateTimeOffset.UtcNow, null));
            var question = new ProductQuestionDto(id, submission.ProductId, submission.AuthorName, submission.QuestionText, null, DateTimeOffset.UtcNow, null, false);
            return Task.FromResult(new ResultDto<ProductQuestionDto>().Success("پرسش ثبت شد.", question));
        }

        public Task<ResultDto<AdminProductQuestionDto>> ModerateQuestionAsync(Guid questionId, QuestionStatus status, string? answerText, string? moderationNote, Guid? answeredByUserId, CancellationToken cancellationToken = default)
        {
            var old = _questions.SingleOrDefault(x => x.Id == questionId);
            if (old is null) return Task.FromResult(new ResultDto<AdminProductQuestionDto>().NotFound("پرسش پیدا نشد."));
            old.Status = status.ToString();
            old.StatusLabel = "پاسخ داده شده";
            old.AnswerText = answerText;
            old.AnsweredAt = DateTimeOffset.UtcNow;
            return Task.FromResult(new ResultDto<AdminProductQuestionDto>().Success("پرسش بررسی شد.", old));
        }
    }

    private sealed class FakeProductRepository(IReadOnlyCollection<Product> products) : IProductRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Product>>().Success("محصولات دریافت شدند.", products));
        public Task<ResultDto<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = products.SingleOrDefault(x => x.Id == id);
            var result = new ResultDto<Product>();
            return Task.FromResult(item is null ? result.NotFound("محصول پیدا نشد.") : result.Success("محصول دریافت شد.", item));
        }
        public Task<ResultDto<Product>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = products.SingleOrDefault(x => x.Slug == slug);
            var result = new ResultDto<Product>();
            return Task.FromResult(item is null ? result.NotFound("محصول پیدا نشد.") : result.Success("محصول دریافت شد.", item));
        }
        public Task<ResultDto<Product>> UpsertAsync(Product product, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Product>().Success("محصول ذخیره شد.", product));
        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("محصول حذف شد."));
    }

    private sealed class FakeCustomerRepository(Customer customer) : ICustomerRepository
    {
        public Task<ResultDto<Customer>> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default)
            => Task.FromResult(mobile == customer.Mobile ? new ResultDto<Customer>().Success("مشتری دریافت شد.", customer) : new ResultDto<Customer>().NotFound("مشتری پیدا نشد."));
        public Task<ResultDto<Customer>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == customer.Id ? new ResultDto<Customer>().Success("مشتری دریافت شد.", customer) : new ResultDto<Customer>().NotFound("مشتری پیدا نشد."));
        public Task<ResultDto<IReadOnlyCollection<Customer>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Customer>>().Success("مشتریان دریافت شدند.", [customer]));
        public Task<ResultDto<Customer>> UpsertAsync(Customer item, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Customer>().Success("مشتری ذخیره شد.", item));
        public Task<ResultDto<IReadOnlyCollection<Address>>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Address>>().Success("آدرس‌ها دریافت شدند.", Array.Empty<Address>()));
        public Task<ResultDto<Address>> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Address>().NotFound("آدرس پیدا نشد."));
        public Task<ResultDto<Address>> UpsertAddressAsync(Guid customerId, Address address, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Address>().Success("آدرس ذخیره شد.", address));
        public Task<ResultDto> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("آدرس حذف شد."));
    }

    private sealed class FakeOrderRepository(IReadOnlyCollection<Order> orders) : IOrderRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Order>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Order>>().Success("سفارش‌ها دریافت شدند.", orders));
        public Task<ResultDto<Order>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = orders.SingleOrDefault(x => x.Id == id);
            var result = new ResultDto<Order>();
            return Task.FromResult(item is null ? result.NotFound("سفارش پیدا نشد.") : result.Success("سفارش دریافت شد.", item));
        }
        public Task<ResultDto<Order>> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var item = orders.SingleOrDefault(x => x.OrderNumber == orderNumber);
            var result = new ResultDto<Order>();
            return Task.FromResult(item is null ? result.NotFound("سفارش پیدا نشد.") : result.Success("سفارش دریافت شد.", item));
        }
        public Task<ResultDto<Order>> AddAsync(Order order, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().Success("سفارش ثبت شد.", order));
        public Task<ResultDto<Order>> UpdateAsync(Order order, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().Success("سفارش به‌روزرسانی شد.", order));
        public Task<ResultDto<IReadOnlyCollection<Tatakae.Application.Contracts.Orders.OrderStatusHistoryDto>>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Tatakae.Application.Contracts.Orders.OrderStatusHistoryDto>>().Success("تاریخچه دریافت شد.", Array.Empty<Tatakae.Application.Contracts.Orders.OrderStatusHistoryDto>()));
        public Task<ResultDto<Tatakae.Application.Contracts.Orders.OrderStatusHistoryDto>> AddStatusHistoryAsync(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string title, string? note, string? trackingCode, string changedBy, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Tatakae.Application.Contracts.Orders.OrderStatusHistoryDto>().Success("تاریخچه ثبت شد.", new Tatakae.Application.Contracts.Orders.OrderStatusHistoryDto(Guid.NewGuid(), orderId, fromStatus?.ToString(), null, toStatus.ToString(), toStatus.ToString(), title, note, trackingCode, changedBy, DateTimeOffset.UtcNow)));
    }
}
