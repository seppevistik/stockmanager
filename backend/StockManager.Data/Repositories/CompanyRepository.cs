using Microsoft.EntityFrameworkCore;
using StockManager.Core.Entities;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
{
    public CompanyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Company>> GetByBusinessIdAsync(int businessId)
    {
        return await _context.Companies
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Company>> GetSuppliersAsync(int businessId)
    {
        return await _context.Companies
            .Where(c => c.BusinessId == businessId && c.IsSupplier)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Company>> GetCustomersAsync(int businessId)
    {
        return await _context.Companies
            .Where(c => c.BusinessId == businessId && c.IsCustomer)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Company?> GetByNameAsync(string name, int businessId)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Name == name && c.BusinessId == businessId);
    }
}
