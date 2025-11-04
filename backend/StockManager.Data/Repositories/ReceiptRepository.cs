using Microsoft.EntityFrameworkCore;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;
using StockManager.Data.Contexts;

namespace StockManager.Data.Repositories;

public class ReceiptRepository : GenericRepository<Receipt>, IReceiptRepository
{
    public ReceiptRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Receipt>> GetByBusinessIdAsync(int businessId)
    {
        return await _dbSet
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Receipt>> GetByBusinessIdWithDetailsAsync(int businessId)
    {
        return await _dbSet
            .Include(r => r.PurchaseOrder)
                .ThenInclude(po => po.Company)
            .Include(r => r.ReceivedByUser)
            .Include(r => r.ValidatedByUser)
            .Include(r => r.Lines)
                .ThenInclude(l => l.Product)
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Receipt?> GetByIdWithDetailsAsync(int id, int businessId)
    {
        return await _dbSet
            .Include(r => r.PurchaseOrder)
                .ThenInclude(po => po.Company)
            .Include(r => r.PurchaseOrder)
                .ThenInclude(po => po.Lines)
                    .ThenInclude(l => l.Product)
            .Include(r => r.ReceivedByUser)
            .Include(r => r.ValidatedByUser)
            .Include(r => r.Lines)
                .ThenInclude(l => l.Product)
            .Include(r => r.Lines)
                .ThenInclude(l => l.PurchaseOrderLine)
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId);
    }

    public async Task<IEnumerable<Receipt>> GetByPurchaseOrderIdAsync(int purchaseOrderId, int businessId)
    {
        return await _dbSet
            .Include(r => r.ReceivedByUser)
            .Include(r => r.ValidatedByUser)
            .Include(r => r.Lines)
                .ThenInclude(l => l.Product)
            .Where(r => r.PurchaseOrderId == purchaseOrderId && r.BusinessId == businessId)
            .OrderByDescending(r => r.ReceiptDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Receipt>> GetByStatusAsync(int businessId, ReceiptStatus status)
    {
        return await _dbSet
            .Include(r => r.PurchaseOrder)
                .ThenInclude(po => po.Company)
            .Include(r => r.ReceivedByUser)
            .Where(r => r.BusinessId == businessId && r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Receipt>> GetPendingValidationAsync(int businessId)
    {
        return await _dbSet
            .Include(r => r.PurchaseOrder)
                .ThenInclude(po => po.Company)
            .Include(r => r.ReceivedByUser)
            .Include(r => r.Lines)
                .ThenInclude(l => l.Product)
            .Where(r => r.BusinessId == businessId && r.Status == ReceiptStatus.PendingValidation)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Receipt?> GetByReceiptNumberAsync(string receiptNumber, int businessId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.ReceiptNumber == receiptNumber && r.BusinessId == businessId);
    }

    public async Task<string> GenerateReceiptNumberAsync(int businessId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"REC-{year}-";

        var lastReceipt = await _dbSet
            .Where(r => r.BusinessId == businessId && r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .FirstOrDefaultAsync();

        if (lastReceipt == null)
        {
            return $"{prefix}0001";
        }

        var lastNumber = lastReceipt.ReceiptNumber.Replace(prefix, "");
        if (int.TryParse(lastNumber, out var number))
        {
            return $"{prefix}{(number + 1):D4}";
        }

        return $"{prefix}0001";
    }
}
