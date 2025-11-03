namespace StockManager.Core.Entities;

public class ProductSupplier
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public decimal? SupplierPrice { get; set; }
    public string? SupplierProductCode { get; set; }
    public int? LeadTimeDays { get; set; }
    public int? MinimumOrderQuantity { get; set; }
    public bool IsPrimarySupplier { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
