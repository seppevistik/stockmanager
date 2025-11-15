using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CustomerListQuery query)
    {
        var businessId = GetBusinessId();
        var result = await _customerService.GetCustomersAsync(businessId, query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var businessId = GetBusinessId();
        var customer = await _customerService.GetCustomerByIdAsync(id, businessId);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var result = await _customerService.CreateCustomerAsync(createDto, businessId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Customer!.Id }, result.Customer);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var result = await _customerService.UpdateCustomerAsync(id, updateDto, businessId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var businessId = GetBusinessId();
        var result = await _customerService.DeleteCustomerAsync(id, businessId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }
}
