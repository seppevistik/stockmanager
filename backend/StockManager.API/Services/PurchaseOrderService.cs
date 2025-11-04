using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class PurchaseOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseOrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public virtual async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersAsync(int businessId, PurchaseOrderFilterDto? filter = null)
    {
        IEnumerable<PurchaseOrder> purchaseOrders;

        if (filter?.Status != null)
        {
            purchaseOrders = await _unitOfWork.PurchaseOrders.GetByStatusAsync(businessId, filter.Status.Value);
        }
        else if (filter?.CompanyId != null)
        {
            purchaseOrders = await _unitOfWork.PurchaseOrders.GetBySupplierAsync(businessId, filter.CompanyId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            purchaseOrders = await _unitOfWork.PurchaseOrders.SearchAsync(businessId, filter.Search);
        }
        else
        {
            purchaseOrders = await _unitOfWork.PurchaseOrders.GetByBusinessIdWithDetailsAsync(businessId);
        }

        return purchaseOrders.Select(po => MapToDto(po));
    }

    public virtual async Task<IEnumerable<PurchaseOrderDto>> GetOutstandingOrdersAsync(int businessId)
    {
        var orders = await _unitOfWork.PurchaseOrders.GetOutstandingOrdersAsync(businessId);
        return orders.Select(po => MapToDto(po));
    }

    public virtual async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id, int businessId)
    {
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdWithDetailsAsync(id, businessId);
        if (purchaseOrder == null)
            return null;

        return MapToDto(purchaseOrder);
    }

    public virtual async Task<(bool Success, string? Error, PurchaseOrderDto? PurchaseOrder)> CreatePurchaseOrderAsync(
        CreatePurchaseOrderDto createDto, int businessId, int userId)
    {
        // Validate company is a supplier
        var company = await _unitOfWork.Companies.GetByIdAsync(createDto.CompanyId);
        if (company == null || company.BusinessId != businessId)
        {
            return (false, "Supplier not found", null);
        }
        if (!company.IsSupplier)
        {
            return (false, "Selected company is not marked as a supplier", null);
        }

        // Validate line items
        if (createDto.Lines == null || !createDto.Lines.Any())
        {
            return (false, "Purchase order must have at least one line item", null);
        }

        // Generate order number
        var orderNumber = await _unitOfWork.PurchaseOrders.GenerateOrderNumberAsync(businessId);

        // Calculate totals
        decimal subTotal = 0;
        var lines = new List<PurchaseOrderLine>();

        foreach (var lineDto in createDto.Lines)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(lineDto.ProductId);
            if (product == null || product.BusinessId != businessId)
            {
                return (false, $"Product {lineDto.ProductId} not found", null);
            }

            var lineTotal = lineDto.QuantityOrdered * lineDto.UnitPrice;
            subTotal += lineTotal;

            var line = new PurchaseOrderLine
            {
                ProductId = lineDto.ProductId,
                ProductName = product.Name,
                ProductSku = product.SKU,
                QuantityOrdered = lineDto.QuantityOrdered,
                UnitPrice = lineDto.UnitPrice,
                LineTotal = lineTotal,
                QuantityReceived = 0,
                QuantityOutstanding = lineDto.QuantityOrdered,
                Status = LineItemStatus.Pending,
                Notes = lineDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            lines.Add(line);
        }

        var totalAmount = subTotal + createDto.TaxAmount + createDto.ShippingCost;

        var purchaseOrder = new PurchaseOrder
        {
            BusinessId = businessId,
            CompanyId = createDto.CompanyId,
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = createDto.ExpectedDeliveryDate,
            Status = PurchaseOrderStatus.Draft,
            SubTotal = subTotal,
            TaxAmount = createDto.TaxAmount,
            ShippingCost = createDto.ShippingCost,
            TotalAmount = totalAmount,
            Notes = createDto.Notes,
            SupplierReference = createDto.SupplierReference,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = lines
        };

        await _unitOfWork.PurchaseOrders.AddAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        var dto = await GetPurchaseOrderByIdAsync(purchaseOrder.Id, businessId);
        return (true, null, dto);
    }

    public virtual async Task<(bool Success, string? Error)> UpdatePurchaseOrderAsync(
        int id, UpdatePurchaseOrderDto updateDto, int businessId)
    {
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdAsync(id);
        if (purchaseOrder == null || purchaseOrder.BusinessId != businessId)
        {
            return (false, "Purchase order not found");
        }

        // Can only update draft orders completely
        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            // For submitted orders, only allow certain fields
            purchaseOrder.ExpectedDeliveryDate = updateDto.ExpectedDeliveryDate;
            purchaseOrder.ConfirmedDeliveryDate = updateDto.ConfirmedDeliveryDate;
            purchaseOrder.Notes = updateDto.Notes;
            purchaseOrder.SupplierReference = updateDto.SupplierReference;
        }
        else
        {
            purchaseOrder.ExpectedDeliveryDate = updateDto.ExpectedDeliveryDate;
            purchaseOrder.TaxAmount = updateDto.TaxAmount;
            purchaseOrder.ShippingCost = updateDto.ShippingCost;
            purchaseOrder.Notes = updateDto.Notes;
            purchaseOrder.SupplierReference = updateDto.SupplierReference;

            // Recalculate total
            purchaseOrder.TotalAmount = purchaseOrder.SubTotal + updateDto.TaxAmount + updateDto.ShippingCost;
        }

        purchaseOrder.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> SubmitPurchaseOrderAsync(int id, int businessId, int userId)
    {
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdAsync(id);
        if (purchaseOrder == null || purchaseOrder.BusinessId != businessId)
        {
            return (false, "Purchase order not found");
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Only draft purchase orders can be submitted");
        }

        purchaseOrder.Status = PurchaseOrderStatus.Submitted;
        purchaseOrder.SubmittedAt = DateTime.UtcNow;
        purchaseOrder.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> ConfirmPurchaseOrderAsync(
        int id, DateTime confirmedDeliveryDate, int businessId)
    {
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdAsync(id);
        if (purchaseOrder == null || purchaseOrder.BusinessId != businessId)
        {
            return (false, "Purchase order not found");
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.Submitted)
        {
            return (false, "Only submitted purchase orders can be confirmed");
        }

        purchaseOrder.Status = PurchaseOrderStatus.Confirmed;
        purchaseOrder.ConfirmedDeliveryDate = confirmedDeliveryDate;
        purchaseOrder.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> CancelPurchaseOrderAsync(
        int id, string reason, int businessId, int userId)
    {
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdWithDetailsAsync(id, businessId);
        if (purchaseOrder == null)
        {
            return (false, "Purchase order not found");
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Completed)
        {
            return (false, "Cannot cancel a completed purchase order");
        }

        // Check if any items have been received
        if (purchaseOrder.Lines.Any(l => l.QuantityReceived > 0))
        {
            return (false, "Cannot cancel purchase order with received items");
        }

        purchaseOrder.Status = PurchaseOrderStatus.Cancelled;
        purchaseOrder.CancelledAt = DateTime.UtcNow;
        purchaseOrder.CancellationReason = reason;
        purchaseOrder.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> DeletePurchaseOrderAsync(int id, int businessId)
    {
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdAsync(id);
        if (purchaseOrder == null || purchaseOrder.BusinessId != businessId)
        {
            return (false, "Purchase order not found");
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            return (false, "Only draft purchase orders can be deleted");
        }

        await _unitOfWork.PurchaseOrders.DeleteAsync(purchaseOrder);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    private PurchaseOrderDto MapToDto(PurchaseOrder po)
    {
        return new PurchaseOrderDto
        {
            Id = po.Id,
            BusinessId = po.BusinessId,
            CompanyId = po.CompanyId,
            CompanyName = po.Company?.Name ?? "",
            OrderNumber = po.OrderNumber,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            ConfirmedDeliveryDate = po.ConfirmedDeliveryDate,
            Status = po.Status,
            SubTotal = po.SubTotal,
            TaxAmount = po.TaxAmount,
            ShippingCost = po.ShippingCost,
            TotalAmount = po.TotalAmount,
            Notes = po.Notes,
            SupplierReference = po.SupplierReference,
            CreatedBy = po.CreatedBy,
            CreatedByName = po.CreatedByUser?.UserName ?? "",
            CreatedAt = po.CreatedAt,
            UpdatedAt = po.UpdatedAt,
            SubmittedAt = po.SubmittedAt,
            CompletedAt = po.CompletedAt,
            CancelledAt = po.CancelledAt,
            CancellationReason = po.CancellationReason,
            Lines = po.Lines?.Select(l => new PurchaseOrderLineDto
            {
                Id = l.Id,
                PurchaseOrderId = l.PurchaseOrderId,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductSku = l.ProductSku,
                QuantityOrdered = l.QuantityOrdered,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                QuantityReceived = l.QuantityReceived,
                QuantityOutstanding = l.QuantityOutstanding,
                Status = l.Status,
                Notes = l.Notes
            }).ToList() ?? new List<PurchaseOrderLineDto>()
        };
    }
}
