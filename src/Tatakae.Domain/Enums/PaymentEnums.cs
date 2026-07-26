namespace Tatakae.Domain.Enums;

public enum PaymentMethod
{
    OnlineGateway = 1,
    CardToCard = 2,
    Wallet = 3,
    CashOnDelivery = 4,
    BankTransfer = 5
}

public enum IranianPaymentGateway
{
    None = 0,
    Zarinpal = 1,
    IdPay = 2,
    PayIr = 3,
    NextPay = 4,
    BehPardakht = 5,
    SamanKish = 6,
    Parsian = 7,
    Pasargad = 8
}

public enum PaymentTransactionStatus
{
    Pending = 1,
    RedirectedToGateway = 2,
    Succeeded = 3,
    Failed = 4,
    CancelledByUser = 5,
    Verified = 6,
    Reversed = 7,
    Refunded = 8
}

public enum RefundStatus
{
    Requested = 1,
    Approved = 2,
    Rejected = 3,
    PaidToWallet = 4,
    PaidToBankCard = 5,
    Cancelled = 6
}

public enum WalletTransactionType
{
    Charge = 1,
    Payment = 2,
    Refund = 3,
    Withdrawal = 4,
    ManualAdjustment = 5
}
