using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly CompanyService _companyService;

    public CompaniesController(CompanyService companyService)
    {
        _companyService = companyService;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var businessId = GetBusinessId();
        var companies = await _companyService.GetCompaniesByBusinessAsync(businessId);
        return Ok(companies);
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers()
    {
        var businessId = GetBusinessId();
        var suppliers = await _companyService.GetSuppliersAsync(businessId);
        return Ok(suppliers);
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        var businessId = GetBusinessId();
        var customers = await _companyService.GetCustomersAsync(businessId);
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var businessId = GetBusinessId();
        var company = await _companyService.GetCompanyByIdAsync(id, businessId);

        if (company == null)
            return NotFound();

        return Ok(company);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateCompanyDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var result = await _companyService.CreateCompanyAsync(createDto, businessId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Company!.Id }, result.Company);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCompanyDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var result = await _companyService.UpdateCompanyAsync(id, updateDto, businessId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var businessId = GetBusinessId();
        var result = await _companyService.DeleteCompanyAsync(id, businessId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }
}
