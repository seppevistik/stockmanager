using StockManager.Core.Enums;

namespace StockManager.Core.DTOs;

public class ReceiptDto
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public int PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string ReceivedByName { get; set; } = string.Empty;
    public ReceiptStatus Status { get; set; }
    public string? SupplierDeliveryNote { get; set; }
    public string? Notes { get; set; }
    public bool HasVariances { get; set; }
    public string? VarianceNotes { get; set; }
    public string? ValidatedBy { get; set; }
    public string? ValidatedByName { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ReceiptLineDto> Lines { get; set; } = new();
}

public class CreateReceiptDto
{
    public int PurchaseOrderId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public string? SupplierDeliveryNote { get; set; }
    public string? Notes { get; set; }
    public List<CreateReceiptLineDto> Lines { get; set; } = new();
}

public class UpdateReceiptDto
{
    public DateTime ReceiptDate { get; set; }
    public string? SupplierDeliveryNote { get; set; }
    public string? Notes { get; set; }
    public List<CreateReceiptLineDto> Lines { get; set; } = new();
}

public class ReceiptLineDto
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
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
}

public class CreateReceiptLineDto
{
    public int PurchaseOrderLineId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal? UnitPriceReceived { get; set; }
    public ItemCondition Condition { get; set; } = ItemCondition.Good;
    public string? DamageNotes { get; set; }
    public string? Location { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class ReceiptValidationDto
{
    public int ReceiptId { get; set; }
    public bool HasVariances { get; set; }
    public List<VarianceDto> Variances { get; set; } = new();
}

public class VarianceDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityVariance { get; set; }
    public decimal? PriceVariance { get; set; }
    public ItemCondition Condition { get; set; }
}

public class ApproveReceiptDto
{
    public string? VarianceNotes { get; set; }
}

public class RejectReceiptDto
{
    public string Reason { get; set; } = string.Empty;
}
