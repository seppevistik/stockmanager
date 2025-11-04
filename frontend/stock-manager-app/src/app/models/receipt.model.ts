export enum ReceiptStatus {
  InProgress = 'InProgress',
  PendingValidation = 'PendingValidation',
  Validated = 'Validated',
  Completed = 'Completed',
  Rejected = 'Rejected'
}

export enum ItemCondition {
  Good = 'Good',
  Damaged = 'Damaged',
  Defective = 'Defective'
}

export interface Receipt {
  id: number;
  businessId: number;
  purchaseOrderId: number;
  purchaseOrderNumber: string;
  companyName: string;
  receiptNumber: string;
  receiptDate: Date;
  receivedBy: number;
  receivedByName: string;
  status: ReceiptStatus;
  supplierDeliveryNote?: string;
  notes?: string;
  hasVariances: boolean;
  varianceNotes?: string;
  validatedBy?: number;
  validatedByName?: string;
  validatedAt?: Date;
  completedAt?: Date;
  createdAt: Date;
  updatedAt: Date;
  lines: ReceiptLine[];
}

export interface ReceiptLine {
  id: number;
  receiptId: number;
  purchaseOrderLineId: number;
  productId: number;
  productName: string;
  productSku: string;
  quantityOrdered: number;
  quantityReceived: number;
  quantityVariance: number;
  unitPriceOrdered: number;
  unitPriceReceived?: number;
  priceVariance: number;
  condition: ItemCondition;
  damageNotes?: string;
  location?: string;
  batchNumber?: string;
  expiryDate?: Date;
}

export interface CreateReceiptRequest {
  purchaseOrderId: number;
  receiptDate: Date;
  supplierDeliveryNote?: string;
  notes?: string;
  lines: CreateReceiptLine[];
}

export interface CreateReceiptLine {
  purchaseOrderLineId: number;
  quantityReceived: number;
  unitPriceReceived?: number;
  condition: ItemCondition;
  damageNotes?: string;
  location?: string;
  batchNumber?: string;
  expiryDate?: Date;
}

export interface UpdateReceiptRequest {
  receiptDate: Date;
  supplierDeliveryNote?: string;
  notes?: string;
  lines: CreateReceiptLine[];
}

export interface ReceiptValidation {
  receiptId: number;
  hasVariances: boolean;
  variances: Variance[];
}

export interface Variance {
  productId: number;
  productName: string;
  quantityOrdered: number;
  quantityReceived: number;
  quantityVariance: number;
  priceVariance?: number;
  condition: ItemCondition;
}

export interface ApproveReceiptRequest {
  varianceNotes?: string;
}

export interface RejectReceiptRequest {
  reason: string;
}
