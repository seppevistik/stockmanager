export enum StockMovementType {
  StockIn = 'StockIn',
  StockOut = 'StockOut',
  StockTransfer = 'StockTransfer',
  StockAdjustment = 'StockAdjustment'
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
