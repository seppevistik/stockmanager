using StockManager.Core.Entities;

namespace StockManager.Core.Interfaces;

public interface IBusinessRepository : IGenericRepository<Business>
{
    // No additional methods needed beyond the generic repository for now
}
