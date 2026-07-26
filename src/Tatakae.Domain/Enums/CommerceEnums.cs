namespace Tatakae.Domain.Enums;

public enum InvoiceType
{
    Informal = 1,
    Official = 2
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Cancelled = 3,
    Refunded = 4
}

public enum MediaUsageType
{
    ProductImage = 1,
    ProductGallery = 2,
    EmbroideryArtwork = 3,
    ReviewImage = 4,
    Banner = 5,
    Avatar = 6,
    InvoiceAttachment = 7
}

public enum SeoRedirectType
{
    Permanent301 = 1,
    Temporary302 = 2
}

public enum EmbroideryArtworkStatus
{
    PendingReview = 1,
    Approved = 2,
    Rejected = 3,
    NeedsRevision = 4,
    Archived = 5
}

public enum ReviewStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Hidden = 4
}

public enum QuestionStatus
{
    Pending = 1,
    Answered = 2,
    Rejected = 3,
    Hidden = 4
}
