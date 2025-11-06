namespace StockManager.Core.Enums;

public enum SalesOrderStatus
{
    Draft = 0,          // Being created
    Submitted = 1,      // Submitted for processing
    Confirmed = 2,      // Confirmed, stock allocated
    AwaitingPickup = 3, // Ready to be picked
    Picking = 4,        // Currently being picked
    Picked = 5,         // All items picked
    Packing = 6,        // Being packed
    Packed = 7,         // Packed, ready to ship
    Shipped = 8,        // Shipped to customer
    Delivered = 9,      // Delivered to customer
    Cancelled = 10,     // Order cancelled
    OnHold = 11         // On hold (payment issue, stock issue)
}
