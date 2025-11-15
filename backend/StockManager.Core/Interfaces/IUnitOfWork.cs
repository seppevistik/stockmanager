namespace StockManager.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IStockMovementRepository StockMovements { get; }
    ICompanyRepository Companies { get; }
    IPurchaseOrderRepository PurchaseOrders { get; }
    IReceiptRepository Receipts { get; }
    IBusinessRepository Businesses { get; }
    Task<int> SaveChangesAsync();
}
