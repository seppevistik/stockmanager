using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class InventoryUpdateService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryUpdateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public virtual async Task<(bool Success, string? Error)> ApplyReceiptToInventoryAsync(Receipt receipt, string userId)
    {
        try
        {
            foreach (var line in receipt.Lines)
            {
                // Only update for items in good condition
                if (line.Condition != ItemCondition.Good)
                {
                    continue;
                }

                var product = await _unitOfWork.Products.GetByIdAsync(line.ProductId);
                if (product == null)
                {
                    return (false, $"Product {line.ProductId} not found");
                }

                var previousStock = product.CurrentStock;
                var newStock = previousStock + line.QuantityReceived;

                // Update product stock
                product.CurrentStock = newStock;
                product.UpdatedAt = DateTime.UtcNow;

                // Optionally update cost per unit (using weighted average)
                // You can make this configurable based on business rules
                if (line.UnitPriceReceived.HasValue && line.UnitPriceReceived.Value > 0)
                {
                    var totalValue = (previousStock * product.CostPerUnit) + (line.QuantityReceived * line.UnitPriceReceived.Value);
                    var totalQuantity = newStock;
                    if (totalQuantity > 0)
                    {
                        product.CostPerUnit = totalValue / totalQuantity;
                    }
                }

                await _unitOfWork.Products.UpdateAsync(product);

                // Create stock movement record
                var stockMovement = new StockMovement
                {
                    ProductId = line.ProductId,
                    MovementType = StockMovementType.StockIn,
                    Quantity = line.QuantityReceived,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    Reason = $"Receipt from PO {receipt.PurchaseOrder?.OrderNumber ?? receipt.PurchaseOrderId.ToString()} - Receipt #{receipt.ReceiptNumber}",
                    Notes = $"Receipt #{receipt.ReceiptNumber}",
                    UserId = userId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.StockMovements.AddAsync(stockMovement);
            }

            await _unitOfWork.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error updating inventory: {ex.Message}");
        }
    }

    public virtual async Task<(bool Success, string? Error)> RollbackReceiptFromInventoryAsync(int receiptId, int userId)
    {
        try
        {
            var receipt = await _unitOfWork.Receipts.GetByIdWithDetailsAsync(receiptId, 0); // Pass 0 to skip business filter
            if (receipt == null)
            {
                return (false, "Receipt not found");
            }

            if (receipt.Status != ReceiptStatus.Completed)
            {
                return (false, "Can only rollback completed receipts");
            }

            foreach (var line in receipt.Lines)
            {
                if (line.Condition != ItemCondition.Good)
                {
                    continue;
                }

                var product = await _unitOfWork.Products.GetByIdAsync(line.ProductId);
                if (product == null)
                {
                    return (false, $"Product {line.ProductId} not found");
                }

                var previousStock = product.CurrentStock;
                var newStock = previousStock - line.QuantityReceived;

                if (newStock < 0)
                {
                    return (false, $"Cannot rollback receipt: would result in negative stock for {product.Name}");
                }

                product.CurrentStock = newStock;
                product.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Products.UpdateAsync(product);

                // Create stock movement record for the rollback
                var stockMovement = new StockMovement
                {
                    ProductId = line.ProductId,
                    MovementType = StockMovementType.StockAdjustment,
                    Quantity = -line.QuantityReceived,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    Reason = $"Rollback receipt {receipt.ReceiptNumber}",
                    Notes = $"Rollback - Receipt #{receipt.ReceiptNumber}",
                    UserId = userId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.StockMovements.AddAsync(stockMovement);
            }

            await _unitOfWork.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error rolling back inventory: {ex.Message}");
        }
    }
}
