using Tatakae.Domain.Common;

namespace Tatakae.Domain.Entities;

public sealed record ProductImage
{
    public ProductImage(Guid id, string url, string altText, bool isPrimary, int sortOrder)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه تصویر محصول معتبر نیست.");
        Url = url?.Trim() ?? string.Empty;
        AltText = DomainGuard.Required(altText, nameof(altText), "متن جایگزین تصویر الزامی است.");
        SortOrder = DomainGuard.NonNegative(sortOrder, nameof(sortOrder), "ترتیب تصویر نمی‌تواند منفی باشد.");
        IsPrimary = isPrimary;
    }

    public Guid Id { get; }
    public string Url { get; }
    public string AltText { get; }
    public bool IsPrimary { get; }
    public int SortOrder { get; }
}
