namespace Tatakae.Domain.Enums;

public enum StockTransactionType
{
    InitialStock = 1,
    PurchaseReceipt = 2,
    OrderReservation = 3,
    ReservationRelease = 4,
    OrderShipment = 5,
    ReturnReceipt = 6,
    ManualAdjustment = 7,
    Damaged = 8,
    ReservationConsumed = 9
}

public enum InventoryReservationStatus
{
    Reserved = 1,
    Released = 2,
    Consumed = 3,
    Expired = 4
}
