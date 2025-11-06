export enum PurchaseOrderStatus {
  Draft = 0,
  Submitted = 1,
  Confirmed = 2,
  Receiving = 3,
  PartiallyReceived = 4,
  Completed = 5,
  Cancelled = 6
}

export enum LineItemStatus {
  Pending = 0,
  PartiallyReceived = 1,
  FullyReceived = 2,
  Cancelled = 3,
  ShortShipped = 4
}

export interface PurchaseOrder {
  id: number;
  businessId: number;
  companyId: number;
  companyName: string;
  orderNumber: string;
  orderDate: Date;
  expectedDeliveryDate?: Date;
  confirmedDeliveryDate?: Date;
  status: PurchaseOrderStatus;
  subTotal: number;
  taxAmount: number;
  shippingCost: number;
  totalAmount: number;
  notes?: string;
  supplierReference?: string;
  createdBy: number;
  createdByName: string;
  createdAt: Date;
  updatedAt: Date;
  submittedAt?: Date;
  completedAt?: Date;
  cancelledAt?: Date;
  cancellationReason?: string;
  lines: PurchaseOrderLine[];
}

export interface PurchaseOrderLine {
  id: number;
  purchaseOrderId: number;
  productId: number;
  productName: string;
  productSku: string;
  quantityOrdered: number;
  unitPrice: number;
  lineTotal: number;
  quantityReceived: number;
  quantityOutstanding: number;
  status: LineItemStatus;
  notes?: string;
}

export interface CreatePurchaseOrderRequest {
  companyId: number;
  expectedDeliveryDate?: Date;
  taxAmount: number;
  shippingCost: number;
  notes?: string;
  supplierReference?: string;
  lines: CreatePurchaseOrderLine[];
}

export interface CreatePurchaseOrderLine {
  productId: number;
  quantityOrdered: number;
  unitPrice: number;
  notes?: string;
}

export interface UpdatePurchaseOrderRequest {
  expectedDeliveryDate?: Date;
  confirmedDeliveryDate?: Date;
  taxAmount: number;
  shippingCost: number;
  notes?: string;
  supplierReference?: string;
}

export interface PurchaseOrderFilter {
  status?: PurchaseOrderStatus;
  companyId?: number;
  fromDate?: Date;
  toDate?: Date;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ConfirmPurchaseOrderRequest {
  confirmedDeliveryDate: Date;
}

export interface CancelPurchaseOrderRequest {
  reason: string;
}
