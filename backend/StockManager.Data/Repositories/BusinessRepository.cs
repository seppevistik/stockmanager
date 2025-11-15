using StockManager.Core.Entities;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class BusinessRepository : GenericRepository<Business>, IBusinessRepository
{
    public BusinessRepository(ApplicationDbContext context) : base(context)
    {
    }
}
