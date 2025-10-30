using StockManager.Core.Entities;

namespace StockManager.Core.Interfaces;

public interface IStockMovementRepository : IGenericRepository<StockMovement>
{
    Task<IEnumerable<StockMovement>> GetByProductIdAsync(int productId);
    Task<IEnumerable<StockMovement>> GetByBusinessIdAsync(int businessId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<StockMovement>> GetRecentMovementsAsync(int businessId, int count = 10);
}
