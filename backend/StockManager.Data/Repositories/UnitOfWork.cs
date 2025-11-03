using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IProductRepository? _products;
    private IStockMovementRepository? _stockMovements;
    private ICompanyRepository? _companies;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products => _products ??= new ProductRepository(_context);

    public IStockMovementRepository StockMovements => _stockMovements ??= new StockMovementRepository(_context);

    public ICompanyRepository Companies => _companies ??= new CompanyRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
