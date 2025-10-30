using StockManager.Core.Enums;

namespace StockManager.Core.DTOs;

public class CreateStockMovementDto
{
    public int ProductId { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
}
