using System;
using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }

    // Product snapshot (at time of order)
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;

    // Quantities
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityPicked { get; set; }
    public decimal QuantityShipped { get; set; }
    public decimal QuantityOutstanding { get; set; }

    // Pricing
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }

    // Fulfillment
    public SalesOrderLineStatus Status { get; set; }
    public string? Location { get; set; }
    public string? PickedBy { get; set; }
    public DateTime? PickedAt { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public SalesOrder SalesOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
