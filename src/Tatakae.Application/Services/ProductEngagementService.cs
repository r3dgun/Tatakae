using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tatakae.Application.Interfaces.Services;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Enums;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Application.Services;

public sealed partial class ProductEngagementService(
    IProductEngagementRepository engagement,
    IProductRepository products,
    ICustomerRepository customers,
    IOrderRepository orders,
    ILogger<ProductEngagementService>? logger = null) : IProductEngagementService
{
    private readonly ILogger<ProductEngagementService> _logger = logger ?? NullLogger<ProductEngagementService>.Instance;
    public async Task<IReadOnlyCollection<ProductReviewDto>> GetApprovedReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
        => (await engagement.GetApprovedReviewsAsync(productId, cancellationToken)).RequireData();

    public async Task<ProductRatingSummaryDto> GetRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default)
        => (await engagement.GetRatingSummaryAsync(productId, cancellationToken)).RequireData();

    public async Task<ProductReviewDto?> CreateReviewAsync(string mobile, CreateProductReviewRequest request, CancellationToken cancellationToken = default)
    {
        var customer = (await customers.GetByMobileAsync(mobile, cancellationToken)).DataOrDefault();
        if (customer is null) return null;

        var product = (await products.GetByIdAsync(request.ProductId, cancellationToken)).DataOrDefault();
        if (product is null || !product.IsPublished) return null;

        if ((await engagement.HasCustomerReviewedAsync(customer.Id, request.ProductId, cancellationToken)).RequireData())
            throw new InvalidOperationException("برای این محصول قبلاً نظر ثبت کرده‌ای.");

        var hasDeliveredPurchase = await HasDeliveredPurchaseAsync(customer.Id, request.ProductId, cancellationToken);
        if (!hasDeliveredPurchase)
            throw new InvalidOperationException("ثبت نظر فقط بعد از خرید و تحویل سفارش فعال است.");

        return (await engagement.AddReviewAsync(new ProductReviewSubmission(
            request.ProductId,
            customer.Id,
            null,
            request.Rating,
            request.Title,
            request.Body,
            request.RecommendProduct,
            IsBuyer: true,
            request.PositivePointsCsv,
            request.NegativePointsCsv), cancellationToken)).DataOrDefault();
    }

    public async Task<IReadOnlyCollection<AdminProductReviewDto>> GetReviewsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
        => (await engagement.GetReviewsForAdminAsync(status, cancellationToken)).RequireData();

    public async Task<AdminProductReviewDto?> ModerateReviewAsync(Guid reviewId, AdminReviewModerationRequest request, CancellationToken cancellationToken = default)
    {
        var status = Enum.Parse<ReviewStatus>(request.Status, ignoreCase: true);
        return (await engagement.ModerateReviewAsync(reviewId, status, request.AdminReply, request.ModerationNote, cancellationToken)).DataOrDefault();
    }

    public async Task<IReadOnlyCollection<ProductQuestionDto>> GetPublicQuestionsAsync(Guid productId, CancellationToken cancellationToken = default)
        => (await engagement.GetPublicQuestionsAsync(productId, cancellationToken)).RequireData();

    public async Task<ProductQuestionDto?> SubmitQuestionAsync(SubmitProductQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var product = (await products.GetByIdAsync(request.ProductId, cancellationToken)).DataOrDefault();
        if (product is null || !product.IsPublished) return null;

        var customer = string.IsNullOrWhiteSpace(request.Mobile) ? null : (await customers.GetByMobileAsync(request.Mobile, cancellationToken)).DataOrDefault();
        return (await engagement.AddQuestionAsync(new ProductQuestionSubmission(
            request.ProductId,
            customer?.Id,
            request.AuthorName,
            request.Mobile,
            request.QuestionText), cancellationToken)).DataOrDefault();
    }

    public async Task<IReadOnlyCollection<AdminProductQuestionDto>> GetQuestionsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
        => (await engagement.GetQuestionsForAdminAsync(status, cancellationToken)).RequireData();

    public async Task<AdminProductQuestionDto?> ModerateQuestionAsync(Guid questionId, AdminQuestionModerationRequest request, Guid? answeredByUserId = null, CancellationToken cancellationToken = default)
    {
        var status = Enum.Parse<QuestionStatus>(request.Status, ignoreCase: true);
        return (await engagement.ModerateQuestionAsync(questionId, status, request.AnswerText, request.ModerationNote, answeredByUserId, cancellationToken)).DataOrDefault();
    }

    private async Task<bool> HasDeliveredPurchaseAsync(Guid customerId, Guid productId, CancellationToken cancellationToken)
    {
        var all = (await orders.GetAllAsync(cancellationToken)).RequireData();
        return all
            .Where(x => x.CustomerId == customerId && x.Status == OrderStatus.Delivered)
            .SelectMany(x => x.Lines)
            .Any(x => x.ProductId == productId);
    }
}
