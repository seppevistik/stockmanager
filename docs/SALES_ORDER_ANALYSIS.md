# Sales Order & Fulfillment Module - Analysis & Design

## Executive Summary

This document outlines the design and implementation plan for the Sales Order & Fulfillment module, which completes the inventory cycle by managing the outbound flow of products from the warehouse to customers.

**Status**: Planning Phase
**Estimated Effort**: 2-3 weeks
**Dependencies**: Product Management, Company Management, Stock Movement

---

## 1. Business Requirements

### Problem Statement
Currently, the system tracks:
- ✅ **Inbound**: Purchase Orders → Receipts → Inventory
- ❌ **Outbound**: No way to track sales, picking, packing, or shipments

We need to manage:
1. Customer orders (sales orders)
2. Picking products from inventory
3. Packing items for shipment
4. Shipping and delivery tracking
5. Inventory reduction when items ship

### Business Flows

#### **Customer Order Flow**
```
Customer Places Order
    ↓
Create Sales Order (Draft)
    ↓
Submit for Processing
    ↓
Allocate Stock / Check Availability
    ↓
Generate Pick List
    ↓
Warehouse Staff Picks Items
    ↓
Pack Items (with packing slip)
    ↓
Generate Shipping Label
    ↓
Ship Package
    ↓
Update Inventory (reduce stock)
    ↓
Mark as Delivered
```

#### **Stock Allocation Flow**
- When SO is confirmed, check if enough stock exists
- Reserve/allocate stock for the order (virtual hold)
- Allocated stock = not available for other orders
- Physical stock reduction happens when shipped

---

## 2. Data Model Design

### **SalesOrder Entity**

```csharp
public class SalesOrder
{
    // Identity
    public int Id { get; set; }
    public string OrderNumber { get; set; }  // SO-20250106-001

    // Customer Information
    public int CustomerId { get; set; }       // FK to Company (IsCustomer=true)
    public Company Customer { get; set; }

    // Shipping Information
    public string ShipToName { get; set; }
    public string ShipToAddress { get; set; }
    public string ShipToCity { get; set; }
    public string ShipToPostalCode { get; set; }
    public string ShipToCountry { get; set; }
    public string ShipToPhone { get; set; }

    // Dates
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
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
    public string? ShippingCarrier { get; set; }     // FedEx, UPS, USPS
    public string? TrackingNumber { get; set; }
    public string? ShippingMethod { get; set; }      // Ground, Express, Overnight

    // References
    public string? CustomerReference { get; set; }   // Customer's PO number
    public string? Notes { get; set; }

    // Audit
    public int BusinessId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Business Business { get; set; }
    public ApplicationUser CreatedByUser { get; set; }
    public ICollection<SalesOrderLine> Lines { get; set; }
    public ICollection<Shipment> Shipments { get; set; }
}

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

public enum Priority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
```

### **SalesOrderLine Entity**

```csharp
public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }

    // Product snapshot (at time of order)
    public string ProductName { get; set; }
    public string ProductSku { get; set; }

    // Quantities
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityPicked { get; set; }
    public decimal QuantityShipped { get; set; }
    public decimal QuantityOutstanding { get; set; }  // Ordered - Shipped

    // Pricing
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }

    // Fulfillment
    public SalesOrderLineStatus Status { get; set; }
    public string? Location { get; set; }      // Where to pick from (e.g., "A-05-B")
    public string? PickedBy { get; set; }      // User who picked
    public DateTime? PickedAt { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public SalesOrder SalesOrder { get; set; }
    public Product Product { get; set; }
}

public enum SalesOrderLineStatus
{
    Pending = 0,
    Allocated = 1,      // Stock allocated
    Picked = 2,         // Picked from warehouse
    Packed = 3,         // Packed for shipment
    Shipped = 4,        // Shipped to customer
    Cancelled = 5
}
```

### **Shipment Entity**

