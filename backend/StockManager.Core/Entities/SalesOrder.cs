using System;
using System.Collections.Generic;
using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class SalesOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    // Customer Information (optional - allows for daily summaries without customers)
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Shipping Information
    public string ShipToName { get; set; } = string.Empty;
    public string ShipToAddress { get; set; } = string.Empty;
    public string ShipToCity { get; set; } = string.Empty;
    public string ShipToState { get; set; } = string.Empty;
    public string ShipToPostalCode { get; set; } = string.Empty;
    public string ShipToCountry { get; set; } = string.Empty;
    public string? ShipToPhone { get; set; }

    // Dates
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? PromisedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Financial
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Status & Workflow
    public SalesOrderStatus Status { get; set; }
    public Priority Priority { get; set; }

    // Shipping & Fulfillment
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShippingMethod { get; set; }
    public string? PickedBy { get; set; }
    public string? PackedBy { get; set; }
    public string? ShippedBy { get; set; }

    // References
    public string? CustomerReference { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // Audit
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public string CreatedBy { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    // Navigation
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}
