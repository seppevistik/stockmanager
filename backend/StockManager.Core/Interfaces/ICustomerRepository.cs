using StockManager.Core.Entities;

namespace StockManager.Core.Interfaces;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<IEnumerable<Customer>> GetByBusinessIdAsync(int businessId);
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(int businessId);
    Task<Customer?> GetByNameAsync(string name, int businessId);
    Task<Customer?> GetByEmailAsync(string email, int businessId);
}