```csharp
public class Shipment
{
    public int Id { get; set; }
    public string ShipmentNumber { get; set; }  // SHIP-20250106-001
    public int SalesOrderId { get; set; }

    // Shipping Details
    public DateTime ShippedDate { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }

    public string ShippingCarrier { get; set; }
    public string TrackingNumber { get; set; }
    public string ShippingMethod { get; set; }
    public decimal ShippingCost { get; set; }

    public ShipmentStatus Status { get; set; }

    // Package Details
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }      // kg, lb
    public string? PackageType { get; set; }     // Box, Envelope, Pallet
    public int NumberOfPackages { get; set; }

    public string? Notes { get; set; }

    // Audit
    public int BusinessId { get; set; }
    public string ShippedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public SalesOrder SalesOrder { get; set; }
    public Business Business { get; set; }
    public ApplicationUser ShippedByUser { get; set; }
    public ICollection<ShipmentLine> Lines { get; set; }
}

public enum ShipmentStatus
{
    Pending = 0,
    InTransit = 1,
    OutForDelivery = 2,
    Delivered = 3,
    Failed = 4,         // Delivery failed
    Returned = 5        // Returned to sender
}
```

### **ShipmentLine Entity**

```csharp
public class ShipmentLine
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public int SalesOrderLineId { get; set; }
    public int ProductId { get; set; }

    public string ProductName { get; set; }
    public string ProductSku { get; set; }

    public decimal QuantityShipped { get; set; }

    // Serial numbers or batch numbers if tracked
    public string? BatchNumber { get; set; }
    public string? SerialNumbers { get; set; }  // JSON array if multiple

    // Navigation
    public Shipment Shipment { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; }
    public Product Product { get; set; }
}
```

### **StockAllocation Entity** (Optional, for advanced stock management)

```csharp
public class StockAllocation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int SalesOrderId { get; set; }
    public int SalesOrderLineId { get; set; }

    public decimal QuantityAllocated { get; set; }
    public DateTime AllocatedAt { get; set; }
    public string AllocatedBy { get; set; }

    public bool IsReleased { get; set; }         // Released back to available stock
    public DateTime? ReleasedAt { get; set; }

    // Navigation
    public Product Product { get; set; }
    public SalesOrder SalesOrder { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; }
}
```

---

## 3. API Endpoints

### **SalesOrdersController**

```
GET    /api/salesorders                  - List sales orders (paginated, filtered)
GET    /api/salesorders/{id}             - Get sales order details
POST   /api/salesorders                  - Create new sales order (draft)
PUT    /api/salesorders/{id}             - Update sales order (draft only)
DELETE /api/salesorders/{id}             - Delete sales order (draft only)

POST   /api/salesorders/{id}/submit      - Submit for processing
POST   /api/salesorders/{id}/confirm     - Confirm order (allocate stock)
POST   /api/salesorders/{id}/cancel      - Cancel order
POST   /api/salesorders/{id}/hold        - Put on hold
POST   /api/salesorders/{id}/resume      - Resume from hold

GET    /api/salesorders/{id}/picklist    - Generate pick list (PDF)
POST   /api/salesorders/{id}/pick        - Mark items as picked
POST   /api/salesorders/{id}/pack        - Mark as packed

GET    /api/salesorders/statistics       - Dashboard statistics
```

### **ShipmentsController**

```
GET    /api/shipments                    - List shipments
GET    /api/shipments/{id}               - Get shipment details
POST   /api/shipments                    - Create shipment from sales order
PUT    /api/shipments/{id}               - Update shipment details

POST   /api/shipments/{id}/ship          - Mark as shipped (reduce inventory)
POST   /api/shipments/{id}/deliver       - Mark as delivered
POST   /api/shipments/{id}/track         - Get tracking updates

GET    /api/shipments/{id}/label         - Generate shipping label (PDF)
GET    /api/shipments/{id}/packingslip   - Generate packing slip (PDF)
```

---

## 4. State Machine & Workflow

### **Sales Order Status Transitions**

