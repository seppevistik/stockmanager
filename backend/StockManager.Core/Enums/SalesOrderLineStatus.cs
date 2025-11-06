namespace StockManager.Core.Enums;

public enum SalesOrderLineStatus
{
    Pending = 0,
    Allocated = 1,      // Stock allocated
    Picked = 2,         // Picked from warehouse
    Packed = 3,         // Packed for shipment
    Shipped = 4,        // Shipped to customer
    Cancelled = 5
}
