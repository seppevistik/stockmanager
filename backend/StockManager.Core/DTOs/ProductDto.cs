using StockManager.Core.Enums;

namespace StockManager.Core.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string UnitOfMeasurement { get; set; } = "pieces";
    public string? ImageUrl { get; set; }
    public string? Supplier { get; set; }
    public int MinimumStockLevel { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal TotalValue { get; set; }
    public ProductStatus Status { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductSupplierDto> Suppliers { get; set; } = new();
}