```
Draft
  ├→ Submit → Submitted
  └→ Delete (allowed)

Submitted
  ├→ Confirm → Confirmed (allocate stock)
  ├→ Cancel → Cancelled
  └→ Hold → OnHold

Confirmed
  ├→ Start Picking → Picking
  ├→ Cancel → Cancelled
  └→ Hold → OnHold

Picking
  ├→ Complete Picking → Picked
  └→ Back → Confirmed

Picked
  ├→ Start Packing → Packing
  └→ Back → Picking

Packing
  └→ Complete Packing → Packed

Packed
  └→ Ship → Shipped (inventory reduced)

Shipped
  └→ Deliver → Delivered

OnHold
  ├→ Resume → (previous status)
  └→ Cancel → Cancelled
```

### **Business Rules**

1. **Draft**: Can edit, delete, add/remove lines
2. **Submitted**: Read-only, awaiting confirmation
3. **Confirmed**: Stock allocated, cannot edit quantities
4. **Picking**: Warehouse staff marks items picked
5. **Packed**: Ready for shipment
6. **Shipped**: Inventory reduced, tracking number assigned
7. **Delivered**: Final state, order complete
8. **Cancelled**: Can cancel before shipped
9. **OnHold**: Freeze processing (payment issue, stock unavailable)

### **Stock Allocation Logic**

When order is **Confirmed**:
```csharp
foreach (line in salesOrder.Lines)
{
    var availableStock = product.CurrentStock - product.AllocatedStock;
    if (availableStock >= line.QuantityOrdered)
    {
        product.AllocatedStock += line.QuantityOrdered;
        line.Status = Allocated;
    }
    else
    {
        // Insufficient stock - put order on hold
        salesOrder.Status = OnHold;
        break;
    }
}
```

When order is **Shipped**:
```csharp
foreach (line in shipment.Lines)
{
    product.CurrentStock -= line.QuantityShipped;
    product.AllocatedStock -= line.QuantityShipped;

    // Create stock movement record
    StockMovement.Create(product, -line.QuantityShipped,
        MovementType.Sale, salesOrder.OrderNumber);
}
```

---

## 5. UI Components

### **Sales Orders List**
- Table: Order #, Customer, Date, Status, Priority, Total, Actions
- Filters: Status, Customer, Date range, Priority
- Search: Order number, customer name
- Actions: View, Edit (draft), Cancel, Hold

### **Sales Order Detail**
- Header: Order #, Customer, Status badge, Priority badge, Dates
- Customer & Shipping info cards
- Line items table with quantities, prices
- Status history timeline
- Actions: Submit, Confirm, Pick, Pack, Ship (based on status)

### **Sales Order Form** (Create/Edit)
- Customer selection (from Companies where IsCustomer=true)
- Shipping address (can differ from customer address)
- Add products (search, select, quantity, price)
- Price calculation (subtotal, tax, shipping, discount)
- Priority, required date
- Notes

### **Pick List View**
- Grouped by warehouse location
- Checkbox per line item
- Quantity to pick
- Current stock location
- "Mark as Picked" action

### **Packing View**
- Items to pack
- Package dimensions/weight
- Generate packing slip
- "Mark as Packed" action

### **Shipping View**
- Carrier selection
- Shipping method
- Tracking number input
- Weight/dimensions
- Generate shipping label
- "Mark as Shipped" action (reduces inventory)

### **Dashboard Widgets**
- Orders awaiting pickup (count)
- Orders in picking (count)
- Orders ready to ship (count)
- Revenue today/week/month
- Top customers

---

## 6. Implementation Phases

### **Phase 1: Core Sales Orders** (Week 1)
**Goal**: Create and manage sales orders

**Backend**:
- Create entities: SalesOrder, SalesOrderLine
- Create SalesOrderService with CRUD operations
- Create SalesOrdersController
- Add status transitions: Draft → Submitted → Confirmed → Cancelled
- Database migration

**Frontend**:
- Sales orders list component (with filters)
- Sales order detail component
- Sales order form (create/edit)
- Customer selection dropdown
- Product line items editor
- Status badges and workflow actions

**Deliverables**:
- ✅ Create sales orders
- ✅ Add products and calculate totals
- ✅ Submit orders
- ✅ Cancel orders
- ✅ View order list and details

---

### **Phase 2: Stock Allocation & Fulfillment** (Week 2)
**Goal**: Pick, pack, and prepare for shipping

