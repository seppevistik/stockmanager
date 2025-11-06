export enum StockMovementType {
  StockIn = 0,
  StockOut = 1,
  StockTransfer = 2,
  StockAdjustment = 3
}

export interface StockMovement {
  id: number;
  productId: number;
  productName: string;
  productSKU: string;
  movementType: StockMovementType;
  quantity: number;
  previousStock: number;
  newStock: number;
  reason?: string;
  notes?: string;
  fromLocation?: string;
  toLocation?: string;
  userName: string;
  createdAt: Date;
}

export interface CreateStockMovementRequest {
  productId: number;
  movementType: StockMovementType;
  quantity: number;
  reason?: string;
  notes?: string;
  fromLocation?: string;
  toLocation?: string;
}
