using Microsoft.EntityFrameworkCore;
using StockManager.Core.Entities;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetByBusinessIdAsync(int businessId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.ProductSuppliers)
                .ThenInclude(ps => ps.Company)
            .Where(p => p.BusinessId == businessId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetBySkuAsync(string sku, int businessId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.ProductSuppliers)
                .ThenInclude(ps => ps.Company)
            .FirstOrDefaultAsync(p => p.SKU == sku && p.BusinessId == businessId);
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int businessId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.BusinessId == businessId && p.CurrentStock <= p.MinimumStockLevel)
            .OrderBy(p => p.CurrentStock)
            .ToListAsync();
    }
}
