using StockManager.Core.Enums;

namespace StockManager.Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string UnitOfMeasurement { get; set; } = "pieces";
    public string? ImageUrl { get; set; }
    public string? Supplier { get; set; }
    public int MinimumStockLevel { get; set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    public decimal CostPerUnit { get; set; } = 0;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public int BusinessId { get; set; }
    public string? Location { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<ProductSupplier> ProductSuppliers { get; set; } = new List<ProductSupplier>();
}
