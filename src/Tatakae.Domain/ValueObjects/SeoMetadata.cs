using Tatakae.Domain.Common;

namespace Tatakae.Domain.Entities;

/// <summary>Indexing metadata owned by the aggregate being indexed.</summary>
public sealed record SeoMetadata
{
    public SeoMetadata(
        string metaTitle,
        string metaDescription,
        string? canonicalPath = null,
        string? openGraphImageUrl = null,
        bool allowIndex = true,
        bool allowFollow = true)
    {
        MetaTitle = DomainGuard.Required(metaTitle, nameof(metaTitle), "عنوان SEO الزامی است.");
        MetaDescription = DomainGuard.Required(metaDescription, nameof(metaDescription), "توضیحات SEO الزامی است.");
        CanonicalPath = DomainGuard.Optional(canonicalPath);
        OpenGraphImageUrl = DomainGuard.Optional(openGraphImageUrl);
        AllowIndex = allowIndex;
        AllowFollow = allowFollow;
    }

    public string MetaTitle { get; }
    public string MetaDescription { get; }
    public string? CanonicalPath { get; }
    public string? OpenGraphImageUrl { get; }
    public bool AllowIndex { get; }
    public bool AllowFollow { get; }
}
