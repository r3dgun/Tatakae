using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Embroidery;
using Tatakae.Application.Contracts.Orders;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Interfaces;
using Tatakae.Application.Services;
using Tatakae.Domain.Entities;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Tests;

public sealed class AdminDashboardServiceTests
{
    private static readonly Guid CategoryId = Guid.Parse("91000000-0000-0000-0000-000000000001");
    private static readonly Guid CustomerId = Guid.Parse("92000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetAsync_ReturnsSalesInventoryAndWorkflowMetrics()
    {
        var product = Product("ink-tee", stock: 6, reserved: 1);
        var lowStock = Product("low-stock-hoodie", stock: 2, lowStockThreshold: 3);
        var paidOrder = PaidOrder(product, 2, 900_000m);
        var embroideryOrder = PaidOrder(product, 1, 900_000m);
        embroideryOrder.ChangeStatus(OrderStatus.ArtworkReview);
        embroideryOrder.ChangeStatus(OrderStatus.InEmbroidery);
        var pendingPayment = Order(product, 1, 900_000m);

        var service = CreateService(
            [product, lowStock],
            [paidOrder, embroideryOrder, pendingPayment],
            pendingReviews: 2,
            pendingQuestions: 1,
            pendingArtworks: 3,
            revisionArtworks: 1);

        var dashboard = await service.GetAsync();

        Assert.Equal(2, dashboard.ProductCount);
        Assert.Equal(3, dashboard.OrderCount);
        Assert.Equal(1, dashboard.PendingPaymentCount);
        Assert.Equal(1, dashboard.InEmbroideryOrderCount);
        Assert.Equal(1, dashboard.LowStockVariantCount);
        Assert.Equal(3, dashboard.PendingArtworkCount);
        Assert.Equal(2, dashboard.PendingReviewCount);
        Assert.Equal(1, dashboard.UnansweredQuestionCount);
        Assert.True(dashboard.GrossSales > 0);
        Assert.Contains(dashboard.OrderPipeline, x => x.Status == "InEmbroidery" && x.Count == 1);
    }

    [Fact]
    public async Task GetAsync_BuildsActionItemsForPendingWork()
    {
        var product = Product("sold-out-tee", stock: 0);
        var order = Order(product, 1, 850_000m);
        var service = CreateService([product], [order], pendingReviews: 1, pendingQuestions: 1, pendingArtworks: 1, revisionArtworks: 0);

        var dashboard = await service.GetAsync();

        Assert.Contains(dashboard.ActionItems, x => x.Link == "/admin/payments" && x.Count == 1);
        Assert.Contains(dashboard.ActionItems, x => x.Link == "/admin/artworks" && x.Count == 1);
        Assert.Contains(dashboard.ActionItems, x => x.Link == "/admin/reviews" && x.Count == 1);
        Assert.Contains(dashboard.ActionItems, x => x.Link == "/admin/inventory" && x.Title.Contains("ناموجود"));
    }

    [Fact]
    public async Task GetAsync_ReturnsTopProductsFromPaidOrdersOnly()
    {
        var best = Product("best-seller", stock: 20);
        var ignored = Product("pending-order-product", stock: 20);
        var paid = PaidOrder(best, 3, 700_000m);
        var pending = Order(ignored, 9, 100_000m);
        var service = CreateService([best, ignored], [pending, paid]);

        var dashboard = await service.GetAsync();

        Assert.Single(dashboard.TopProducts);
        Assert.Equal(best.Id, dashboard.TopProducts.Single().ProductId);
        Assert.Equal(3, dashboard.TopProducts.Single().QuantitySold);
    }

    private static AdminDashboardService CreateService(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<Order> orders,
        int pendingReviews = 0,
        int pendingQuestions = 0,
        int pendingArtworks = 0,
        int revisionArtworks = 0)
        => new(
            new FakeProductRepository(products),
            new FakeOrderRepository(orders),
            new FakeCategoryRepository([new Category(CategoryId, "تی‌شرت", "tshirts", "", null, Seo())]),
            new FakeEngagementRepository(pendingReviews, pendingQuestions),
            new FakeArtworkRepository(pendingArtworks, revisionArtworks));

    private static Order PaidOrder(Product product, int quantity, decimal unitPrice)
    {
        var order = Order(product, quantity, unitPrice);
        order.MarkPaid();
        return order;
    }

    private static Order Order(Product product, int quantity, decimal unitPrice)
        => Tatakae.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            $"EMB-TEST-{Guid.NewGuid():N}".ToUpperInvariant(),
            CustomerId,
            "مشتری تست",
            "09120000000",
            new Address(Guid.NewGuid(), "مشتری تست", "09120000000", "تهران", "تهران", "1234567890", "خیابان تست", "1", "2", true),
            [new OrderLine(product.Id, product.Variants.Single().Id, product.Name, product.Slug, product.Images.Single().Url, product.Variants.Single().Sku, "M", "مشکی", "#111111", quantity, unitPrice, Embroidery(unitPrice / 10))],
            50_000m,
            0m,
            "post",
            "پست پیشتاز",
            DateTimeOffset.UtcNow);

