using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class Receipt
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public int PurchaseOrderId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int ReceivedBy { get; set; }
    public ReceiptStatus Status { get; set; }
    public string? SupplierDeliveryNote { get; set; }
    public string? Notes { get; set; }
    public bool HasVariances { get; set; }
    public string? VarianceNotes { get; set; }
    public int? ValidatedBy { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public ApplicationUser ReceivedByUser { get; set; } = null!;
    public ApplicationUser? ValidatedByUser { get; set; }
    public ICollection<ReceiptLine> Lines { get; set; } = new List<ReceiptLine>();
}
