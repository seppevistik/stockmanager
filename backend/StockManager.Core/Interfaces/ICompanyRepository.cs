using StockManager.Core.Entities;

namespace StockManager.Core.Interfaces;

public interface ICompanyRepository : IGenericRepository<Company>
{
    Task<IEnumerable<Company>> GetByBusinessIdAsync(int businessId);
    Task<IEnumerable<Company>> GetSuppliersAsync(int businessId);
    Task<IEnumerable<Company>> GetCustomersAsync(int businessId);
    Task<Company?> GetByNameAsync(string name, int businessId);
}