    private static Product Product(string slug, int stock, int reserved = 0, int lowStockThreshold = 3)
        => Tatakae.Domain.Entities.Product.Create(
            Guid.NewGuid(),
            slug.Replace('-', ' '),
            slug,
            ApparelCategory.TShirt,
            CategoryId,
            "توضیح کوتاه",
            "توضیح کامل محصول تستی",
            "پنبه",
            "Regular",
            "شستشو با آب سرد",
            "",
            Seo(),
            Policy(),
            [new ProductImage(Guid.NewGuid(), "https://example.com/p.jpg", "محصول", true, 0)],
            [new ProductVariant(Guid.NewGuid(), $"TT-{slug.ToUpperInvariant()}", "M", "مشکی", "#111111", 900_000m, null, stock, reservedQuantity: reserved, lowStockThreshold: lowStockThreshold)],
            Array.Empty<ProductSpecification>(),
            ["test"],
            isPublished: true,
            isFeatured: false,
            supportsEmbroidery: true,
            createdAt: DateTimeOffset.UnixEpoch);

    private static SeoMetadata Seo() => new("title", "description", null, null, true, true);
    private static EmbroideryPolicy Policy() => new(0, 0, 0, 8, 20, 20, [EmbroideryPlacement.LeftChest], ["#111111"]);
    private static EmbroideryConfiguration Embroidery(decimal price) => new(Guid.NewGuid(), EmbroideryPlacement.LeftChest, 8, 8, 2, ["#111111", "#ffffff"], null, null, null, null, null, price);

