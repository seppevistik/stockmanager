using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class ReceiptLine
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityVariance { get; set; }
    public decimal UnitPriceOrdered { get; set; }
    public decimal? UnitPriceReceived { get; set; }
    public decimal PriceVariance { get; set; }
    public ItemCondition Condition { get; set; }
    public string? DamageNotes { get; set; }
    public string? Location { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Receipt Receipt { get; set; } = null!;
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
