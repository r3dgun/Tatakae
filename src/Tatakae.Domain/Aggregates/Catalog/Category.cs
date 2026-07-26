using Tatakae.Domain.Common;

namespace Tatakae.Domain.Entities;

public sealed class Category
{
    public Category(
        Guid id,
        string name,
        string slug,
        string description,
        string? coverImageUrl,
        SeoMetadata seo,
        Guid? parentId = null,
        int sortOrder = 0,
        bool isActive = true)
    {
        Id = DomainGuard.NotEmpty(id, nameof(id), "شناسه دسته‌بندی معتبر نیست.");
        Name = DomainGuard.Required(name, nameof(name), "نام دسته‌بندی الزامی است.");
        Slug = DomainGuard.Required(slug, nameof(slug), "اسلاگ دسته‌بندی الزامی است.").ToLowerInvariant();
        Description = description?.Trim() ?? string.Empty;
        CoverImageUrl = DomainGuard.Optional(coverImageUrl);
        Seo = seo ?? throw new ArgumentNullException(nameof(seo));
        if (parentId == Guid.Empty)
            throw new ArgumentException("شناسه والد دسته‌بندی معتبر نیست.", nameof(parentId));
        if (parentId == id)
            throw new ArgumentException("دسته‌بندی نمی‌تواند والد خودش باشد.", nameof(parentId));
        ParentId = parentId;
        SortOrder = DomainGuard.NonNegative(sortOrder, nameof(sortOrder), "ترتیب دسته‌بندی نمی‌تواند منفی باشد.");
        IsActive = isActive;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public SeoMetadata Seo { get; private set; }
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
