using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class StockMovementService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockMovementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByBusinessAsync(int businessId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var movements = await _unitOfWork.StockMovements.GetByBusinessIdAsync(businessId, startDate, endDate);
        return movements.Select(MapToDto);
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByProductAsync(int productId, int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null || product.BusinessId != businessId)
            return Enumerable.Empty<StockMovementDto>();

        var movements = await _unitOfWork.StockMovements.GetByProductIdAsync(productId);
        return movements.Select(MapToDto);
    }

    public async Task<IEnumerable<StockMovementDto>> GetRecentMovementsAsync(int businessId, int count = 10)
    {
        var movements = await _unitOfWork.StockMovements.GetRecentMovementsAsync(businessId, count);
        return movements.Select(MapToDto);
    }

    public async Task<(bool Success, string? Error, StockMovementDto? Movement)> CreateMovementAsync(CreateStockMovementDto createDto, int businessId, string userId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(createDto.ProductId);
        if (product == null || product.BusinessId != businessId)
        {
            return (false, "Product not found", null);
        }

        if (createDto.Quantity <= 0)
        {
            return (false, "Quantity must be greater than zero", null);
        }

        var previousStock = product.CurrentStock;
        var newStock = previousStock;

        // Calculate new stock based on movement type
        switch (createDto.MovementType)
        {
            case StockMovementType.StockIn:
                newStock = previousStock + createDto.Quantity;
                break;
            case StockMovementType.StockOut:
                if (createDto.Quantity > previousStock)
                {
                    return (false, "Insufficient stock available", null);
                }
                newStock = previousStock - createDto.Quantity;
                break;
            case StockMovementType.StockAdjustment:
                // For adjustment, quantity can be positive or negative
                newStock = previousStock + createDto.Quantity;
                if (newStock < 0)
                {
                    return (false, "Stock cannot be negative after adjustment", null);
                }
                break;
            case StockMovementType.StockTransfer:
                // For MVP, we'll treat transfer as a simple movement
                // In future phases, this would involve two locations
                if (createDto.Quantity > previousStock)
                {
                    return (false, "Insufficient stock available for transfer", null);
                }
                newStock = previousStock - createDto.Quantity;
                break;
        }

        var stockMovement = new StockMovement
        {
            ProductId = createDto.ProductId,
            MovementType = createDto.MovementType,
            Quantity = createDto.Quantity,
            PreviousStock = previousStock,
            NewStock = newStock,
            Reason = createDto.Reason,
            Notes = createDto.Notes,
            FromLocation = createDto.FromLocation,
            ToLocation = createDto.ToLocation,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StockMovements.AddAsync(stockMovement);

        // Update product stock
        product.CurrentStock = newStock;
        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Products.UpdateAsync(product);

        await _unitOfWork.SaveChangesAsync();

        return (true, null, MapToDto(stockMovement));
    }

    private StockMovementDto MapToDto(StockMovement movement)
    {
        return new StockMovementDto
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            ProductName = movement.Product?.Name ?? "",
            ProductSKU = movement.Product?.SKU ?? "",
            MovementType = movement.MovementType,
            Quantity = movement.Quantity,
            PreviousStock = movement.PreviousStock,
            NewStock = movement.NewStock,
            Reason = movement.Reason,
            Notes = movement.Notes,
            FromLocation = movement.FromLocation,
            ToLocation = movement.ToLocation,
            UserName = movement.User != null ? $"{movement.User.FirstName} {movement.User.LastName}" : "",
            CreatedAt = movement.CreatedAt
        };
    }
}
