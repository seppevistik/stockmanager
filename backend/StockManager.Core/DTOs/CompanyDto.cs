namespace StockManager.Core.DTOs;

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public bool IsSupplier { get; set; }
    public bool IsCustomer { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCompanyDto
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public bool IsSupplier { get; set; }
    public bool IsCustomer { get; set; }
    public string? Notes { get; set; }
}

public class ProductSupplierDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal? SupplierPrice { get; set; }
    public string? SupplierProductCode { get; set; }
    public int? LeadTimeDays { get; set; }
    public int? MinimumOrderQuantity { get; set; }
    public bool IsPrimarySupplier { get; set; }
}

public class CreateProductSupplierDto
{
    public int CompanyId { get; set; }
    public decimal? SupplierPrice { get; set; }
    public string? SupplierProductCode { get; set; }
    public int? LeadTimeDays { get; set; }
    public int? MinimumOrderQuantity { get; set; }
    public bool IsPrimarySupplier { get; set; }
}
