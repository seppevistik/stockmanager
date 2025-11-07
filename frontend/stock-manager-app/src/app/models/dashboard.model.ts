export interface DashboardStats {
  totalProducts: number;
  lowStockCount: number;
  outOfStockCount: number;
  totalInventoryValue: number;
}

export interface StockSummary {
  inStock: number;
  lowStock: number;
  outOfStock: number;
}

export interface RecentActivity {
  id: number;
  productName: string;
  movementType: string;
  quantity: number;
  userName: string;
  createdAt: Date;
}

export interface DailySalesData {
  date: string;
  sales: number;
  costs: number;
}