    private sealed class FakeProductRepository(IReadOnlyCollection<Product> products) : IProductRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Product>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Product>>().Success("محصولات دریافت شدند.", products));

        public Task<ResultDto<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = products.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(item is null
                ? new ResultDto<Product>().NotFound("محصول پیدا نشد.")
                : new ResultDto<Product>().Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = products.SingleOrDefault(x => x.Slug == slug);
            return Task.FromResult(item is null
                ? new ResultDto<Product>().NotFound("محصول پیدا نشد.")
                : new ResultDto<Product>().Success("محصول دریافت شد.", item));
        }

        public Task<ResultDto<Product>> UpsertAsync(Product product, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Product>().Success("محصول ذخیره شد.", product));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("محصول حذف شد."));
    }

    private sealed class FakeOrderRepository(IReadOnlyCollection<Order> orders) : IOrderRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Order>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Order>>().Success("سفارش‌ها دریافت شدند.", orders));

        public Task<ResultDto<Order>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = orders.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(item is null
                ? new ResultDto<Order>().NotFound("سفارش پیدا نشد.")
                : new ResultDto<Order>().Success("سفارش دریافت شد.", item));
        }

        public Task<ResultDto<Order>> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var item = orders.SingleOrDefault(x => x.OrderNumber == orderNumber);
            return Task.FromResult(item is null
                ? new ResultDto<Order>().NotFound("سفارش پیدا نشد.")
                : new ResultDto<Order>().Success("سفارش دریافت شد.", item));
        }

        public Task<ResultDto<Order>> AddAsync(Order order, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().Success("سفارش ثبت شد.", order));

        public Task<ResultDto<Order>> UpdateAsync(Order order, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Order>().Success("سفارش به‌روزرسانی شد.", order));

        public Task<ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<OrderStatusHistoryDto>>().Success("تاریخچه سفارش دریافت شد.", Array.Empty<OrderStatusHistoryDto>()));

        public Task<ResultDto<OrderStatusHistoryDto>> AddStatusHistoryAsync(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string title, string? note, string? trackingCode, string changedBy, CancellationToken cancellationToken = default)
        {
            var item = new OrderStatusHistoryDto(Guid.NewGuid(), orderId, fromStatus?.ToString(), fromStatus?.ToString(), toStatus.ToString(), toStatus.ToString(), title, note, trackingCode, changedBy, DateTimeOffset.UtcNow);
            return Task.FromResult(new ResultDto<OrderStatusHistoryDto>().Success("تاریخچه سفارش ثبت شد.", item));
        }
    }

    private sealed class FakeCategoryRepository(IReadOnlyCollection<Category> categories) : ICategoryRepository
    {
        public Task<ResultDto<IReadOnlyCollection<Category>>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<Category>>().Success("دسته‌بندی‌ها دریافت شدند.", categories));

        public Task<ResultDto<Category>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = categories.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(item is null
                ? new ResultDto<Category>().NotFound("دسته‌بندی پیدا نشد.")
                : new ResultDto<Category>().Success("دسته‌بندی دریافت شد.", item));
        }

        public Task<ResultDto<Category>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var item = categories.SingleOrDefault(x => x.Slug == slug);
            return Task.FromResult(item is null
                ? new ResultDto<Category>().NotFound("دسته‌بندی پیدا نشد.")
                : new ResultDto<Category>().Success("دسته‌بندی دریافت شد.", item));
        }

        public Task<ResultDto<Category>> UpsertAsync(Category category, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<Category>().Success("دسته‌بندی ذخیره شد.", category));

        public Task<ResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto().Success("دسته‌بندی حذف شد."));
    }

    private sealed class FakeEngagementRepository(int pendingReviews, int pendingQuestions) : IProductEngagementRepository
    {
        public Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> GetApprovedReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<ProductReviewDto>>().Success("نظرها دریافت شدند.", Array.Empty<ProductReviewDto>()));

        public Task<ResultDto<ProductRatingSummaryDto>> GetRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<ProductRatingSummaryDto>().Success("خلاصه امتیاز دریافت شد.", new ProductRatingSummaryDto(productId, 0, 0, new Dictionary<int, int>(), 0, 0)));

        public Task<ResultDto<IReadOnlyCollection<AdminProductReviewDto>>> GetReviewsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<AdminProductReviewDto>>().Success("نظرها دریافت شدند.", Enumerable.Range(0, pendingReviews).Select(_ => new AdminProductReviewDto { Id = Guid.NewGuid(), Status = status ?? "Pending" }).ToArray()));

        public Task<ResultDto<ProductReviewDto>> AddReviewAsync(ProductReviewSubmission submission, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<ProductReviewDto>().NotFound("نظر ثبت نشد."));

        public Task<ResultDto<bool>> HasCustomerReviewedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<bool>().Success("وضعیت نظر دریافت شد.", false));

        public Task<ResultDto<AdminProductReviewDto>> ModerateReviewAsync(Guid reviewId, ReviewStatus status, string? adminReply, string? moderationNote, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<AdminProductReviewDto>().NotFound("نظر پیدا نشد."));

        public Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> GetPublicQuestionsAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<ProductQuestionDto>>().Success("پرسش‌ها دریافت شدند.", Array.Empty<ProductQuestionDto>()));

        public Task<ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>> GetQuestionsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>().Success("پرسش‌ها دریافت شدند.", Enumerable.Range(0, pendingQuestions).Select(_ => new AdminProductQuestionDto { Id = Guid.NewGuid(), Status = status ?? "Pending" }).ToArray()));

        public Task<ResultDto<ProductQuestionDto>> AddQuestionAsync(ProductQuestionSubmission submission, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<ProductQuestionDto>().NotFound("پرسش ثبت نشد."));

        public Task<ResultDto<AdminProductQuestionDto>> ModerateQuestionAsync(Guid questionId, QuestionStatus status, string? answerText, string? moderationNote, Guid? answeredByUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<AdminProductQuestionDto>().NotFound("پرسش پیدا نشد."));
    }

    private sealed class FakeArtworkRepository(int pendingArtworks, int revisionArtworks) : IEmbroideryArtworkRepository
    {
        public Task<ResultDto<EmbroideryArtworkDto>> SubmitAsync(Guid? customerId, SubmitEmbroideryArtworkRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<EmbroideryArtworkDto>().NotFound("طرح ثبت نشد."));

        public Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>().Success("طرح‌ها دریافت شدند.", Array.Empty<EmbroideryArtworkDto>()));

        public Task<ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>> GetForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            var count = status == "NeedsRevision" ? revisionArtworks : pendingArtworks;
            var items = Enumerable.Range(0, count).Select(_ => Artwork(status ?? "PendingReview")).ToArray();
            return Task.FromResult(new ResultDto<IReadOnlyCollection<EmbroideryArtworkDto>>().Success("طرح‌ها دریافت شدند.", items));
        }

        public Task<ResultDto<EmbroideryArtworkDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<EmbroideryArtworkDto>().NotFound("طرح پیدا نشد."));

        public Task<ResultDto<EmbroideryArtworkDto>> ModerateAsync(Guid id, EmbroideryArtworkStatus status, string? adminNote, string? rejectionReason, string? previewImageUrl, string? productionFileExtension, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResultDto<EmbroideryArtworkDto>().NotFound("طرح پیدا نشد."));

        private static EmbroideryArtworkDto Artwork(string status) => new(Guid.NewGuid(), Guid.NewGuid(), CustomerId, null, null, null, "logo.dst", "application/x-dst", "https://example.com/logo.dst", 1200, status, status, 8, 8, 2, null, null, null, null, null, DateTimeOffset.UtcNow, null);
    }

}
