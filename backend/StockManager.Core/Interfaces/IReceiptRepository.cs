using StockManager.Core.Entities;
using StockManager.Core.Enums;

namespace StockManager.Core.Interfaces;

public interface IReceiptRepository : IGenericRepository<Receipt>
{
    Task<IEnumerable<Receipt>> GetByBusinessIdAsync(int businessId);
    Task<IEnumerable<Receipt>> GetByBusinessIdWithDetailsAsync(int businessId);
    Task<Receipt?> GetByIdWithDetailsAsync(int id, int businessId);
    Task<IEnumerable<Receipt>> GetByPurchaseOrderIdAsync(int purchaseOrderId, int businessId);
    Task<IEnumerable<Receipt>> GetByStatusAsync(int businessId, ReceiptStatus status);
    Task<IEnumerable<Receipt>> GetPendingValidationAsync(int businessId);
    Task<Receipt?> GetByReceiptNumberAsync(string receiptNumber, int businessId);
    Task<string> GenerateReceiptNumberAsync(int businessId);
}
