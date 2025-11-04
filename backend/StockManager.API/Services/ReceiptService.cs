using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class ReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventoryUpdateService _inventoryUpdateService;
    private const decimal VarianceTolerancePercentage = 5.0m; // 5% tolerance

    public ReceiptService(IUnitOfWork unitOfWork, InventoryUpdateService inventoryUpdateService)
    {
        _unitOfWork = unitOfWork;
        _inventoryUpdateService = inventoryUpdateService;
    }

    public virtual async Task<IEnumerable<ReceiptDto>> GetReceiptsAsync(int businessId)
    {
        var receipts = await _unitOfWork.Receipts.GetByBusinessIdWithDetailsAsync(businessId);
        return receipts.Select(r => MapToDto(r));
    }

    public virtual async Task<IEnumerable<ReceiptDto>> GetReceiptsForPurchaseOrderAsync(int purchaseOrderId, int businessId)
    {
        var receipts = await _unitOfWork.Receipts.GetByPurchaseOrderIdAsync(purchaseOrderId, businessId);
        return receipts.Select(r => MapToDto(r));
    }

    public virtual async Task<IEnumerable<ReceiptDto>> GetPendingValidationAsync(int businessId)
    {
        var receipts = await _unitOfWork.Receipts.GetPendingValidationAsync(businessId);
        return receipts.Select(r => MapToDto(r));
    }

    public virtual async Task<ReceiptDto?> GetReceiptByIdAsync(int id, int businessId)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdWithDetailsAsync(id, businessId);
        if (receipt == null)
            return null;

        return MapToDto(receipt);
    }

    public virtual async Task<(bool Success, string? Error, ReceiptDto? Receipt)> CreateReceiptAsync(
        CreateReceiptDto createDto, int businessId, int userId)
    {
        // Validate purchase order
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdWithDetailsAsync(createDto.PurchaseOrderId, businessId);
        if (purchaseOrder == null)
        {
            return (false, "Purchase order not found", null);
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Draft)
        {
            return (false, "Cannot receive goods for draft purchase orders", null);
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Completed || purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
        {
            return (false, "Purchase order is already completed or cancelled", null);
        }

        // Validate line items
        if (createDto.Lines == null || !createDto.Lines.Any())
        {
            return (false, "Receipt must have at least one line item", null);
        }

        // Generate receipt number
        var receiptNumber = await _unitOfWork.Receipts.GenerateReceiptNumberAsync(businessId);

        var lines = new List<ReceiptLine>();
        bool hasVariances = false;

        foreach (var lineDto in createDto.Lines)
        {
            var poLine = purchaseOrder.Lines.FirstOrDefault(l => l.Id == lineDto.PurchaseOrderLineId);
            if (poLine == null)
            {
                return (false, $"Purchase order line {lineDto.PurchaseOrderLineId} not found", null);
            }

            if (lineDto.QuantityReceived < 0)
            {
                return (false, "Received quantity cannot be negative", null);
            }

            // Calculate variances
            var quantityVariance = lineDto.QuantityReceived - poLine.QuantityOutstanding;
            var priceReceived = lineDto.UnitPriceReceived ?? poLine.UnitPrice;
            var priceVariance = priceReceived - poLine.UnitPrice;

            // Check if variance exceeds tolerance
            var variancePercentage = poLine.QuantityOrdered > 0
                ? Math.Abs(quantityVariance / poLine.QuantityOrdered * 100)
                : 0;

            if (variancePercentage > VarianceTolerancePercentage ||
                lineDto.Condition != ItemCondition.Good ||
                Math.Abs(priceVariance) > 0.01m)
            {
                hasVariances = true;
            }

            var line = new ReceiptLine
            {
                PurchaseOrderLineId = lineDto.PurchaseOrderLineId,
                ProductId = poLine.ProductId,
                QuantityOrdered = poLine.QuantityOrdered,
                QuantityReceived = lineDto.QuantityReceived,
                QuantityVariance = quantityVariance,
                UnitPriceOrdered = poLine.UnitPrice,
                UnitPriceReceived = lineDto.UnitPriceReceived,
                PriceVariance = priceVariance,
                Condition = lineDto.Condition,
                DamageNotes = lineDto.DamageNotes,
                Location = lineDto.Location,
                BatchNumber = lineDto.BatchNumber,
                ExpiryDate = lineDto.ExpiryDate,
                CreatedAt = DateTime.UtcNow
            };

            lines.Add(line);
        }

        // Determine initial status
        var initialStatus = hasVariances ? ReceiptStatus.PendingValidation : ReceiptStatus.Validated;

        var receipt = new Receipt
        {
            BusinessId = businessId,
            PurchaseOrderId = createDto.PurchaseOrderId,
            ReceiptNumber = receiptNumber,
            ReceiptDate = createDto.ReceiptDate,
            ReceivedBy = userId,
            Status = initialStatus,
            SupplierDeliveryNote = createDto.SupplierDeliveryNote,
            Notes = createDto.Notes,
            HasVariances = hasVariances,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = lines
        };

        await _unitOfWork.Receipts.AddAsync(receipt);

        // Update PO status
        if (purchaseOrder.Status == PurchaseOrderStatus.Submitted ||
            purchaseOrder.Status == PurchaseOrderStatus.Confirmed)
        {
            purchaseOrder.Status = PurchaseOrderStatus.Receiving;
            purchaseOrder.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);
        }

        await _unitOfWork.SaveChangesAsync();

        var dto = await GetReceiptByIdAsync(receipt.Id, businessId);
        return (true, null, dto);
    }

    public virtual async Task<(bool Success, string? Error)> UpdateReceiptAsync(
        int id, UpdateReceiptDto updateDto, int businessId)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdWithDetailsAsync(id, businessId);
        if (receipt == null)
        {
            return (false, "Receipt not found");
        }

        if (receipt.Status != ReceiptStatus.InProgress)
        {
            return (false, "Only receipts in progress can be updated");
        }

        receipt.ReceiptDate = updateDto.ReceiptDate;
        receipt.SupplierDeliveryNote = updateDto.SupplierDeliveryNote;
        receipt.Notes = updateDto.Notes;
        receipt.UpdatedAt = DateTime.UtcNow;

        // Update lines if provided
        if (updateDto.Lines != null && updateDto.Lines.Any())
        {
            // Remove existing lines
            foreach (var line in receipt.Lines.ToList())
            {
                await _unitOfWork.Receipts.DeleteAsync(receipt); // This will cascade delete lines
            }

            // Add new lines (simplified - in real implementation, you'd handle this more carefully)
            // For now, assume user will recreate receipt if major changes needed
        }

        await _unitOfWork.Receipts.UpdateAsync(receipt);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<ReceiptValidationDto> ValidateReceiptAsync(int id, int businessId)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdWithDetailsAsync(id, businessId);
        if (receipt == null)
        {
            throw new InvalidOperationException("Receipt not found");
        }

        var variances = new List<VarianceDto>();

        foreach (var line in receipt.Lines)
        {
            if (line.QuantityVariance != 0 || line.PriceVariance != 0 || line.Condition != ItemCondition.Good)
            {
                variances.Add(new VarianceDto
                {
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name ?? "",
                    QuantityOrdered = line.QuantityOrdered,
                    QuantityReceived = line.QuantityReceived,
                    QuantityVariance = line.QuantityVariance,
                    PriceVariance = line.PriceVariance,
                    Condition = line.Condition
                });
            }
        }

        return new ReceiptValidationDto
        {
            ReceiptId = id,
            HasVariances = variances.Any(),
            Variances = variances
        };
    }

    public virtual async Task<(bool Success, string? Error)> ApproveReceiptAsync(
        int id, ApproveReceiptDto approveDto, int businessId, int userId)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdWithDetailsAsync(id, businessId);
        if (receipt == null)
        {
            return (false, "Receipt not found");
        }

        if (receipt.Status != ReceiptStatus.PendingValidation && receipt.Status != ReceiptStatus.Validated)
        {
            return (false, "Receipt cannot be approved in current status");
        }

        receipt.Status = ReceiptStatus.Validated;
        receipt.VarianceNotes = approveDto.VarianceNotes;
        receipt.ValidatedBy = userId;
        receipt.ValidatedAt = DateTime.UtcNow;
        receipt.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Receipts.UpdateAsync(receipt);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> RejectReceiptAsync(
        int id, RejectReceiptDto rejectDto, int businessId, int userId)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdAsync(id);
        if (receipt == null || receipt.BusinessId != businessId)
        {
            return (false, "Receipt not found");
        }

        if (receipt.Status != ReceiptStatus.PendingValidation)
        {
            return (false, "Only receipts pending validation can be rejected");
        }

        receipt.Status = ReceiptStatus.Rejected;
        receipt.Notes = $"{receipt.Notes}\n\nREJECTED: {rejectDto.Reason}";
        receipt.ValidatedBy = userId;
        receipt.ValidatedAt = DateTime.UtcNow;
        receipt.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Receipts.UpdateAsync(receipt);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> CompleteReceiptAsync(int id, int businessId, int userId)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdWithDetailsAsync(id, businessId);
        if (receipt == null)
        {
            return (false, "Receipt not found");
        }

        if (receipt.Status != ReceiptStatus.Validated)
        {
            return (false, "Only validated receipts can be completed");
        }

        // Apply to inventory
        var (success, error) = await _inventoryUpdateService.ApplyReceiptToInventoryAsync(receipt, userId);
        if (!success)
        {
            return (false, error);
        }

        receipt.Status = ReceiptStatus.Completed;
        receipt.CompletedAt = DateTime.UtcNow;
        receipt.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Receipts.UpdateAsync(receipt);

        // Update purchase order lines
        var purchaseOrder = await _unitOfWork.PurchaseOrders.GetByIdWithDetailsAsync(receipt.PurchaseOrderId, businessId);
        if (purchaseOrder != null)
        {
            foreach (var receiptLine in receipt.Lines)
            {
                var poLine = purchaseOrder.Lines.FirstOrDefault(l => l.Id == receiptLine.PurchaseOrderLineId);
                if (poLine != null)
                {
                    poLine.QuantityReceived += receiptLine.QuantityReceived;
                    poLine.QuantityOutstanding = poLine.QuantityOrdered - poLine.QuantityReceived;

                    if (poLine.QuantityOutstanding <= 0)
                    {
                        poLine.Status = LineItemStatus.FullyReceived;
                    }
                    else if (poLine.QuantityReceived > 0)
                    {
                        poLine.Status = LineItemStatus.PartiallyReceived;
                    }

                    poLine.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);
                }
            }

            // Update PO status
            var allLinesReceived = purchaseOrder.Lines.All(l => l.QuantityOutstanding <= 0);
            var anyLinesReceived = purchaseOrder.Lines.Any(l => l.QuantityReceived > 0);

            if (allLinesReceived)
            {
                purchaseOrder.Status = PurchaseOrderStatus.Completed;
                purchaseOrder.CompletedAt = DateTime.UtcNow;
            }
            else if (anyLinesReceived)
            {
                purchaseOrder.Status = PurchaseOrderStatus.PartiallyReceived;
            }

            purchaseOrder.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.PurchaseOrders.UpdateAsync(purchaseOrder);
        }

        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> DeleteReceiptAsync(int id, int businessId)
    {
        var receipt = await _unitOfWork.Receipts.GetByIdAsync(id);
        if (receipt == null || receipt.BusinessId != businessId)
        {
            return (false, "Receipt not found");
        }

        if (receipt.Status == ReceiptStatus.Completed)
        {
            return (false, "Cannot delete completed receipts");
        }

        await _unitOfWork.Receipts.DeleteAsync(receipt);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    private ReceiptDto MapToDto(Receipt r)
    {
        return new ReceiptDto
        {
            Id = r.Id,
            BusinessId = r.BusinessId,
            PurchaseOrderId = r.PurchaseOrderId,
            PurchaseOrderNumber = r.PurchaseOrder?.OrderNumber ?? "",
            CompanyName = r.PurchaseOrder?.Company?.Name ?? "",
            ReceiptNumber = r.ReceiptNumber,
            ReceiptDate = r.ReceiptDate,
            ReceivedBy = r.ReceivedBy,
            ReceivedByName = r.ReceivedByUser?.UserName ?? "",
            Status = r.Status,
            SupplierDeliveryNote = r.SupplierDeliveryNote,
            Notes = r.Notes,
            HasVariances = r.HasVariances,
            VarianceNotes = r.VarianceNotes,
            ValidatedBy = r.ValidatedBy,
            ValidatedByName = r.ValidatedByUser?.UserName ?? "",
            ValidatedAt = r.ValidatedAt,
            CompletedAt = r.CompletedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            Lines = r.Lines?.Select(l => new ReceiptLineDto
            {
                Id = l.Id,
                ReceiptId = l.ReceiptId,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ProductId = l.ProductId,
                ProductName = l.Product?.Name ?? l.PurchaseOrderLine?.ProductName ?? "",
                ProductSku = l.Product?.SKU ?? l.PurchaseOrderLine?.ProductSku ?? "",
                QuantityOrdered = l.QuantityOrdered,
                QuantityReceived = l.QuantityReceived,
                QuantityVariance = l.QuantityVariance,
                UnitPriceOrdered = l.UnitPriceOrdered,
                UnitPriceReceived = l.UnitPriceReceived,
                PriceVariance = l.PriceVariance,
                Condition = l.Condition,
                DamageNotes = l.DamageNotes,
                Location = l.Location,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate
            }).ToList() ?? new List<ReceiptLineDto>()
        };
    }
}
