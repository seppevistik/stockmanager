using StockManager.Core.Entities;

namespace StockManager.Core.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetByBusinessIdAsync(int businessId);
    Task<Product?> GetBySkuAsync(string sku, int businessId);
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int businessId);
}
