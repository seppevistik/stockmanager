using StockManager.Core.Entities;
using StockManager.Core.Enums;

namespace StockManager.Core.Interfaces;

public interface IPurchaseOrderRepository : IGenericRepository<PurchaseOrder>
{
    Task<IEnumerable<PurchaseOrder>> GetByBusinessIdAsync(int businessId);
    Task<IEnumerable<PurchaseOrder>> GetByBusinessIdWithDetailsAsync(int businessId);
    Task<PurchaseOrder?> GetByIdWithDetailsAsync(int id, int businessId);
    Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(int businessId, PurchaseOrderStatus status);
    Task<IEnumerable<PurchaseOrder>> GetOutstandingOrdersAsync(int businessId);
    Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, int businessId);
    Task<string> GenerateOrderNumberAsync(int businessId);
    Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(int businessId, int companyId);
    Task<IEnumerable<PurchaseOrder>> SearchAsync(int businessId, string searchTerm);
}
