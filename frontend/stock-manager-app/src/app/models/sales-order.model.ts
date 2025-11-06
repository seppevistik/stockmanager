// Enums
export enum SalesOrderStatus {
  Draft = 'Draft',
  Submitted = 'Submitted',
  Confirmed = 'Confirmed',
  AwaitingPickup = 'AwaitingPickup',
  Picking = 'Picking',
  Picked = 'Picked',
  Packing = 'Packing',
  Packed = 'Packed',
  Shipped = 'Shipped',
  Delivered = 'Delivered',
  Cancelled = 'Cancelled',
  OnHold = 'OnHold'
}

export enum SalesOrderLineStatus {
  Pending = 'Pending',
  Allocated = 'Allocated',
  Picked = 'Picked',
  Packed = 'Packed',
  Shipped = 'Shipped',
  Cancelled = 'Cancelled'
}

export enum Priority {
  Low = 'Low',
  Normal = 'Normal',
  High = 'High',
  Urgent = 'Urgent'
}

// Main DTOs
export interface SalesOrder {
  id: number;
  orderNumber: string;
  businessId: number;
  customerId: number;
  customerName: string;
  customerEmail: string;

  // Shipping Information
  shipToName: string;
  shipToAddress: string;
  shipToCity: string;
  shipToState: string;
  shipToPostalCode: string;
  shipToCountry: string;
  shipToPhone?: string;

  // Financial
  subTotal: number;
  taxAmount: number;
  taxRate: number;
  shippingCost: number;
  discountAmount: number;
  totalAmount: number;

  // Status & Workflow
  status: SalesOrderStatus;
  statusDisplay: string;
  priority: Priority;
  priorityDisplay: string;

  // Dates
  orderDate: Date;
  requiredDate?: Date;
  promisedDate?: Date;
  shippedDate?: Date;
  deliveredDate?: Date;

  // Fulfillment
  shippingMethod?: string;
  trackingNumber?: string;
  carrier?: string;
  pickedBy?: string;
  packedBy?: string;
  shippedBy?: string;

  // Additional Info
  customerReference?: string;
  notes?: string;
  internalNotes?: string;

  // Audit
  createdBy: string;
  createdAt: Date;
  updatedAt?: Date;

  // Related Data
  lines: SalesOrderLine[];

  // Summary Statistics
  totalLineItems: number;
  totalQuantityOrdered: number;
  totalQuantityPicked: number;
  totalQuantityShipped: number;
  totalQuantityOutstanding: number;
}

export interface SalesOrderLine {
  id: number;
  salesOrderId: number;
  productId: number;

  // Product snapshot
  productName: string;
  productSku: string;
  productDescription?: string;

  // Quantities
  quantityOrdered: number;
  quantityPicked: number;
  quantityShipped: number;
  quantityOutstanding: number;

  // Pricing
  unitPrice: number;
  discountPercent: number;
  lineTotal: number;

  // Fulfillment
  status: SalesOrderLineStatus;
  statusDisplay: string;
  location?: string;
  pickedBy?: string;
  pickedAt?: Date;

  notes?: string;
}

// Create/Update Request DTOs
export interface CreateSalesOrderRequest {
  customerId: number;

  // Shipping Information
  shipToName: string;
  shipToAddress: string;
  shipToCity: string;
  shipToState: string;
  shipToPostalCode: string;
  shipToCountry: string;
  shipToPhone?: string;

  // Financial
  taxRate: number;
  shippingCost: number;
  discountAmount: number;

  // Priority & Dates
  priority: Priority;
  requiredDate?: Date;
  promisedDate?: Date;

  // Additional Info
  customerReference?: string;
  shippingMethod?: string;
  notes?: string;
  internalNotes?: string;

  // Order Lines
  lines: CreateSalesOrderLineRequest[];
}

export interface CreateSalesOrderLineRequest {
  productId: number;
  quantityOrdered: number;
  unitPrice: number;
  discountPercent: number;
  notes?: string;
}

export interface UpdateSalesOrderRequest {
  customerId: number;

  // Shipping Information
  shipToName: string;
  shipToAddress: string;
  shipToCity: string;
  shipToState: string;
  shipToPostalCode: string;
  shipToCountry: string;
  shipToPhone?: string;

  // Financial
  taxRate: number;
  shippingCost: number;
  discountAmount: number;

  // Priority & Dates
  priority: Priority;
  requiredDate?: Date;
  promisedDate?: Date;

  // Additional Info
  customerReference?: string;
  shippingMethod?: string;
  notes?: string;
  internalNotes?: string;

  // Order Lines
  lines: UpdateSalesOrderLineRequest[];
}

export interface UpdateSalesOrderLineRequest {
  id?: number; // Null for new lines, set for existing lines
  productId: number;
  quantityOrdered: number;
  unitPrice: number;
  discountPercent: number;
  notes?: string;
}

// Workflow Action Request DTOs
export interface SubmitOrderRequest {
  notes?: string;
}

export interface ConfirmOrderRequest {
  promisedDate?: Date;
  notes?: string;
}

export interface CancelOrderRequest {
  reason: string;
}

export interface HoldOrderRequest {
  reason: string;
}

export interface ReleaseOrderRequest {
  notes?: string;
}

export interface StartPickingRequest {
  notes?: string;
}

export interface CompletePickingRequest {
  pickedLines: PickedLineDto[];
  notes?: string;
}

export interface PickedLineDto {
  lineId: number;
  quantityPicked: number;
  location?: string;
  notes?: string;
}

export interface StartPackingRequest {
  notes?: string;
}

export interface CompletePackingRequest {
  notes?: string;
}

export interface ShipOrderRequest {
  shippedDate: Date;
  carrier: string;
  trackingNumber?: string;
  notes?: string;
}

export interface DeliverOrderRequest {
  deliveredDate: Date;
  receivedBy?: string;
  notes?: string;
}

// Query/Filter DTOs
export interface SalesOrderListQuery {
  page: number;
  pageSize: number;
  searchTerm?: string;
  customerId?: number;
  status?: SalesOrderStatus;
  priority?: Priority;
  orderDateFrom?: Date;
  orderDateTo?: Date;
  requiredDateFrom?: Date;
  requiredDateTo?: Date;
  sortBy?: string;
  sortDirection?: string;
}

// Statistics DTOs
export interface SalesOrderStatistics {
  totalOrders: number;
  draftOrders: number;
  submittedOrders: number;
  confirmedOrders: number;
  inProgress: number;
  shippedToday: number;
  overdueOrders: number;
  totalRevenue: number;
  pendingRevenue: number;
}

// Summary DTOs
export interface SalesOrderSummary {
  id: number;
  orderNumber: string;
  customerId: number;
  customerName: string;
  orderDate: Date;
  requiredDate?: Date;
  status: SalesOrderStatus;
  statusDisplay: string;
  priority: Priority;
  priorityDisplay: string;
  totalAmount: number;
  totalLineItems: number;
  shipToCity: string;
  shipToState: string;
}

// Paged Result
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
