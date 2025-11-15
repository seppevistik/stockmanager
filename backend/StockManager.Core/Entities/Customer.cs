namespace StockManager.Core.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsCompany { get; set; } = false;

    // Contact Information
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Address Information
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }

    // Business-specific fields (optional)
    public string? CompanyName { get; set; }
    public string? TaxNumber { get; set; }
    public string? Website { get; set; }

    // Payment and Credit Information
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermsDays { get; set; }
    public string? PaymentMethod { get; set; }

    // Additional Information
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Multi-tenant support
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    // Navigation properties
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
