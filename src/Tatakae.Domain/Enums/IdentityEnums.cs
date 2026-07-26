namespace Tatakae.Domain.Enums;

public enum IranianAuthProvider
{
    Password = 1,
    SmsOtp = 2,
    Google = 3
}

public enum UserRoleName
{
    Customer = 1,
    Admin = 2,
    Operator = 3,
    Seller = 4,
    Production = 5,
    Support = 6
}

public enum SellerType
{
    OwnStore = 1,
    LegalCompany = 2,
    RealPerson = 3,
    Workshop = 4
}

public enum SellerStatus
{
    Draft = 1,
    PendingReview = 2,
    Active = 3,
    Suspended = 4,
    Rejected = 5
}
