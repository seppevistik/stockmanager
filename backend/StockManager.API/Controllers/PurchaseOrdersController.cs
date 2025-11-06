using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly PurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(PurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseOrderFilterDto? filter)
    {
        var businessId = GetBusinessId();
        var purchaseOrders = await _purchaseOrderService.GetPurchaseOrdersAsync(businessId, filter);
        return Ok(purchaseOrders);
    }

    [HttpGet("outstanding")]
    public async Task<IActionResult> GetOutstanding()
    {
        var businessId = GetBusinessId();
        var purchaseOrders = await _purchaseOrderService.GetOutstandingOrdersAsync(businessId);
        return Ok(purchaseOrders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var businessId = GetBusinessId();
        var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id, businessId);

        if (purchaseOrder == null)
            return NotFound(new { message = "Purchase order not found" });

        return Ok(purchaseOrder);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();

        var (success, error, purchaseOrder) = await _purchaseOrderService.CreatePurchaseOrderAsync(createDto, businessId, userId);

        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = purchaseOrder!.Id }, purchaseOrder);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseOrderDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var (success, error) = await _purchaseOrderService.UpdatePurchaseOrderAsync(id, updateDto, businessId);

        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }

    [HttpPost("{id}/submit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Submit(int id)
    {
        var businessId = GetBusinessId();
        var userId = GetUserId();

        var (success, error) = await _purchaseOrderService.SubmitPurchaseOrderAsync(id, businessId, userId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Purchase order submitted successfully" });
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmPurchaseOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var (success, error) = await _purchaseOrderService.ConfirmPurchaseOrderAsync(id, request.ConfirmedDeliveryDate, businessId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Purchase order confirmed successfully" });
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelPurchaseOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();

        var (success, error) = await _purchaseOrderService.CancelPurchaseOrderAsync(id, request.Reason, businessId, userId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Purchase order cancelled successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var businessId = GetBusinessId();
        var (success, error) = await _purchaseOrderService.DeletePurchaseOrderAsync(id, businessId);

        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }
}

public class ConfirmPurchaseOrderRequest
{
    public DateTime ConfirmedDeliveryDate { get; set; }
}

public class CancelPurchaseOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}
