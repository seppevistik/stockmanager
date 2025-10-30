using Microsoft.EntityFrameworkCore;
using StockManager.Core.Entities;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class StockMovementRepository : GenericRepository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<StockMovement>> GetByProductIdAsync(int productId)
    {
        return await _dbSet
            .Include(sm => sm.User)
            .Where(sm => sm.ProductId == productId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockMovement>> GetByBusinessIdAsync(int businessId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbSet
            .Include(sm => sm.Product)
            .Include(sm => sm.User)
            .Where(sm => sm.Product.BusinessId == businessId);

        if (startDate.HasValue)
            query = query.Where(sm => sm.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(sm => sm.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockMovement>> GetRecentMovementsAsync(int businessId, int count = 10)
    {
        return await _dbSet
            .Include(sm => sm.Product)
            .Include(sm => sm.User)
            .Where(sm => sm.Product.BusinessId == businessId)
            .OrderByDescending(sm => sm.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
