namespace Tatakae.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    ArtworkReview = 3,
    InEmbroidery = 4,
    QualityControl = 5,
    Packed = 6,
    Shipped = 7,
    Delivered = 8,
    Cancelled = 9,
    Refunded = 10
}
