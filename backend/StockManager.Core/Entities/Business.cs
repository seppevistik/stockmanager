namespace StockManager.Core.Entities;

public class Business : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxNumber { get; set; }

    // Navigation properties
    public ICollection<UserBusiness> UserBusinesses { get; set; } = new List<UserBusiness>();
    public ICollection<ApplicationUser> CurrentUsers { get; set; } = new List<ApplicationUser>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
