using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class StockMovement : BaseEntity
{
    public int ProductId { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Navigation properties
    public Product Product { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
