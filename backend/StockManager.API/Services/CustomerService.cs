using Microsoft.EntityFrameworkCore;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Data.Contexts;

namespace StockManager.API.Services;

public class CustomerService
{
    private readonly ApplicationDbContext _context;

    public CustomerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public virtual async Task<PagedResult<CustomerDto>> GetCustomersAsync(
        int businessId,
        CustomerListQuery query)
    {
        var queryable = _context.Customers
            .Where(c => c.BusinessId == businessId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(c =>
                c.Name.ToLower().Contains(searchTerm) ||
                (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                (c.Phone != null && c.Phone.ToLower().Contains(searchTerm)) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(searchTerm)));
        }

        if (query.IsCompany.HasValue)
        {
            queryable = queryable.Where(c => c.IsCompany == query.IsCompany.Value);
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            queryable = queryable.Where(c => c.Country == query.Country);
        }

        // Apply sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "email" => query.SortDirection?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.Email)
                : queryable.OrderBy(c => c.Email),
            "city" => query.SortDirection?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.City)
                : queryable.OrderBy(c => c.City),
            "country" => query.SortDirection?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.Country)
                : queryable.OrderBy(c => c.Country),
            "createdat" => query.SortDirection?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.CreatedAt)
                : queryable.OrderBy(c => c.CreatedAt),
            _ => query.SortDirection?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.Name)
                : queryable.OrderBy(c => c.Name)
        };

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var customers = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = customers.Select(MapToDto).ToList();

        return new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public virtual async Task<CustomerDto?> GetCustomerByIdAsync(int id, int businessId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        return customer != null ? MapToDto(customer) : null;
    }

    public virtual async Task<(bool Success, string? Error, CustomerDto? Customer)> CreateCustomerAsync(
        CreateCustomerDto createDto,
        int businessId)
    {
        // Check if customer with same name already exists
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Name == createDto.Name && c.BusinessId == businessId);

        if (existingCustomer != null)
        {
            return (false, "A customer with this name already exists", null);
        }

        // Check if email already exists (if provided)
        if (!string.IsNullOrWhiteSpace(createDto.Email))
        {
            var existingEmail = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == createDto.Email && c.BusinessId == businessId);

            if (existingEmail != null)
            {
                return (false, "A customer with this email already exists", null);
            }
        }

        var customer = new Customer
        {
            Name = createDto.Name,
            IsCompany = createDto.IsCompany,
            ContactPerson = createDto.ContactPerson,
            Email = createDto.Email,
            Phone = createDto.Phone,
            Address = createDto.Address,
            City = createDto.City,
            State = createDto.State,
            Country = createDto.Country,
            PostalCode = createDto.PostalCode,
            CompanyName = createDto.CompanyName,
            TaxNumber = createDto.TaxNumber,
            Website = createDto.Website,
            CreditLimit = createDto.CreditLimit,
            PaymentTermsDays = createDto.PaymentTermsDays,
            PaymentMethod = createDto.PaymentMethod,
            Notes = createDto.Notes,
            IsActive = createDto.IsActive,
            BusinessId = businessId
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return (true, null, MapToDto(customer));
    }

    public virtual async Task<(bool Success, string? Error)> UpdateCustomerAsync(
        int id,
        UpdateCustomerDto updateDto,
        int businessId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (customer == null)
        {
            return (false, "Customer not found");
        }

        // Check name uniqueness if changed
        if (customer.Name != updateDto.Name)
        {
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Name == updateDto.Name && c.BusinessId == businessId);

            if (existingCustomer != null)
            {
                return (false, "A customer with this name already exists");
            }
        }

        // Check email uniqueness if changed
        if (customer.Email != updateDto.Email && !string.IsNullOrWhiteSpace(updateDto.Email))
        {
            var existingEmail = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == updateDto.Email && c.BusinessId == businessId);

            if (existingEmail != null)
            {
                return (false, "A customer with this email already exists");
            }
        }

        customer.Name = updateDto.Name;
        customer.IsCompany = updateDto.IsCompany;
        customer.ContactPerson = updateDto.ContactPerson;
        customer.Email = updateDto.Email;
        customer.Phone = updateDto.Phone;
        customer.Address = updateDto.Address;
        customer.City = updateDto.City;
        customer.State = updateDto.State;
        customer.Country = updateDto.Country;
        customer.PostalCode = updateDto.PostalCode;
        customer.CompanyName = updateDto.CompanyName;
        customer.TaxNumber = updateDto.TaxNumber;
        customer.Website = updateDto.Website;
        customer.CreditLimit = updateDto.CreditLimit;
        customer.PaymentTermsDays = updateDto.PaymentTermsDays;
        customer.PaymentMethod = updateDto.PaymentMethod;
        customer.Notes = updateDto.Notes;
        customer.IsActive = updateDto.IsActive;

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> DeleteCustomerAsync(int id, int businessId)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (customer == null)
        {
            return (false, "Customer not found");
        }

        // Soft delete
        customer.IsDeleted = true;
        await _context.SaveChangesAsync();

        return (true, null);
    }

    private CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            IsCompany = customer.IsCompany,
            ContactPerson = customer.ContactPerson,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Country = customer.Country,
            PostalCode = customer.PostalCode,
            CompanyName = customer.CompanyName,
            TaxNumber = customer.TaxNumber,
            Website = customer.Website,
            CreditLimit = customer.CreditLimit,
            PaymentTermsDays = customer.PaymentTermsDays,
            PaymentMethod = customer.PaymentMethod,
            Notes = customer.Notes,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
