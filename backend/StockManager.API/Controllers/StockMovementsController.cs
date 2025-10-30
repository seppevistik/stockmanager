using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly StockMovementService _stockMovementService;

    public StockMovementsController(StockMovementService stockMovementService)
    {
        _stockMovementService = stockMovementService;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var businessId = GetBusinessId();
        var movements = await _stockMovementService.GetMovementsByBusinessAsync(businessId, startDate, endDate);
        return Ok(movements);
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var businessId = GetBusinessId();
        var movements = await _stockMovementService.GetMovementsByProductAsync(productId, businessId);
        return Ok(movements);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10)
    {
        var businessId = GetBusinessId();
        var movements = await _stockMovementService.GetRecentMovementsAsync(businessId, count);
        return Ok(movements);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateStockMovementDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();
        var result = await _stockMovementService.CreateMovementAsync(createDto, businessId, userId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(result.Movement);
    }
}
