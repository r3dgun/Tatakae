using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tatakae.Application.Contracts.Products;
using Tatakae.Application.Contracts.Reviews;
using Tatakae.Application.Interfaces;
using Tatakae.Domain.Enums;
using Tatakae.Infrastructure.Persistence.Models;

namespace Tatakae.Infrastructure.Persistence.Repositories;

public sealed partial class SqlProductEngagementRepository(
    TatakaeDbContext db,
    ILogger<SqlProductEngagementRepository>? logger = null) : IProductEngagementRepository
{
    private readonly ILogger<SqlProductEngagementRepository> _resultLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlProductEngagementRepository>.Instance;

    private async Task<IReadOnlyCollection<ProductReviewDto>> GetApprovedReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var rows = await ReviewQuery()
            .Where(x => x.ProductId == productId && x.Status == ReviewStatus.Approved)
            .OrderByDescending(x => x.IsBuyer)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(ToPublicReview).ToArray();
    }

    private async Task<ProductRatingSummaryDto> GetRatingSummaryAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var rows = await db.ProductReviews.AsNoTracking()
            .Where(x => x.ProductId == productId && x.Status == ReviewStatus.Approved)
            .ToListAsync(cancellationToken);

        var histogram = Enumerable.Range(1, 5).ToDictionary(x => x, x => rows.Count(r => r.Rating == x));
        var avg = rows.Count == 0 ? 0m : Math.Round((decimal)rows.Average(x => x.Rating), 1);
        var recommend = rows.Count == 0 ? 0 : (int)Math.Round(rows.Count(x => x.RecommendProduct) * 100m / rows.Count);
        return new ProductRatingSummaryDto(productId, avg, rows.Count, histogram, rows.Count(x => x.IsBuyer), recommend);
    }

    private async Task<IReadOnlyCollection<AdminProductReviewDto>> GetReviewsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = ReviewQuery();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReviewStatus>(status, true, out var parsed))
            query = query.Where(x => x.Status == parsed);

        var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(ToAdminReview).ToArray();
    }

    private async Task<ProductReviewDto?> AddReviewAsync(ProductReviewSubmission submission, CancellationToken cancellationToken = default)
    {
        var row = new ProductReviewDbRecord
        {
            Id = Guid.NewGuid(),
            ProductId = submission.ProductId,
            CustomerId = submission.CustomerId,
            OrderLineId = submission.OrderLineId,
            Rating = submission.Rating,
            Title = submission.Title.Trim(),
            Body = submission.Body.Trim(),
            PositivePointsCsv = NormalizeCsv(submission.PositivePointsCsv),
            NegativePointsCsv = NormalizeCsv(submission.NegativePointsCsv),
            RecommendProduct = submission.RecommendProduct,
            IsBuyer = submission.IsBuyer,
            Status = ReviewStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProductReviews.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReviewQuery().SingleOrDefaultAsync(x => x.Id == row.Id, cancellationToken);
        return saved is null ? null : ToPublicReview(saved);
    }

    private Task<bool> HasCustomerReviewedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
        => db.ProductReviews.AsNoTracking().AnyAsync(x => x.CustomerId == customerId && x.ProductId == productId, cancellationToken);

    private async Task<AdminProductReviewDto?> ModerateReviewAsync(Guid reviewId, ReviewStatus status, string? adminReply, string? moderationNote, CancellationToken cancellationToken = default)
    {
        var row = await db.ProductReviews.SingleOrDefaultAsync(x => x.Id == reviewId, cancellationToken);
        if (row is null) return null;

        row.Status = status;
        row.AdminReply = string.IsNullOrWhiteSpace(adminReply) ? null : adminReply.Trim();
        row.ModerationNote = string.IsNullOrWhiteSpace(moderationNote) ? null : moderationNote.Trim();
        row.RepliedAt = string.IsNullOrWhiteSpace(row.AdminReply) ? null : DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReviewQuery().SingleOrDefaultAsync(x => x.Id == reviewId, cancellationToken);
        return saved is null ? null : ToAdminReview(saved);
    }

    private async Task<IReadOnlyCollection<ProductQuestionDto>> GetPublicQuestionsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var rows = await QuestionQuery()
            .Where(x => x.ProductId == productId && x.Status == QuestionStatus.Answered)
            .OrderByDescending(x => x.AnsweredAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(ToPublicQuestion).ToArray();
    }

    private async Task<IReadOnlyCollection<AdminProductQuestionDto>> GetQuestionsForAdminAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = QuestionQuery();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<QuestionStatus>(status, true, out var parsed))
            query = query.Where(x => x.Status == parsed);

        var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(ToAdminQuestion).ToArray();
    }

    private async Task<ProductQuestionDto?> AddQuestionAsync(ProductQuestionSubmission submission, CancellationToken cancellationToken = default)
    {
        var row = new ProductQuestionDbRecord
        {
            Id = Guid.NewGuid(),
            ProductId = submission.ProductId,
            CustomerId = submission.CustomerId,
            AuthorName = submission.AuthorName.Trim(),
            Mobile = string.IsNullOrWhiteSpace(submission.Mobile) ? null : submission.Mobile.Trim(),
            QuestionText = submission.QuestionText.Trim(),
            Status = QuestionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProductQuestions.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await QuestionQuery().SingleOrDefaultAsync(x => x.Id == row.Id, cancellationToken);
        return saved is null ? null : ToPublicQuestion(saved);
    }

    private async Task<AdminProductQuestionDto?> ModerateQuestionAsync(Guid questionId, QuestionStatus status, string? answerText, string? moderationNote, Guid? answeredByUserId, CancellationToken cancellationToken = default)
    {
        var row = await db.ProductQuestions.SingleOrDefaultAsync(x => x.Id == questionId, cancellationToken);
        if (row is null) return null;

        row.Status = status;
        row.AnswerText = string.IsNullOrWhiteSpace(answerText) ? null : answerText.Trim();
        row.ModerationNote = string.IsNullOrWhiteSpace(moderationNote) ? null : moderationNote.Trim();
        row.AnsweredByUserId = answeredByUserId;
        row.AnsweredAt = status == QuestionStatus.Answered ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);

        var saved = await QuestionQuery().SingleOrDefaultAsync(x => x.Id == questionId, cancellationToken);
        return saved is null ? null : ToAdminQuestion(saved);
    }

    private IQueryable<ProductReviewDbRecord> ReviewQuery() => db.ProductReviews.AsNoTracking()
        .Include(x => x.Product)
        .Include(x => x.Customer);

    private IQueryable<ProductQuestionDbRecord> QuestionQuery() => db.ProductQuestions.AsNoTracking()
        .Include(x => x.Product)
        .Include(x => x.Customer);

    private static ProductReviewDto ToPublicReview(ProductReviewDbRecord row) => new(
        row.Id,
        row.ProductId,
        row.Product?.Name ?? "محصول",
        MaskName(row.Customer?.FullName ?? "مشتری Tatakae"),
        row.Rating,
        row.Title,
        row.Body,
        row.RecommendProduct,
        row.IsBuyer,
        row.Status.ToString(),
        ReviewStatusLabel(row.Status),
        SplitCsv(row.PositivePointsCsv),
        SplitCsv(row.NegativePointsCsv),
        row.AdminReply,
        row.RepliedAt,
        row.CreatedAt);

    private static AdminProductReviewDto ToAdminReview(ProductReviewDbRecord row) => new(
        row.Id,
        row.ProductId,
        row.Product?.Name ?? "محصول",
        row.CustomerId,
        row.Customer?.FullName ?? "مشتری",
        row.Customer?.Mobile ?? "—",
        row.Rating,
        row.Title,
        row.Body,
        row.RecommendProduct,
        row.IsBuyer,
        row.Status.ToString(),
        ReviewStatusLabel(row.Status),
        row.PositivePointsCsv,
        row.NegativePointsCsv,
        row.AdminReply,
        row.ModerationNote,
        row.RepliedAt,
        row.CreatedAt);

    private static ProductQuestionDto ToPublicQuestion(ProductQuestionDbRecord row) => new(
        row.Id,
        row.ProductId,
        string.IsNullOrWhiteSpace(row.AuthorName) ? (row.Customer?.FullName ?? "مشتری Tatakae") : row.AuthorName,
        row.QuestionText,
        row.AnswerText,
        row.CreatedAt,
        row.AnsweredAt,
        row.Status == QuestionStatus.Answered && !string.IsNullOrWhiteSpace(row.AnswerText));

    private static AdminProductQuestionDto ToAdminQuestion(ProductQuestionDbRecord row) => new(
        row.Id,
        row.ProductId,
        row.Product?.Name ?? "محصول",
        row.CustomerId,
        string.IsNullOrWhiteSpace(row.AuthorName) ? (row.Customer?.FullName ?? "مشتری") : row.AuthorName,
        row.Mobile ?? row.Customer?.Mobile,
        row.QuestionText,
        row.AnswerText,
        row.Status.ToString(),
        QuestionStatusLabel(row.Status),
        row.ModerationNote,
        row.CreatedAt,
        row.AnsweredAt);

    private static string ReviewStatusLabel(ReviewStatus status) => status switch
    {
        ReviewStatus.Pending => "در انتظار بررسی",
        ReviewStatus.Approved => "منتشر شده",
        ReviewStatus.Rejected => "رد شده",
        ReviewStatus.Hidden => "مخفی",
        _ => status.ToString()
    };

    private static string QuestionStatusLabel(QuestionStatus status) => status switch
    {
        QuestionStatus.Pending => "در انتظار پاسخ",
        QuestionStatus.Answered => "پاسخ داده شده",
        QuestionStatus.Rejected => "رد شده",
        QuestionStatus.Hidden => "مخفی",
        _ => status.ToString()
    };

    private static IReadOnlyCollection<string> SplitCsv(string? value) => string.IsNullOrWhiteSpace(value)
        ? Array.Empty<string>()
        : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static string? NormalizeCsv(string? value)
    {
        var items = SplitCsv(value);
        return items.Count == 0 ? null : string.Join(", ", items);
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "مشتری Tatakae";
        var clean = name.Trim();
        return clean.Length <= 2 ? clean : $"{clean[..1]}***";
    }
}
