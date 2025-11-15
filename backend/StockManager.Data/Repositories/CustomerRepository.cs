using Microsoft.EntityFrameworkCore;
using StockManager.Core.Entities;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Customer>> GetByBusinessIdAsync(int businessId)
    {
        return await _context.Customers
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(int businessId)
    {
        return await _context.Customers
            .Where(c => c.BusinessId == businessId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Customer?> GetByNameAsync(string name, int businessId)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Name == name && c.BusinessId == businessId);
    }

    public async Task<Customer?> GetByEmailAsync(string email, int businessId)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == email && c.BusinessId == businessId);
    }
}
