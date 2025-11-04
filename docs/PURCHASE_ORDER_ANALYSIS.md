# Purchase Order and Receiving System - Analysis Document

## Executive Summary

This document provides a comprehensive analysis for implementing a Purchase Order (PO) and Receiving system for the Stock Manager application. The system will enable businesses to create purchase orders for suppliers, track order status, receive goods, validate deliveries against orders, and automatically update inventory levels.

## Table of Contents

1. [Business Requirements](#business-requirements)
2. [System Overview](#system-overview)
3. [Data Model](#data-model)
4. [Workflow & State Management](#workflow--state-management)
5. [User Stories](#user-stories)
6. [Technical Architecture](#technical-architecture)
7. [API Endpoints](#api-endpoints)
8. [Frontend Components](#frontend-components)
9. [Business Rules & Validation](#business-rules--validation)
10. [Security & Authorization](#security--authorization)
11. [Reporting & Analytics](#reporting--analytics)
12. [Future Enhancements](#future-enhancements)

---

## Business Requirements

### Primary Goals

1. **Purchase Order Creation**: Enable users to create purchase orders for products from suppliers
2. **Order Tracking**: Track purchase orders through their lifecycle (Draft → Submitted → Confirmed → Receiving → Completed/Cancelled)
3. **Goods Receiving**: Record receipt of goods with validation against original order
4. **Variance Management**: Handle discrepancies between ordered and received quantities
5. **Inventory Updates**: Automatically update stock levels when goods are received
6. **Audit Trail**: Maintain complete history of all PO activities

### Key Features

- Create POs with multiple line items (products)
- Select supplier from existing company suppliers
- Track expected delivery dates
- Support partial deliveries (receive order in multiple shipments)
- Validate received quantities against ordered quantities
- Handle overages and shortages
- Update product cost per unit based on actual supplier pricing
- Generate receiving reports
- Cancel orders or individual line items
- Add notes and comments to orders and receipts

---

## System Overview

### Process Flow

```
1. CREATE PURCHASE ORDER
   ↓
   - User selects supplier
   - Adds products and quantities
   - Sets expected delivery date
   - Saves as Draft or submits to supplier
   ↓
2. SUBMIT TO SUPPLIER
   ↓
   - PO status changes to "Submitted"
   - Awaiting supplier confirmation
   ↓
3. SUPPLIER CONFIRMS (Optional)
   ↓
   - PO status changes to "Confirmed"
   - Confirmed delivery date recorded
   ↓
4. RECEIVE GOODS
   ↓
   - User initiates receiving process
   - Scans/enters received quantities
   - Records actual unit costs (if different)
   - Notes any damages or issues
   ↓
5. VALIDATE RECEIPT
   ↓
   - System compares ordered vs received
   - Flags variances for review
   - User approves or rejects
   ↓
6. COMPLETE RECEIPT
   ↓
   - Inventory updated automatically
   - Stock levels increased
   - Cost per unit updated (if configured)
   - PO status updated (Partially Received / Completed)
   ↓
7. CLOSE PURCHASE ORDER
   ↓
   - All items received or cancelled
   - Final audit trail created
```

### System States

**Purchase Order States:**
- `Draft` - Being created, not yet submitted
- `Submitted` - Sent to supplier, awaiting confirmation
- `Confirmed` - Supplier confirmed the order
- `Receiving` - Goods are being received
- `PartiallyReceived` - Some items received, more expected
- `Completed` - All items received and validated
- `Cancelled` - Order cancelled

**Receipt States:**
- `InProgress` - Receipt being recorded
- `PendingValidation` - Awaiting approval due to variances
- `Validated` - Approved and ready to update inventory
- `Completed` - Inventory updated
- `Rejected` - Receipt rejected, not applied to inventory

**Line Item States:**
- `Pending` - Not yet received
- `PartiallyReceived` - Some quantity received
- `FullyReceived` - Complete quantity received
- `Cancelled` - Line item cancelled
- `ShortShipped` - Received less than ordered, closed

---

## Data Model

### Entity: PurchaseOrder

| Field | Type | Description | Required | Default |
|-------|------|-------------|----------|---------|
| Id | int | Primary key | Yes | Auto |
| BusinessId | int | Foreign key to Business | Yes | - |
| CompanyId | int | Foreign key to Company (supplier) | Yes | - |
| OrderNumber | string | Unique order identifier | Yes | Auto-generated |
| OrderDate | DateTime | When order was created | Yes | Now |
| ExpectedDeliveryDate | DateTime? | Expected delivery date | No | - |
| ConfirmedDeliveryDate | DateTime? | Supplier confirmed date | No | - |
| Status | string | Order status (enum) | Yes | Draft |
| SubTotal | decimal | Sum of all line items | Yes | Calculated |
| TaxAmount | decimal? | Tax amount if applicable | No | 0 |
| ShippingCost | decimal? | Shipping cost | No | 0 |
| TotalAmount | decimal | SubTotal + Tax + Shipping | Yes | Calculated |
| Notes | string | Internal notes | No | - |
| SupplierReference | string | Supplier's reference number | No | - |
| CreatedBy | int | User who created | Yes | Current user |
| CreatedAt | DateTime | Creation timestamp | Yes | Now |
| UpdatedAt | DateTime | Last update timestamp | Yes | Now |
| SubmittedAt | DateTime? | When submitted to supplier | No | - |
| CompletedAt | DateTime? | When fully received | No | - |
| CancelledAt | DateTime? | When cancelled | No | - |
| CancellationReason | string | Why it was cancelled | No | - |

**Relationships:**
- Belongs to Business (many-to-one)
- Belongs to Company/Supplier (many-to-one)
- Has many PurchaseOrderLines (one-to-many)
- Has many Receipts (one-to-many)
- Created by User (many-to-one)

**Indexes:**
- BusinessId + OrderNumber (unique)
- BusinessId + Status
- CompanyId
- OrderDate
- ExpectedDeliveryDate

---

### Entity: PurchaseOrderLine

| Field | Type | Description | Required | Default |
|-------|------|-------------|----------|---------|
| Id | int | Primary key | Yes | Auto |
| PurchaseOrderId | int | Foreign key to PurchaseOrder | Yes | - |
| ProductId | int | Foreign key to Product | Yes | - |
| ProductName | string | Product name at time of order | Yes | From Product |
| ProductSku | string | Product SKU at time of order | Yes | From Product |
| QuantityOrdered | decimal | Quantity ordered | Yes | - |
| UnitPrice | decimal | Price per unit | Yes | - |
| LineTotal | decimal | QuantityOrdered * UnitPrice | Yes | Calculated |
| QuantityReceived | decimal | Total quantity received | Yes | 0 |
| QuantityOutstanding | decimal | Ordered - Received | Yes | Calculated |
| Status | string | Line status (enum) | Yes | Pending |
| Notes | string | Line-specific notes | No | - |
| CreatedAt | DateTime | Creation timestamp | Yes | Now |
| UpdatedAt | DateTime | Last update timestamp | Yes | Now |

**Relationships:**
- Belongs to PurchaseOrder (many-to-one)
- Belongs to Product (many-to-one)
- Has many ReceiptLines (one-to-many)

**Indexes:**
- PurchaseOrderId
- ProductId

**Business Rules:**
- ProductName and ProductSku are denormalized to preserve historical data
- QuantityReceived cannot exceed QuantityOrdered (without override)
- QuantityOutstanding = QuantityOrdered - QuantityReceived

---

### Entity: Receipt

| Field | Type | Description | Required | Default |
|-------|------|-------------|----------|---------|
| Id | int | Primary key | Yes | Auto |
| BusinessId | int | Foreign key to Business | Yes | - |
| PurchaseOrderId | int | Foreign key to PurchaseOrder | Yes | - |
| ReceiptNumber | string | Unique receipt identifier | Yes | Auto-generated |
| ReceiptDate | DateTime | When goods were received | Yes | Now |
| ReceivedBy | int | User who received goods | Yes | Current user |
| Status | string | Receipt status (enum) | Yes | InProgress |
| SupplierDeliveryNote | string | Supplier's delivery note # | No | - |
| Notes | string | Receipt notes | No | - |
| HasVariances | bool | Has quantity variances | Yes | Calculated |
| VarianceNotes | string | Explanation of variances | No | - |
| ValidatedBy | int? | User who validated | No | - |
| ValidatedAt | DateTime? | When validated | No | - |
| CompletedAt | DateTime? | When completed | No | - |
| CreatedAt | DateTime | Creation timestamp | Yes | Now |
| UpdatedAt | DateTime | Last update timestamp | Yes | Now |

**Relationships:**
- Belongs to Business (many-to-one)
- Belongs to PurchaseOrder (many-to-one)
- Has many ReceiptLines (one-to-many)
- Received by User (many-to-one)
- Validated by User (many-to-one)

**Indexes:**
- BusinessId + ReceiptNumber (unique)
- PurchaseOrderId
- ReceiptDate
- Status

---

### Entity: ReceiptLine

| Field | Type | Description | Required | Default |
|-------|------|-------------|----------|---------|
| Id | int | Primary key | Yes | Auto |
| ReceiptId | int | Foreign key to Receipt | Yes | - |
| PurchaseOrderLineId | int | Foreign key to PurchaseOrderLine | Yes | - |
| ProductId | int | Foreign key to Product | Yes | - |
| QuantityOrdered | decimal | Ordered quantity (from PO line) | Yes | From PO |
| QuantityReceived | decimal | Actually received quantity | Yes | - |
| QuantityVariance | decimal | Received - Ordered | Yes | Calculated |
| UnitPriceOrdered | decimal | Ordered price (from PO line) | Yes | From PO |
| UnitPriceReceived | decimal? | Actual price if different | No | - |
| PriceVariance | decimal | Price difference | Yes | Calculated |
| Condition | string | Item condition (Good/Damaged/Defective) | Yes | Good |
| DamageNotes | string | Notes about damages | No | - |
| Location | string | Where item was placed | No | - |
| BatchNumber | string | Batch/Lot number | No | - |
| ExpiryDate | DateTime? | Expiry date if applicable | No | - |
| CreatedAt | DateTime | Creation timestamp | Yes | Now |

**Relationships:**
- Belongs to Receipt (many-to-one)
- Belongs to PurchaseOrderLine (many-to-one)
- Belongs to Product (many-to-one)

**Indexes:**
- ReceiptId
- PurchaseOrderLineId
- ProductId

**Business Rules:**
- QuantityVariance = QuantityReceived - QuantityOrdered
- PriceVariance = (UnitPriceReceived ?? UnitPriceOrdered) - UnitPriceOrdered
- Condition defaults to "Good"

---

## Workflow & State Management

### Purchase Order Lifecycle

#### 1. Draft State
- **Allowed Actions:**
  - Edit order details
  - Add/remove/edit line items
  - Delete entire order
  - Submit to supplier
- **Transitions:**
  - → Submitted (when submitted)
  - → Cancelled (when deleted)

#### 2. Submitted State
- **Allowed Actions:**
  - Mark as confirmed
  - Edit delivery dates
  - Add notes
  - Cancel order
  - Start receiving (optional, can skip to Confirmed)
- **Transitions:**
  - → Confirmed (supplier confirms)
  - → Receiving (start receiving without confirmation)
  - → Cancelled

#### 3. Confirmed State
- **Allowed Actions:**
  - Update confirmed delivery date
  - Add notes
  - Start receiving
  - Cancel order
- **Transitions:**
  - → Receiving (first receipt created)
  - → Cancelled

#### 4. Receiving State
- **Allowed Actions:**
  - Create receipt
  - View receipt history
  - Add notes
  - Cancel remaining items
- **Transitions:**
  - → PartiallyReceived (some items received)
  - → Completed (all items received)
  - → Cancelled

#### 5. PartiallyReceived State
- **Allowed Actions:**
  - Create additional receipts
  - View receipt history
  - Close remaining items
  - Cancel remaining items
- **Transitions:**
  - → Completed (all items received)
  - → Cancelled (if all remaining cancelled)

#### 6. Completed State
- **Allowed Actions:**
  - View only (read-only)
  - Generate reports
- **Transitions:**
  - None (terminal state)

#### 7. Cancelled State
- **Allowed Actions:**
  - View only (read-only)
- **Transitions:**
  - None (terminal state)

### Receipt Lifecycle

#### 1. InProgress State
- **Allowed Actions:**
  - Add/edit receipt lines
  - Enter quantities
  - Record damages
  - Save draft
  - Submit for validation
  - Delete receipt
- **Transitions:**
  - → PendingValidation (if variances exist)
  - → Validated (if no variances or auto-approve enabled)

#### 2. PendingValidation State
- **Allowed Actions:**
  - Review variances
  - Add variance notes
  - Approve receipt
  - Reject receipt
- **Transitions:**
  - → Validated (when approved)
  - → Rejected (when rejected)
  - → InProgress (when sent back for editing)

#### 3. Validated State
- **Allowed Actions:**
  - Apply to inventory
  - View details
- **Transitions:**
  - → Completed (when applied to inventory)

#### 4. Completed State
- **Allowed Actions:**
  - View only
  - Generate reports
- **Transitions:**
  - None (terminal state)

#### 5. Rejected State
- **Allowed Actions:**
  - View details
  - Create new corrected receipt
- **Transitions:**
  - None (terminal state)

---

## User Stories

### Purchase Order Creation

**As a** Warehouse Manager
**I want to** create a purchase order for a supplier
**So that** I can order products to replenish inventory

**Acceptance Criteria:**
- Can select a supplier from the company list (filtered to suppliers only)
- Can add multiple products to the order
- For each product, can specify quantity and unit price
- Can use supplier-specific pricing if configured in product-supplier relationship
- Can set expected delivery date
- Can add notes and internal comments
- Can save as draft for later completion
- Can submit directly to supplier
- Order number is auto-generated
- Order total is calculated automatically

---

### Product Selection from Supplier Catalog

**As a** Purchasing Agent
**I want to** see only products that are available from the selected supplier
**So that** I don't order products from wrong suppliers

**Acceptance Criteria:**
- When creating PO, after selecting supplier, product list shows only products linked to that supplier
- Product dropdown shows: Product Name, SKU, and supplier's product code (if available)
- Default price is pre-filled from product-supplier relationship
- Can override price if needed
- Can see supplier's lead time as a reference

---

### Receiving Goods

**As a** Warehouse Operator
**I want to** record receipt of goods from a purchase order
**So that** inventory is updated and we track what was actually delivered

**Acceptance Criteria:**
- Can search for PO by order number or supplier
- Can view all outstanding PO line items
- Can enter received quantity for each item
- Can mark items as damaged or defective
- Can record batch numbers and expiry dates
- Can enter supplier's delivery note number
- Can save partial receipts (not all items received)
- Can add notes about the delivery

---

### Variance Management

**As a** Inventory Manager
**I want to** review and approve receipts with variances
**So that** I can control inventory accuracy and investigate discrepancies

**Acceptance Criteria:**
- System automatically flags receipts where received qty ≠ ordered qty
- System shows clear variance report (ordered vs received)
- Can add notes explaining variances
- Can approve variances to update inventory
- Can reject receipt and send back for correction
- Variance types include:
  - Over-shipment (received more than ordered)
  - Short-shipment (received less than ordered)
  - Damaged goods
  - Wrong items

---

### Partial Deliveries

**As a** Purchasing Agent
**I want to** receive a purchase order in multiple shipments
**So that** I can process deliveries as they arrive

**Acceptance Criteria:**
- Can create multiple receipts for the same PO
- System tracks total received across all receipts
- Can see outstanding quantities per line item
- PO remains in "PartiallyReceived" state until all items received
- Each receipt has its own receipt number
- Can close remaining quantities if final delivery is short

---

### Inventory Update

**As a** System User
**I want** inventory to update automatically when receipts are completed
**So that** stock levels are accurate without manual updates

**Acceptance Criteria:**
- Upon receipt completion, product stock quantities increase by received amount
- Stock movement record is created for audit trail
- Movement type is "Purchase Order Receipt"
- References receipt number and PO number
- Updates product cost per unit (if configured)
- Sends notifications if product was below minimum stock

---

### Order Cancellation

**As a** Purchasing Agent
**I want to** cancel a purchase order or specific line items
**So that** I can manage order changes

**Acceptance Criteria:**
- Can cancel entire PO if not yet received
- Can cancel individual line items
- Must provide cancellation reason
- Cannot cancel items that have already been received
- Cancelled orders show in order history
- Can filter order list to exclude cancelled orders

---

### Reporting and Analytics

**As a** Business Owner
**I want to** see reports on purchase orders and receiving
**So that** I can analyze supplier performance and inventory trends

**Acceptance Criteria:**
- Dashboard shows: Open POs, Expected deliveries this week, Value of outstanding orders
- Can see supplier performance (on-time delivery %, accuracy %)
- Can see average lead time by supplier
- Can see variance trends
- Can export PO data to Excel/CSV
- Can print purchase orders
- Can generate receiving reports

---

## Technical Architecture

### Backend Implementation

#### Technology Stack
- **Framework:** ASP.NET Core 8.0
- **ORM:** Entity Framework Core
- **Database:** SQL Server / PostgreSQL
- **Authentication:** JWT tokens
- **Logging:** Serilog
- **Validation:** FluentValidation (optional)

#### Project Structure

```
backend/
├── StockManager.Core/
│   ├── Entities/
│   │   ├── PurchaseOrder.cs
│   │   ├── PurchaseOrderLine.cs
│   │   ├── Receipt.cs
│   │   └── ReceiptLine.cs
│   ├── DTOs/
│   │   ├── PurchaseOrderDto.cs
│   │   ├── CreatePurchaseOrderDto.cs
│   │   ├── UpdatePurchaseOrderDto.cs
│   │   ├── PurchaseOrderLineDto.cs
│   │   ├── ReceiptDto.cs
│   │   ├── CreateReceiptDto.cs
│   │   ├── ReceiptLineDto.cs
│   │   └── ReceiptValidationDto.cs
│   ├── Enums/
│   │   ├── PurchaseOrderStatus.cs
│   │   ├── ReceiptStatus.cs
│   │   ├── LineItemStatus.cs
│   │   └── ItemCondition.cs
│   └── Interfaces/
│       ├── IPurchaseOrderRepository.cs
│       ├── IReceiptRepository.cs
│       └── IPurchaseOrderService.cs
├── StockManager.Data/
│   ├── Contexts/
│   │   └── ApplicationDbContext.cs (update)
│   └── Repositories/
│       ├── PurchaseOrderRepository.cs
│       └── ReceiptRepository.cs
└── StockManager.API/
    ├── Controllers/
    │   ├── PurchaseOrdersController.cs
    │   └── ReceiptsController.cs
    └── Services/
        ├── PurchaseOrderService.cs
        ├── ReceiptService.cs
        └── InventoryUpdateService.cs
```

#### Key Service Methods

**PurchaseOrderService:**
```csharp
Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, int businessId, int userId)
Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(int id, UpdatePurchaseOrderDto dto, int businessId)
Task<bool> SubmitPurchaseOrderAsync(int id, int businessId, int userId)
Task<bool> ConfirmPurchaseOrderAsync(int id, DateTime confirmedDate, int businessId)
Task<bool> CancelPurchaseOrderAsync(int id, string reason, int businessId, int userId)
Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersAsync(int businessId, PurchaseOrderFilter filter)
Task<PurchaseOrderDto> GetPurchaseOrderByIdAsync(int id, int businessId)
Task<IEnumerable<PurchaseOrderDto>> GetOutstandingOrdersAsync(int businessId)
```

**ReceiptService:**
```csharp
Task<ReceiptDto> CreateReceiptAsync(CreateReceiptDto dto, int businessId, int userId)
Task<ReceiptDto> UpdateReceiptAsync(int id, UpdateReceiptDto dto, int businessId)
Task<ReceiptValidationDto> ValidateReceiptAsync(int id, int businessId)
Task<bool> ApproveReceiptAsync(int id, string notes, int businessId, int userId)
Task<bool> RejectReceiptAsync(int id, string reason, int businessId, int userId)
Task<bool> CompleteReceiptAsync(int id, int businessId, int userId)
Task<IEnumerable<ReceiptDto>> GetReceiptsForPurchaseOrderAsync(int poId, int businessId)
```

**InventoryUpdateService:**
```csharp
Task<bool> ApplyReceiptToInventoryAsync(Receipt receipt, int userId)
Task RollbackReceiptFromInventoryAsync(int receiptId, int userId)
Task<StockMovement> CreateStockMovementFromReceiptAsync(ReceiptLine line, Receipt receipt, int userId)
```

---

### Frontend Implementation

#### Technology Stack
- **Framework:** Angular 18
- **UI Library:** Angular Material (Material Design 3)
- **State Management:** RxJS / NgRx (if complexity grows)
- **Forms:** Reactive Forms
- **HTTP Client:** Angular HttpClient
- **Routing:** Angular Router

#### Project Structure

```
frontend/src/app/
├── models/
│   ├── purchase-order.model.ts
│   ├── purchase-order-line.model.ts
│   ├── receipt.model.ts
│   └── receipt-line.model.ts
├── services/
│   ├── purchase-order.service.ts
│   ├── receipt.service.ts
│   └── inventory-update.service.ts
├── components/
│   ├── purchase-orders/
│   │   ├── purchase-orders-list/
│   │   ├── purchase-order-form/
│   │   ├── purchase-order-details/
│   │   └── purchase-order-line-item/
│   ├── receipts/
│   │   ├── receipts-list/
│   │   ├── receipt-form/
│   │   ├── receipt-validation/
│   │   └── variance-review/
│   └── reports/
│       ├── po-dashboard/
│       └── supplier-performance/
└── guards/
    └── unsaved-changes.guard.ts
```

---

## API Endpoints

### Purchase Orders

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/purchase-orders` | Get all POs (with filters) | Required |
| GET | `/api/purchase-orders/{id}` | Get PO by ID | Required |
| GET | `/api/purchase-orders/outstanding` | Get outstanding POs | Required |
| GET | `/api/purchase-orders/{id}/receipts` | Get receipts for PO | Required |
| POST | `/api/purchase-orders` | Create new PO | Manager+ |
| PUT | `/api/purchase-orders/{id}` | Update PO | Manager+ |
| POST | `/api/purchase-orders/{id}/submit` | Submit PO to supplier | Manager+ |
| POST | `/api/purchase-orders/{id}/confirm` | Confirm PO | Manager+ |
| POST | `/api/purchase-orders/{id}/cancel` | Cancel PO | Manager+ |
| DELETE | `/api/purchase-orders/{id}` | Delete draft PO | Manager+ |

**Query Parameters for GET /api/purchase-orders:**
- `status` - Filter by status (Draft, Submitted, etc.)
- `supplierId` - Filter by supplier
- `fromDate` - Filter by order date from
- `toDate` - Filter by order date to
- `search` - Search by order number or supplier name
- `page` - Page number
- `pageSize` - Items per page

---

### Receipts

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/receipts` | Get all receipts | Required |
| GET | `/api/receipts/{id}` | Get receipt by ID | Required |
| GET | `/api/receipts/pending-validation` | Get receipts pending validation | Manager+ |
| POST | `/api/receipts` | Create new receipt | Required |
| PUT | `/api/receipts/{id}` | Update receipt | Required |
| POST | `/api/receipts/{id}/validate` | Validate receipt | Manager+ |
| POST | `/api/receipts/{id}/approve` | Approve receipt | Manager+ |
| POST | `/api/receipts/{id}/reject` | Reject receipt | Manager+ |
| POST | `/api/receipts/{id}/complete` | Complete receipt & update inventory | Manager+ |
| DELETE | `/api/receipts/{id}` | Delete draft receipt | Manager+ |

---

### Reports & Analytics

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/reports/po-dashboard` | PO dashboard metrics | Manager+ |
| GET | `/api/reports/supplier-performance` | Supplier performance metrics | Manager+ |
| GET | `/api/reports/receiving-summary` | Receiving summary report | Manager+ |
| GET | `/api/reports/variance-report` | Variance analysis | Manager+ |

---

## Frontend Components

### 1. Purchase Orders List Component

**Purpose:** Display all purchase orders with filtering and search

**Features:**
- Material table with columns: Order #, Date, Supplier, Expected Delivery, Status, Total, Actions
- Status chips with color coding
- Search by order number or supplier
- Filter by status, date range, supplier
- Sort by any column
- Quick actions: View, Edit (drafts), Receive, Cancel
- Pagination

**Template:**
```html
<div class="po-list-container">
  <div class="header">
    <h1>Purchase Orders</h1>
    <button mat-raised-button color="primary" (click)="createPO()">
      <mat-icon>add</mat-icon>
      New Purchase Order
    </button>
  </div>

  <mat-card>
    <mat-card-content>
      <!-- Filters -->
      <div class="filters">
        <mat-form-field>
          <mat-label>Search</mat-label>
          <input matInput [(ngModel)]="searchTerm" placeholder="Order # or supplier">
          <mat-icon matPrefix>search</mat-icon>
        </mat-form-field>

        <mat-form-field>
          <mat-label>Status</mat-label>
          <mat-select [(ngModel)]="statusFilter">
            <mat-option value="">All</mat-option>
            <mat-option value="Draft">Draft</mat-option>
            <mat-option value="Submitted">Submitted</mat-option>
            <mat-option value="Confirmed">Confirmed</mat-option>
            <mat-option value="PartiallyReceived">Partially Received</mat-option>
            <mat-option value="Completed">Completed</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field>
          <mat-label>Supplier</mat-label>
          <mat-select [(ngModel)]="supplierFilter">
            <mat-option value="">All Suppliers</mat-option>
            <mat-option *ngFor="let supplier of suppliers" [value]="supplier.id">
              {{ supplier.name }}
            </mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      <!-- Table -->
      <table mat-table [dataSource]="purchaseOrders">
        <!-- Columns defined here -->
      </table>

      <mat-paginator [length]="totalCount" [pageSize]="pageSize"></mat-paginator>
    </mat-card-content>
  </mat-card>
</div>
```

---

### 2. Purchase Order Form Component

**Purpose:** Create and edit purchase orders

**Features:**
- Supplier selection
- Expected delivery date picker
- Line items table with add/remove
- Product autocomplete
- Quantity and price inputs
- Automatic total calculation
- Notes field
- Save as draft or submit
- Validation

**Form Structure:**
```typescript
purchaseOrderForm = this.fb.group({
  companyId: [null, Validators.required],
  expectedDeliveryDate: [null],
  notes: [''],
  lines: this.fb.array([])
});

lineItemForm = this.fb.group({
  productId: [null, Validators.required],
  quantityOrdered: [0, [Validators.required, Validators.min(1)]],
  unitPrice: [0, [Validators.required, Validators.min(0)]],
  notes: ['']
});
```

**Key Methods:**
```typescript
onSupplierChange(supplierId: number): void
addLineItem(): void
removeLineItem(index: number): void
onProductSelect(productId: number, lineIndex: number): void
calculateTotal(): number
saveDraft(): void
submitOrder(): void
```

---

### 3. Purchase Order Details Component

**Purpose:** View purchase order details with receiving options

**Features:**
- Read-only display of PO header
- Line items table showing ordered, received, outstanding quantities
- Receipt history
- Action buttons based on status:
  - Edit (if draft)
  - Submit (if draft)
  - Confirm (if submitted)
  - Receive Goods (if submitted/confirmed)
  - Cancel
- Status timeline showing PO progression
- Print functionality

---

### 4. Receipt Form Component

**Purpose:** Record receipt of goods

**Features:**
- PO header information (read-only)
- Line items table with:
  - Product name/SKU
  - Quantity ordered
  - Previously received
  - Outstanding
  - Receiving now (editable)
  - Unit price
  - Condition dropdown
  - Batch/Lot number
  - Expiry date
- Supplier delivery note field
- Notes field
- Variance indicator
- Save draft or submit for validation

**Template Concept:**
```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Receive Goods - PO #{{ purchaseOrder.orderNumber }}</mat-card-title>
  </mat-card-header>

  <mat-card-content>
    <!-- PO Info -->
    <div class="po-info">
      <div>Supplier: {{ purchaseOrder.companyName }}</div>
      <div>Order Date: {{ purchaseOrder.orderDate | date }}</div>
      <div>Expected: {{ purchaseOrder.expectedDeliveryDate | date }}</div>
    </div>

    <!-- Receipt Form -->
    <form [formGroup]="receiptForm">
      <mat-form-field>
        <mat-label>Receipt Date</mat-label>
        <input matInput [matDatepicker]="picker" formControlName="receiptDate">
        <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
        <mat-datepicker #picker></mat-datepicker>
      </mat-form-field>

      <mat-form-field>
        <mat-label>Supplier Delivery Note</mat-label>
        <input matInput formControlName="supplierDeliveryNote">
      </mat-form-field>

      <!-- Line Items Table -->
      <table mat-table [dataSource]="receiptLines" formArrayName="lines">
        <ng-container matColumnDef="product">
          <th mat-header-cell *matHeaderCellDef>Product</th>
          <td mat-cell *matCellDef="let line">
            {{ line.productName }}<br>
            <small>{{ line.productSku }}</small>
          </td>
        </ng-container>

        <ng-container matColumnDef="ordered">
          <th mat-header-cell *matHeaderCellDef>Ordered</th>
          <td mat-cell *matCellDef="let line">{{ line.quantityOrdered }}</td>
        </ng-container>

        <ng-container matColumnDef="previouslyReceived">
          <th mat-header-cell *matHeaderCellDef>Previously Received</th>
          <td mat-cell *matCellDef="let line">{{ line.quantityReceived }}</td>
        </ng-container>

        <ng-container matColumnDef="outstanding">
          <th mat-header-cell *matHeaderCellDef>Outstanding</th>
          <td mat-cell *matCellDef="let line">{{ line.quantityOutstanding }}</td>
        </ng-container>

        <ng-container matColumnDef="receiving">
          <th mat-header-cell *matHeaderCellDef>Receiving Now</th>
          <td mat-cell *matCellDef="let line; let i = index" [formGroupName]="i">
            <mat-form-field>
              <input matInput type="number" formControlName="quantityReceived" min="0">
            </mat-form-field>
          </td>
        </ng-container>

        <ng-container matColumnDef="condition">
          <th mat-header-cell *matHeaderCellDef>Condition</th>
          <td mat-cell *matCellDef="let line; let i = index" [formGroupName]="i">
            <mat-select formControlName="condition">
              <mat-option value="Good">Good</mat-option>
              <mat-option value="Damaged">Damaged</mat-option>
              <mat-option value="Defective">Defective</mat-option>
            </mat-select>
          </td>
        </ng-container>

        <!-- More columns -->
      </table>

      <mat-form-field class="full-width">
        <mat-label>Receipt Notes</mat-label>
        <textarea matInput formControlName="notes" rows="3"></textarea>
      </mat-form-field>
    </form>

    <div class="actions">
      <button mat-button (click)="cancel()">Cancel</button>
      <button mat-button (click)="saveDraft()">Save Draft</button>
      <button mat-raised-button color="primary" (click)="submitReceipt()">
        Submit for Validation
      </button>
    </div>
  </mat-card-content>
</mat-card>
```

---

### 5. Variance Review Component

**Purpose:** Review and approve/reject receipts with variances

**Features:**
- Side-by-side comparison (ordered vs received)
- Variance calculations and highlights
- Variance notes field
- Approve/Reject buttons
- Send back for correction option

**Display:**
```html
<mat-card class="variance-review">
  <mat-card-header>
    <mat-card-title>
      <mat-icon color="warn">warning</mat-icon>
      Receipt Variance Detected - Receipt #{{ receipt.receiptNumber }}
    </mat-card-title>
  </mat-card-header>

  <mat-card-content>
    <div class="variance-summary">
      <h3>Variance Summary</h3>
      <ul>
        <li *ngFor="let variance of variances">
          <strong>{{ variance.productName }}</strong>:
          Ordered {{ variance.ordered }},
          Received {{ variance.received }}
          <span [class.negative]="variance.variance < 0"
                [class.positive]="variance.variance > 0">
            ({{ variance.variance > 0 ? '+' : '' }}{{ variance.variance }})
          </span>
        </li>
      </ul>
    </div>

    <mat-form-field class="full-width">
      <mat-label>Variance Explanation</mat-label>
      <textarea matInput [(ngModel)]="varianceNotes" rows="4" required></textarea>
    </mat-form-field>

    <div class="actions">
      <button mat-button (click)="sendBackForCorrection()">
        Send Back for Correction
      </button>
      <button mat-button color="warn" (click)="rejectReceipt()">
        Reject Receipt
      </button>
      <button mat-raised-button color="primary" (click)="approveReceipt()">
        Approve & Update Inventory
      </button>
    </div>
  </mat-card-content>
</mat-card>
```

---

### 6. PO Dashboard Component

**Purpose:** Show key metrics and insights

**Features:**
- KPI cards:
  - Open purchase orders count
  - Total value of open POs
  - Expected deliveries this week
  - Receipts pending validation
- Recent purchase orders list
- Overdue deliveries alert
- Charts:
  - POs by status (pie chart)
  - Purchase volume by month (bar chart)
  - Top suppliers by volume

---

## Business Rules & Validation

### Purchase Order Rules

1. **Supplier Selection:**
   - Only companies marked as suppliers can be selected
   - Supplier must be active (not deleted)

2. **Product Selection:**
   - Products must be linked to the selected supplier (or show all if no link exists)
   - Same product cannot be added twice to the same PO
   - Quantity must be greater than 0
   - Unit price must be 0 or greater

3. **Date Validation:**
   - Order date cannot be in the future
   - Expected delivery date should be after order date (warning, not error)

4. **Status Transitions:**
   - Can only submit orders in Draft status
   - Can only confirm orders in Submitted status
   - Cannot edit orders that have been submitted (except certain fields)
   - Cannot delete orders with receipts
   - Cannot cancel orders that are fully received

5. **Line Item Rules:**
   - Must have at least one line item to submit
   - Cannot remove line items that have been partially received
   - Can cancel individual line items with reason

### Receipt Rules

1. **Quantity Validation:**
   - Received quantity cannot be negative
   - Warning if received > ordered (overage)
   - Warning if received < outstanding (shortage)
   - Can configure tolerance percentage (e.g., ±5% allowed without approval)

2. **Variance Thresholds:**
   - Minor variance (within tolerance) - auto-approve
   - Major variance (exceeds tolerance) - requires approval
   - Any damaged/defective items - requires notes

3. **Validation Requirements:**
   - Receipt date cannot be in the future
   - Receipt date should not be before PO order date (warning)
   - At least one line item must have quantity > 0
   - Damaged items must have damage notes

4. **Inventory Update Rules:**
   - Only completed receipts update inventory
   - Stock movement created for audit trail
   - Product cost can be updated based on configuration:
     - Never update
     - Always update to latest price
     - Update if new price is different by X%
     - Weighted average cost

5. **Partial Receipt Rules:**
   - Can create multiple receipts until all items received
   - Cannot exceed total ordered quantity across all receipts
   - PO auto-completes when all items fully received
   - Can manually close remaining quantities

### Authorization Rules

| Action | Admin | Manager | User | Warehouse |
|--------|-------|---------|------|-----------|
| Create PO | ✓ | ✓ | ✗ | ✗ |
| Edit Draft PO | ✓ | ✓ | ✗ | ✗ |
| Submit PO | ✓ | ✓ | ✗ | ✗ |
| Confirm PO | ✓ | ✓ | ✗ | ✗ |
| Cancel PO | ✓ | ✓ | ✗ | ✗ |
| Create Receipt | ✓ | ✓ | ✓ | ✓ |
| Edit Receipt | ✓ | ✓ | ✓ | ✓ |
| Approve Variances | ✓ | ✓ | ✗ | ✗ |
| Complete Receipt | ✓ | ✓ | ✗ | ✗ |
| View Reports | ✓ | ✓ | View Only | View Only |

---

## Security & Authorization

### Authentication
- All endpoints require JWT authentication
- User must belong to the business associated with the PO/Receipt

### Authorization
- Role-based access control (RBAC)
- Business-level data isolation
- Users can only access POs and receipts for their business

### Data Validation
- Input sanitization to prevent SQL injection
- Validate all numeric inputs
- Validate date ranges
- Prevent mass assignment vulnerabilities

### Audit Trail
- Log all PO creations, submissions, cancellations
- Log all receipt creations, approvals, completions
- Log all inventory updates from receipts
- Track user who performed each action
- Timestamp all actions

---

## Reporting & Analytics

### Standard Reports

1. **Open Purchase Orders Report**
   - List of all open POs
   - Grouped by supplier
   - Shows expected delivery dates
   - Highlights overdue orders

2. **Receiving Report**
   - Daily/Weekly/Monthly receipts
   - Quantity received by product
   - Value of goods received
   - Variance statistics

3. **Supplier Performance Report**
   - On-time delivery percentage
   - Average lead time
   - Variance rate
   - Total purchase volume
   - Rating/scoring

4. **Variance Analysis Report**
   - Variances by product
   - Variances by supplier
   - Trend over time
   - Cost impact

5. **Inventory Valuation Report**
   - Current stock value
   - Value of goods in transit (submitted POs)
   - Expected receipts value

### Dashboard Metrics

- **Open POs:** Count and total value
- **Expected This Week:** Deliveries expected in next 7 days
- **Pending Validation:** Receipts awaiting approval
- **Overdue Deliveries:** POs past expected delivery date
- **Monthly Purchase Volume:** Chart showing spend trend
- **Top Suppliers:** By volume and frequency
- **Average Lead Time:** By supplier
- **Variance Rate:** Percentage of receipts with variances

---

## Future Enhancements

### Phase 2 Features

1. **Email Notifications:**
   - Send PO to supplier via email
   - Notify when delivery is expected soon
   - Alert managers of variances
   - Remind about overdue deliveries

2. **Supplier Portal:**
   - Suppliers can log in to view POs
   - Confirm orders online
   - Update delivery dates
   - Upload shipping documents

3. **Barcode Scanning:**
   - Scan products during receiving
   - Auto-populate quantities
   - Batch number scanning
   - Mobile app for warehouse

4. **Advanced Analytics:**
   - Predictive analytics for order timing
   - Demand forecasting
   - Optimal order quantities
   - Supplier recommendations

5. **Integration:**
   - QuickBooks/Xero integration for accounting
   - Shipping carrier integration for tracking
   - EDI integration for large suppliers
   - Email parsing for delivery notifications

6. **Quality Control:**
   - Inspection checklists
   - Quality scoring
   - Reject and return process
   - Supplier quality ratings

7. **Multi-Currency Support:**
   - Support for international suppliers
   - Currency conversion
   - Exchange rate tracking

8. **Approval Workflows:**
   - Multi-level approval for large POs
   - Budget checking
   - Approval routing rules

9. **Contract Management:**
   - Link POs to supplier contracts
   - Contract pricing enforcement
   - Contract expiry alerts

10. **Advanced Receiving:**
    - Cross-docking
    - Put-away suggestions
    - Quality inspection integration
    - Serial number tracking

---

## Implementation Phases

### Phase 1: Core Functionality (MVP)
**Timeline:** 3-4 weeks

**Week 1-2: Backend**
- Database schema and migrations
- Entity models and DTOs
- Repository layer
- Basic service layer
- Core API endpoints

**Week 3: Backend Completion**
- Advanced service methods
- Validation logic
- Inventory update service
- Unit tests

**Week 4: Frontend**
- Models and services
- PO list component
- PO form component
- Basic receipt form
- Integration testing

### Phase 2: Receiving & Validation
**Timeline:** 2-3 weeks

**Week 5:**
- Receipt validation component
- Variance review component
- Inventory update integration
- Status management

**Week 6:**
- Receipt history
- Partial delivery support
- Enhanced validation
- Testing

### Phase 3: Reporting & Polish
**Timeline:** 2 weeks

**Week 7:**
- Dashboard component
- Standard reports
- Export functionality
- Print templates

**Week 8:**
- UI/UX refinements
- Performance optimization
- Documentation
- User acceptance testing

### Phase 4: Advanced Features
**Timeline:** 3-4 weeks (Future)

- Email notifications
- Barcode scanning
- Advanced analytics
- Third-party integrations

---

## Database Migration Strategy

### Migration Steps

1. **Create new tables:**
   ```sql
   CREATE TABLE PurchaseOrders (...)
   CREATE TABLE PurchaseOrderLines (...)
   CREATE TABLE Receipts (...)
   CREATE TABLE ReceiptLines (...)
   ```

2. **Add indexes:**
   - Performance optimization for queries
   - Foreign key indexes

3. **Add constraints:**
   - Foreign keys
   - Check constraints
   - Unique constraints

4. **Seed data (if needed):**
   - Default statuses
   - Sample data for testing

### Rollback Plan

- Keep migration reversible
- Test rollback in development
- Backup database before production migration
- Document rollback procedure

---

## Testing Strategy

### Unit Tests
- Service layer methods
- Business logic validation
- Calculation methods (totals, variances)
- State transition logic

### Integration Tests
- API endpoint tests
- Database operations
- Service interactions
- Authentication/Authorization

### End-to-End Tests
- Complete PO creation flow
- Receiving workflow
- Variance approval workflow
- Inventory update verification

### User Acceptance Testing
- Real user scenarios
- Edge cases
- Performance under load
- Mobile responsiveness

---

## Performance Considerations

### Database Optimization
- Proper indexing on foreign keys and query columns
- Pagination for large result sets
- Eager loading to prevent N+1 queries
- Consider archiving old completed POs

### Caching Strategy
- Cache supplier list
- Cache product catalog
- Cache user permissions
- Invalidate on updates

### Frontend Optimization
- Lazy loading of routes
- Virtual scrolling for large tables
- Debounce search inputs
- Optimize change detection

### Scalability
- Design for multi-tenant architecture
- Consider read replicas for reporting
- Async processing for inventory updates
- Message queue for notifications (future)

---

## Risks & Mitigation

### Technical Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Inventory update conflicts | High | Medium | Use database transactions, optimistic locking |
| Performance with large datasets | Medium | High | Implement pagination, indexing, archiving |
| Complex state management | Medium | Medium | Clear state diagrams, thorough testing |
| Data integrity issues | High | Low | Validation, constraints, audit logging |

### Business Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| User adoption | High | Medium | Good UX, training, documentation |
| Process changes | Medium | High | Change management, phased rollout |
| Data migration errors | High | Low | Thorough testing, backup strategy |
| Integration complexity | Medium | Medium | Start simple, iterate |

---

## Success Criteria

### Functional Requirements Met
- ✓ Can create and manage purchase orders
- ✓ Can receive goods and record variances
- ✓ Inventory automatically updates on receipt completion
- ✓ Variances require approval
- ✓ Full audit trail maintained
- ✓ Reports provide actionable insights

### Performance Metrics
- PO creation time < 2 minutes
- Receipt recording time < 5 minutes
- Page load times < 2 seconds
- Inventory update completes within 5 seconds
- Support for 1000+ POs and 10,000+ receipts

### User Satisfaction
- Positive feedback from warehouse staff
- Reduced data entry errors
- Time savings vs. manual process
- Reduced inventory discrepancies

---

## Conclusion

This purchase order and receiving system will provide comprehensive functionality for managing procurement, receiving goods, handling variances, and maintaining accurate inventory levels. The phased implementation approach allows for iterative development with continuous user feedback.

The system is designed to be scalable, maintainable, and extensible to accommodate future enhancements such as supplier portals, barcode scanning, and advanced analytics.

**Next Steps:**
1. Review and approve this analysis
2. Prioritize features for MVP
3. Begin backend database design and entity creation
4. Set up development environment and initial project structure
5. Start with Phase 1 implementation

---

**Document Version:** 1.0
**Last Updated:** 2025-11-04
**Author:** System Analysis Team
**Status:** Pending Review
