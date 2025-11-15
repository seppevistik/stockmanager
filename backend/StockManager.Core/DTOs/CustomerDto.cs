using System;
using System.ComponentModel.DataAnnotations;

namespace StockManager.Core.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompany { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxNumber { get; set; }
    public string? Website { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermsDays { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCustomerDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsCompany { get; set; }

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(50)]
    public string? TaxNumber { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CreditLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int? PaymentTermsDays { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateCustomerDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsCompany { get; set; }

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(50)]
    public string? TaxNumber { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CreditLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int? PaymentTermsDays { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }
}

public class CustomerListQuery
{
    public string? SearchTerm { get; set; }
    public bool? IsCompany { get; set; }
    public bool? IsActive { get; set; }
    public string? Country { get; set; }
    public string SortBy { get; set; } = "Name";
    public string SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
