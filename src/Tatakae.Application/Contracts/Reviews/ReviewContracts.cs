using System.ComponentModel.DataAnnotations;

namespace Tatakae.Application.Contracts.Reviews;

public sealed class CreateProductReviewRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "عنوان نظر الزامی است.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "عنوان نظر باید بین ۳ تا ۱۲۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن نظر الزامی است.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "متن نظر باید بین ۱۰ تا ۲۰۰۰ کاراکتر باشد.")]
    public string Body { get; set; } = string.Empty;

    public bool RecommendProduct { get; set; } = true;

    [StringLength(500)]
    public string? PositivePointsCsv { get; set; }

    [StringLength(500)]
    public string? NegativePointsCsv { get; set; }
}

public sealed class AdminReviewModerationRequest : IValidatableObject
{
    [Required, RegularExpression("^(Approved|Rejected|Hidden)$")]
    public string Status { get; set; } = "Approved";

    [StringLength(1200)]
    public string? AdminReply { get; set; }

    [StringLength(700, ErrorMessage = "یادداشت بررسی حداکثر ۷۰۰ کاراکتر است.")]
    public string? ModerationNote { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == "Rejected" && string.IsNullOrWhiteSpace(ModerationNote))
        {
            yield return new ValidationResult("برای رد نظر، دلیل بررسی را وارد کنید.", [nameof(ModerationNote)]);
        }
    }
}

public sealed class AdminQuestionModerationRequest : IValidatableObject
{
    [Required, RegularExpression("^(Answered|Rejected|Hidden|Pending)$")]
    public string Status { get; set; } = "Answered";

    [StringLength(2000)]
    public string? AnswerText { get; set; }

    [StringLength(700, ErrorMessage = "یادداشت بررسی حداکثر ۷۰۰ کاراکتر است.")]
    public string? ModerationNote { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == "Answered" && string.IsNullOrWhiteSpace(AnswerText))
        {
            yield return new ValidationResult("برای وضعیت پاسخ‌داده‌شده، متن پاسخ را وارد کنید.", [nameof(AnswerText)]);
        }

        if (Status == "Rejected" && string.IsNullOrWhiteSpace(ModerationNote))
        {
            yield return new ValidationResult("برای رد پرسش، دلیل بررسی را وارد کنید.", [nameof(ModerationNote)]);
        }
    }
}

public sealed record ProductReviewDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string CustomerName,
    int Rating,
    string Title,
    string Body,
    bool RecommendProduct,
    bool IsBuyer,
    string Status,
    string StatusLabel,
    IReadOnlyCollection<string> PositivePoints,
    IReadOnlyCollection<string> NegativePoints,
    string? AdminReply,
    DateTimeOffset? RepliedAt,
    DateTimeOffset CreatedAt);

public sealed record ProductRatingSummaryDto(
    Guid ProductId,
    decimal AverageRating,
    int ReviewCount,
    IReadOnlyDictionary<int, int> RatingHistogram,
    int BuyerReviewCount,
    int RecommendationPercent);

public sealed class AdminProductReviewDto
{
    public AdminProductReviewDto() { }

    public AdminProductReviewDto(Guid id, Guid productId, string productName, Guid customerId, string customerName, string customerMobile, int rating, string title, string body, bool recommendProduct, bool isBuyer, string status, string statusLabel, string? positivePointsCsv, string? negativePointsCsv, string? adminReply, string? moderationNote, DateTimeOffset? repliedAt, DateTimeOffset createdAt)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerMobile = customerMobile;
        Rating = rating;
        Title = title;
        Body = body;
        RecommendProduct = recommendProduct;
        IsBuyer = isBuyer;
        Status = status;
        StatusLabel = statusLabel;
        PositivePointsCsv = positivePointsCsv;
        NegativePointsCsv = negativePointsCsv;
        AdminReply = adminReply;
        ModerationNote = moderationNote;
        RepliedAt = repliedAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool RecommendProduct { get; set; }
    public bool IsBuyer { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string? PositivePointsCsv { get; set; }
    public string? NegativePointsCsv { get; set; }
    public string? AdminReply { get; set; }
    public string? ModerationNote { get; set; }
    public DateTimeOffset? RepliedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminProductQuestionDto
{
    public AdminProductQuestionDto() { }

    public AdminProductQuestionDto(Guid id, Guid productId, string productName, Guid? customerId, string authorName, string? mobile, string questionText, string? answerText, string status, string statusLabel, string? moderationNote, DateTimeOffset createdAt, DateTimeOffset? answeredAt)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        CustomerId = customerId;
        AuthorName = authorName;
        Mobile = mobile;
        QuestionText = questionText;
        AnswerText = answerText;
        Status = status;
        StatusLabel = statusLabel;
        ModerationNote = moderationNote;
        CreatedAt = createdAt;
        AnsweredAt = answeredAt;
    }

    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string? ModerationNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
}
