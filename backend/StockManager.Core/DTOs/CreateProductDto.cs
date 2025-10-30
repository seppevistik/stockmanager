namespace StockManager.Core.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string UnitOfMeasurement { get; set; } = "pieces";
    public string? ImageUrl { get; set; }
    public string? Supplier { get; set; }
    public int MinimumStockLevel { get; set; } = 0;
    public decimal InitialStock { get; set; } = 0;
    public decimal CostPerUnit { get; set; } = 0;
    public string? Location { get; set; }
}
