using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class CompanyService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public virtual async Task<IEnumerable<CompanyDto>> GetCompaniesByBusinessAsync(int businessId)
    {
        var companies = await _unitOfWork.Companies.GetByBusinessIdAsync(businessId);
        return companies.Select(MapToDto);
    }

    public virtual async Task<IEnumerable<CompanyDto>> GetSuppliersAsync(int businessId)
    {
        var suppliers = await _unitOfWork.Companies.GetSuppliersAsync(businessId);
        return suppliers.Select(MapToDto);
    }

    public virtual async Task<IEnumerable<CompanyDto>> GetCustomersAsync(int businessId)
    {
        var customers = await _unitOfWork.Companies.GetCustomersAsync(businessId);
        return customers.Select(MapToDto);
    }

    public virtual async Task<CompanyDto?> GetCompanyByIdAsync(int id, int businessId)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null || company.BusinessId != businessId)
            return null;

        return MapToDto(company);
    }

    public virtual async Task<(bool Success, string? Error, CompanyDto? Company)> CreateCompanyAsync(
        CreateCompanyDto createDto,
        int businessId)
    {
        // Check if company name already exists
        var existingCompany = await _unitOfWork.Companies.GetByNameAsync(createDto.Name, businessId);
        if (existingCompany != null)
        {
            return (false, "A company with this name already exists", null);
        }

        var company = new Company
        {
            Name = createDto.Name,
            ContactPerson = createDto.ContactPerson,
            Email = createDto.Email,
            Phone = createDto.Phone,
            Address = createDto.Address,
            City = createDto.City,
            Country = createDto.Country,
            PostalCode = createDto.PostalCode,
            Website = createDto.Website,
            TaxNumber = createDto.TaxNumber,
            IsSupplier = createDto.IsSupplier,
            IsCustomer = createDto.IsCustomer,
            Notes = createDto.Notes,
            BusinessId = businessId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Companies.AddAsync(company);
        await _unitOfWork.SaveChangesAsync();

        return (true, null, MapToDto(company));
    }

    public virtual async Task<(bool Success, string? Error)> UpdateCompanyAsync(
        int id,
        CreateCompanyDto updateDto,
        int businessId)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null || company.BusinessId != businessId)
        {
            return (false, "Company not found");
        }

        // Check name uniqueness if changed
        if (company.Name != updateDto.Name)
        {
            var existingCompany = await _unitOfWork.Companies.GetByNameAsync(updateDto.Name, businessId);
            if (existingCompany != null)
            {
                return (false, "A company with this name already exists");
            }
        }

        company.Name = updateDto.Name;
        company.ContactPerson = updateDto.ContactPerson;
        company.Email = updateDto.Email;
        company.Phone = updateDto.Phone;
        company.Address = updateDto.Address;
        company.City = updateDto.City;
        company.Country = updateDto.Country;
        company.PostalCode = updateDto.PostalCode;
        company.Website = updateDto.Website;
        company.TaxNumber = updateDto.TaxNumber;
        company.IsSupplier = updateDto.IsSupplier;
        company.IsCustomer = updateDto.IsCustomer;
        company.Notes = updateDto.Notes;
        company.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Companies.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> DeleteCompanyAsync(int id, int businessId)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null || company.BusinessId != businessId)
        {
            return (false, "Company not found");
        }

        company.IsDeleted = true;
        company.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Companies.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    private CompanyDto MapToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            ContactPerson = company.ContactPerson,
            Email = company.Email,
            Phone = company.Phone,
            Address = company.Address,
            City = company.City,
            Country = company.Country,
            PostalCode = company.PostalCode,
            Website = company.Website,
            TaxNumber = company.TaxNumber,
            IsSupplier = company.IsSupplier,
            IsCustomer = company.IsCustomer,
            Notes = company.Notes,
            CreatedAt = company.CreatedAt
        };
    }
}
