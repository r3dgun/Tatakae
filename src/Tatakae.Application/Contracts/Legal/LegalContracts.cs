using System.ComponentModel.DataAnnotations;
using Tatakae.Application.Seo;

namespace Tatakae.Application.Contracts.Legal;

public sealed record StorePolicyPageDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Body,
    string? SeoTitle,
    string? SeoDescription,
    bool IsPublished,
    int SortOrder,
    DateTimeOffset UpdatedAt);

public sealed class UpsertStorePolicyPageRequest
{
    [Required, RegularExpression(SeoSlug.ValidationPattern, ErrorMessage = "Slug می‌تواند فارسی یا انگلیسی باشد؛ بین واژه‌ها فاصله یا خط تیره بگذارید.")]
    [StringLength(80)]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(180, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(700, MinimumLength = 20)]
    public string Summary { get; set; } = string.Empty;

    [Required, StringLength(12000, MinimumLength = 50)]
    public string Body { get; set; } = string.Empty;

    [StringLength(65)]
    public string? SeoTitle { get; set; }

    [StringLength(160)]
    public string? SeoDescription { get; set; }

    public bool IsPublished { get; set; } = true;

    [Range(0, 9999)]
    public int SortOrder { get; set; }
}

public sealed record ContactMessageDto(
    Guid Id,
    string FullName,
    string Mobile,
    string? Email,
    string Subject,
    string Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AnsweredAt,
    string? AdminNote);

public sealed class SubmitContactMessageRequest
{
    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است.")]
    [RegularExpression(Tatakae.Application.Validation.IranianValidationPatterns.Mobile, ErrorMessage = "شماره موبایل ایران معتبر نیست.")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست."), StringLength(260, ErrorMessage = "ایمیل حداکثر ۲۶۰ کاراکتر است.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "موضوع پیام الزامی است."), StringLength(160, MinimumLength = 3, ErrorMessage = "موضوع باید بین ۳ تا ۱۶۰ کاراکتر باشد.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن پیام الزامی است."), StringLength(2000, MinimumLength = 10, ErrorMessage = "متن پیام باید بین ۱۰ تا ۲۰۰۰ کاراکتر باشد.")]
    public string Message { get; set; } = string.Empty;
}

public sealed class UpdateContactMessageStatusRequest
{
    [Required, RegularExpression("^(new|seen|answered|closed)$")]
    public string Status { get; set; } = "seen";

    [StringLength(1000)]
    public string? AdminNote { get; set; }
}
