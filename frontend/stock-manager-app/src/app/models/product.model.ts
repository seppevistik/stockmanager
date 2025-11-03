export enum ProductStatus {
  Active = 'Active',
  Inactive = 'Inactive'
}

export interface ProductSupplier {
  id: number;
  productId: number;
  companyId: number;
  companyName: string;
  supplierPrice?: number;
  supplierProductCode?: string;
  leadTimeDays?: number;
  minimumOrderQuantity?: number;
  isPrimarySupplier: boolean;
}

export interface Product {
  id: number;
  name: string;
  description?: string;
  sku: string;
  categoryId?: number;
  categoryName?: string;
  unitOfMeasurement: string;
  imageUrl?: string;
  supplier?: string;
  minimumStockLevel: number;
  currentStock: number;
  costPerUnit: number;
  totalValue: number;
  status: ProductStatus;
  location?: string;
  createdAt: Date;
  suppliers: ProductSupplier[];
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  sku: string;
  categoryId?: number;
  unitOfMeasurement: string;
  imageUrl?: string;
  supplier?: string;
  minimumStockLevel: number;
  initialStock: number;
  costPerUnit: number;
  location?: string;
}

export interface StockAdjustment {
  productId: number;
  newStock: number;
}

export interface BulkStockAdjustment {
  adjustments: StockAdjustment[];
  reason: string;
}

export interface CreateProductSupplier {
  companyId: number;
  supplierPrice?: number;
  supplierProductCode?: string;
  leadTimeDays?: number;
  minimumOrderQuantity?: number;
  isPrimarySupplier: boolean;
}
