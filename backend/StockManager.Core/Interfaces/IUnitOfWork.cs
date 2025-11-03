namespace StockManager.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IStockMovementRepository StockMovements { get; }
    ICompanyRepository Companies { get; }
    Task<int> SaveChangesAsync();
}
