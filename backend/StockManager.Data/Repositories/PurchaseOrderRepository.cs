using Microsoft.EntityFrameworkCore;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class PurchaseOrderRepository : GenericRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByBusinessIdAsync(int businessId)
    {
        return await _dbSet
            .Where(po => po.BusinessId == businessId)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByBusinessIdWithDetailsAsync(int businessId)
    {
        return await _dbSet
            .Include(po => po.Company)
            .Include(po => po.CreatedByUser)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Product)
            .Where(po => po.BusinessId == businessId)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdWithDetailsAsync(int id, int businessId)
    {
        return await _dbSet
            .Include(po => po.Company)
            .Include(po => po.CreatedByUser)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Product)
            .Include(po => po.Receipts)
                .ThenInclude(r => r.ReceivedByUser)
            .FirstOrDefaultAsync(po => po.Id == id && po.BusinessId == businessId);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(int businessId, PurchaseOrderStatus status)
    {
        return await _dbSet
            .Include(po => po.Company)
            .Where(po => po.BusinessId == businessId && po.Status == status)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> GetOutstandingOrdersAsync(int businessId)
    {
        var outstandingStatuses = new[]
        {
            PurchaseOrderStatus.Submitted,
            PurchaseOrderStatus.Confirmed,
            PurchaseOrderStatus.Receiving,
            PurchaseOrderStatus.PartiallyReceived
        };

        return await _dbSet
            .Include(po => po.Company)
            .Include(po => po.Lines)
            .Where(po => po.BusinessId == businessId && outstandingStatuses.Contains(po.Status))
            .OrderBy(po => po.ExpectedDeliveryDate)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, int businessId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(po => po.OrderNumber == orderNumber && po.BusinessId == businessId);
    }

    public async Task<string> GenerateOrderNumberAsync(int businessId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PO-{year}-";

        var lastOrder = await _dbSet
            .Where(po => po.BusinessId == businessId && po.OrderNumber.StartsWith(prefix))
            .OrderByDescending(po => po.OrderNumber)
            .FirstOrDefaultAsync();

        if (lastOrder == null)
        {
            return $"{prefix}0001";
        }

        var lastNumber = lastOrder.OrderNumber.Replace(prefix, "");
        if (int.TryParse(lastNumber, out var number))
        {
            return $"{prefix}{(number + 1):D4}";
        }

        return $"{prefix}0001";
    }

    public async Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(int businessId, int companyId)
    {
        return await _dbSet
            .Include(po => po.Company)
            .Include(po => po.Lines)
            .Where(po => po.BusinessId == businessId && po.CompanyId == companyId)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> SearchAsync(int businessId, string searchTerm)
    {
        var lowerSearch = searchTerm.ToLower();

        return await _dbSet
            .Include(po => po.Company)
            .Where(po => po.BusinessId == businessId &&
                        (po.OrderNumber.ToLower().Contains(lowerSearch) ||
                         po.Company.Name.ToLower().Contains(lowerSearch) ||
                         (po.SupplierReference != null && po.SupplierReference.ToLower().Contains(lowerSearch))))
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();
    }
}