**Backend**:
- Add AllocatedStock field to Product
- Implement stock allocation on Confirm
- Add picking workflow: Confirmed → Picking → Picked
- Add packing workflow: Picked → Packing → Packed
- Update SalesOrderService with picking/packing methods

**Frontend**:
- Pick list component
- Mark items as picked
- Packing component
- Generate packing slip (PDF)
- Real-time stock availability indicator

**Deliverables**:
- ✅ Allocate stock when order confirmed
- ✅ Generate pick lists
- ✅ Track picking progress
- ✅ Mark as packed

---

### **Phase 3: Shipping & Inventory Reduction** (Week 3)
**Goal**: Ship orders and reduce inventory

**Backend**:
- Create Shipment and ShipmentLine entities
- Create ShipmentsController
- Implement shipment creation from sales order
- Reduce inventory when shipped
- Create stock movement records
- Add tracking number and carrier fields

**Frontend**:
- Shipments list component
- Shipment detail component
- Shipping form (carrier, tracking, method)
- Generate shipping label (PDF)
- Delivery confirmation
- Tracking status updates

**Deliverables**:
- ✅ Create shipments
- ✅ Reduce inventory on ship
- ✅ Track shipments
- ✅ Mark as delivered
- ✅ Generate shipping labels

---

### **Phase 4: Reporting & Polish** (Optional)
**Goal**: Analytics and enhancements

**Features**:
- Sales reports (revenue by product, customer, period)
- Order fulfillment metrics (average time to ship)
- Customer order history
- Email notifications (order confirmed, shipped, delivered)
- Partial shipments (ship some items now, rest later)
- Backorders (when stock unavailable)
- Returns/RMA system

---

## 7. Authorization Rules

| Action | Admin | Manager | Staff | Viewer |
|--------|-------|---------|-------|--------|
| View Sales Orders | ✓ | ✓ | ✓ | ✓ |
| Create Sales Order | ✓ | ✓ | ✓ | ✗ |
| Edit Draft SO | ✓ | ✓ | ✓ | ✗ |
| Submit SO | ✓ | ✓ | ✗ | ✗ |
| Confirm SO | ✓ | ✓ | ✗ | ✗ |
| Pick Items | ✓ | ✓ | ✓ | ✗ |
| Pack Items | ✓ | ✓ | ✓ | ✗ |
| Ship Orders | ✓ | ✓ | ✗ | ✗ |
| Cancel SO | ✓ | ✓ | ✗ | ✗ |

---

## 8. Testing Strategy

### **Unit Tests**
- SalesOrderService: status transitions, validations
- Stock allocation logic
- Inventory reduction on shipment
- Price calculations

### **Integration Tests**
- Full workflow: Create → Submit → Confirm → Pick → Pack → Ship
- Stock availability checks
- Multiple shipments per order

### **Manual Testing**
- Create order with multiple products
- Confirm and verify stock allocated
- Pick and pack workflow
- Ship and verify inventory reduced
- Cancel order and verify stock released

---

## 9. Database Changes

### **New Tables**
- `SalesOrders`
- `SalesOrderLines`
- `Shipments`
- `ShipmentLines`
- `StockAllocations` (optional)

### **Modified Tables**
- `Products`: Add `AllocatedStock` field

### **Indexes**
- `SalesOrders`: OrderNumber, CustomerId, Status, OrderDate
- `SalesOrderLines`: SalesOrderId, ProductId, Status
- `Shipments`: ShipmentNumber, SalesOrderId, TrackingNumber, Status

---

## 10. Success Metrics

- ✅ Sales orders created and processed
- ✅ Pick lists generated and used
- ✅ Inventory accurately reduced on shipment
- ✅ Orders fulfilled within target time
- ✅ Zero inventory discrepancies
- ✅ Full audit trail of order lifecycle

---

## Next Steps

1. **Review & Approve** this design
2. **Phase 1 Implementation** (Core Sales Orders)
3. **Testing & Iteration**
4. **Phase 2 Implementation** (Fulfillment)
5. **Phase 3 Implementation** (Shipping)

---

**Document Version**: 1.0
**Last Updated**: 2025-01-06
**Author**: AI Assistant
