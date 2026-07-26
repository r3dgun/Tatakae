namespace Tatakae.Domain.Enums;

public enum ShippingCarrier
{
    OwnCourier = 1,
    Post = 2,
    Tipax = 3,
    Chapar = 4,
    Peyk = 5,
    BarBari = 6,
    CustomerPickup = 7
}

public enum ShipmentStatus
{
    Draft = 1,
    ReadyToShip = 2,
    PickedUp = 3,
    InTransit = 4,
    OutForDelivery = 5,
    Delivered = 6,
    Returned = 7,
    Lost = 8,
    Cancelled = 9
}

public enum ReturnRequestStatus
{
    Requested = 1,
    WaitingForCustomerShipment = 2,
    Received = 3,
    UnderReview = 4,
    Approved = 5,
    Rejected = 6,
    Refunded = 7,
    Closed = 8
}

public enum ReturnReason
{
    Damaged = 1,
    WrongItem = 2,
    SizeProblem = 3,
    QualityIssue = 4,
    ArtworkMismatch = 5,
    CustomerChangedMind = 6,
    Other = 99
}

public enum WarrantyType
{
    None = 1,
    SellerWarranty = 2,
    BrandWarranty = 3,
    ServiceCenterWarranty = 4,
    AuthenticityGuarantee = 5
}
