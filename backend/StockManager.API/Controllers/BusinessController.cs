using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BusinessController : ControllerBase
{
    private readonly BusinessService _businessService;

    public BusinessController(BusinessService businessService)
    {
        _businessService = businessService;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private int GetUserRole() => int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "3");

    /// <summary>
    /// Get the current user's business information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBusiness()
    {
        var businessId = GetBusinessId();
        var business = await _businessService.GetBusinessByIdAsync(businessId);

        if (business == null)
            return NotFound(new { message = "Business not found" });

        return Ok(business);
    }

    /// <summary>
    /// Update business settings (Admin only)
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBusiness([FromBody] UpdateBusinessDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();
        var userRole = GetUserRole();

        var result = await _businessService.UpdateBusinessAsync(businessId, updateDto, userId, userRole);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }
}
