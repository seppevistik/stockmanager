using StockManager.Core.Enums;

namespace StockManager.Core.DTOs;

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string? SupplierReference { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class CreatePurchaseOrderDto
{
    public int CompanyId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public string? Notes { get; set; }
    public string? SupplierReference { get; set; }
    public List<CreatePurchaseOrderLineDto> Lines { get; set; } = new();
}

public class UpdatePurchaseOrderDto
{
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public string? Notes { get; set; }
    public string? SupplierReference { get; set; }
}

public class PurchaseOrderLineDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityOutstanding { get; set; }
    public LineItemStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class CreatePurchaseOrderLineDto
{
    public int ProductId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderFilterDto
{
    public PurchaseOrderStatus? Status { get; set; }
    public int? CompanyId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
