using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StockManager.Core.Enums;

namespace StockManager.Core.DTOs;

// Main DTOs
public class SalesOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    // Shipping Information
    public string ShipToName { get; set; } = string.Empty;
    public string ShipToAddress { get; set; } = string.Empty;
    public string ShipToCity { get; set; } = string.Empty;
    public string ShipToState { get; set; } = string.Empty;
    public string ShipToPostalCode { get; set; } = string.Empty;
    public string ShipToCountry { get; set; } = string.Empty;
    public string? ShipToPhone { get; set; }

    // Financial
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Status & Workflow
    public SalesOrderStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public string PriorityDisplay { get; set; } = string.Empty;

    // Dates
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? PromisedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Fulfillment
    public string? ShippingMethod { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? PickedBy { get; set; }
    public string? PackedBy { get; set; }
    public string? ShippedBy { get; set; }

    // Additional Info
    public string? CustomerReference { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // Audit
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Related Data
    public List<SalesOrderLineDto> Lines { get; set; } = new();

    // Summary Statistics
    public int TotalLineItems { get; set; }
    public decimal TotalQuantityOrdered { get; set; }
    public decimal TotalQuantityPicked { get; set; }
    public decimal TotalQuantityShipped { get; set; }
    public decimal TotalQuantityOutstanding { get; set; }
}

public class SalesOrderLineDto
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }

    // Product snapshot
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }

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
    public string StatusDisplay { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PickedBy { get; set; }
    public DateTime? PickedAt { get; set; }

    public string? Notes { get; set; }
}

// Create/Update Request DTOs
public class CreateSalesOrderRequest
{
    [Required]
    public int CustomerId { get; set; }

    // Shipping Information
    [Required]
    [MaxLength(200)]
    public string ShipToName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShipToAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ShipToCity { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ShipToState { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ShipToPostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ShipToCountry { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ShipToPhone { get; set; }

    // Financial
    [Range(0, double.MaxValue)]
    public decimal TaxRate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ShippingCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    // Priority & Dates
    public Priority Priority { get; set; } = Priority.Normal;

    public DateTime? RequiredDate { get; set; }
    public DateTime? PromisedDate { get; set; }

    // Additional Info
    [MaxLength(100)]
    public string? CustomerReference { get; set; }

    [MaxLength(100)]
    public string? ShippingMethod { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? InternalNotes { get; set; }

    // Order Lines
    [Required]
    [MinLength(1, ErrorMessage = "At least one order line is required")]
    public List<CreateSalesOrderLineRequest> Lines { get; set; } = new();
}

public class CreateSalesOrderLineRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal QuantityOrdered { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100")]
    public decimal DiscountPercent { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateSalesOrderRequest
{
    [Required]
    public int CustomerId { get; set; }

    // Shipping Information
    [Required]
    [MaxLength(200)]
    public string ShipToName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShipToAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ShipToCity { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ShipToState { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ShipToPostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ShipToCountry { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ShipToPhone { get; set; }

    // Financial
    [Range(0, double.MaxValue)]
    public decimal TaxRate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ShippingCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    // Priority & Dates
    public Priority Priority { get; set; }

    public DateTime? RequiredDate { get; set; }
    public DateTime? PromisedDate { get; set; }

    // Additional Info
    [MaxLength(100)]
    public string? CustomerReference { get; set; }

    [MaxLength(100)]
    public string? ShippingMethod { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? InternalNotes { get; set; }

    // Order Lines
    [Required]
    [MinLength(1, ErrorMessage = "At least one order line is required")]
    public List<UpdateSalesOrderLineRequest> Lines { get; set; } = new();
}

public class UpdateSalesOrderLineRequest
{
    public int? Id { get; set; } // Null for new lines, set for existing lines

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal QuantityOrdered { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100")]
    public decimal DiscountPercent { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

// Workflow Action Request DTOs
public class SubmitOrderRequest
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class ConfirmOrderRequest
{
    public DateTime? PromisedDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class CancelOrderRequest
{
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class HoldOrderRequest
{
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class ReleaseOrderRequest
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class StartPickingRequest
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class CompletePickingRequest
{
    [Required]
    public List<PickedLineDto> PickedLines { get; set; } = new();

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class PickedLineDto
{
    [Required]
    public int LineId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal QuantityPicked { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class StartPackingRequest
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class CompletePackingRequest
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class ShipOrderRequest
{
    [Required]
    public DateTime ShippedDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string Carrier { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class DeliverOrderRequest
{
    [Required]
    public DateTime DeliveredDate { get; set; }

    [MaxLength(200)]
    public string? ReceivedBy { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

// Query/Filter DTOs
public class SalesOrderListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public int? CustomerId { get; set; }
    public SalesOrderStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public DateTime? OrderDateFrom { get; set; }
    public DateTime? OrderDateTo { get; set; }
    public DateTime? RequiredDateFrom { get; set; }
    public DateTime? RequiredDateTo { get; set; }
    public string? SortBy { get; set; } = "OrderDate";
    public string? SortDirection { get; set; } = "desc";
}

// Statistics DTOs
public class SalesOrderStatistics
{
    public int TotalOrders { get; set; }
    public int DraftOrders { get; set; }
    public int SubmittedOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int InProgress { get; set; }
    public int ShippedToday { get; set; }
    public int OverdueOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PendingRevenue { get; set; }
}

// Summary DTOs
public class SalesOrderSummaryDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public SalesOrderStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public string PriorityDisplay { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalLineItems { get; set; }
    public string ShipToCity { get; set; } = string.Empty;
    public string ShipToState { get; set; } = string.Empty;
}
