using System;
using System.Collections.Generic;
using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class SalesOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    // Customer Information
    public int CustomerId { get; set; }
    public Company Customer { get; set; } = null!;

    // Shipping Information
    public string ShipToName { get; set; } = string.Empty;
    public string ShipToAddress { get; set; } = string.Empty;
    public string ShipToCity { get; set; } = string.Empty;
    public string ShipToPostalCode { get; set; } = string.Empty;
    public string ShipToCountry { get; set; } = string.Empty;
    public string? ShipToPhone { get; set; }

    // Dates
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Financial
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Status & Workflow
    public SalesOrderStatus Status { get; set; }
    public Priority Priority { get; set; }

    // Shipping
    public string? ShippingCarrier { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShippingMethod { get; set; }

    // References
    public string? CustomerReference { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }

    // Audit
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public string CreatedBy { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    // Navigation
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}
