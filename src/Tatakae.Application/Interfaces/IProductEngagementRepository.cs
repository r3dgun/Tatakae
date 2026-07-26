using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Domain.Enums;

namespace Tatakae.Application.Interfaces;

public sealed record ProductReviewSubmission(
    Guid ProductId,
    Guid CustomerId,
    Guid? OrderLineId,
    int Rating,
    string Title,
    string Body,
    bool RecommendProduct,
    bool IsBuyer,
    string? PositivePointsCsv,
    string? NegativePointsCsv);

public sealed record ProductQuestionSubmission(
    Guid ProductId,
    Guid? CustomerId,
    string AuthorName,
    string? Mobile,
    string QuestionText);

public interface IProductEngagementRepository
{
    Task<ResultDto<IReadOnlyCollection<ProductReviewDto>>> GetApprovedReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductRatingSummaryDto>> GetRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<AdminProductReviewDto>>> GetReviewsForAdminAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductReviewDto>> AddReviewAsync(ProductReviewSubmission submission, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> HasCustomerReviewedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<AdminProductReviewDto>> ModerateReviewAsync(Guid reviewId, ReviewStatus status, string? adminReply, string? moderationNote, CancellationToken cancellationToken = default);

    Task<ResultDto<IReadOnlyCollection<ProductQuestionDto>>> GetPublicQuestionsAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ResultDto<IReadOnlyCollection<AdminProductQuestionDto>>> GetQuestionsForAdminAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ResultDto<ProductQuestionDto>> AddQuestionAsync(ProductQuestionSubmission submission, CancellationToken cancellationToken = default);
    Task<ResultDto<AdminProductQuestionDto>> ModerateQuestionAsync(Guid questionId, QuestionStatus status, string? answerText, string? moderationNote, Guid? answeredByUserId, CancellationToken cancellationToken = default);
}
