using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/receipts")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly ReceiptService _receiptService;

    public ReceiptsController(ReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var businessId = GetBusinessId();
        var receipts = await _receiptService.GetReceiptsAsync(businessId);
        return Ok(receipts);
    }

    [HttpGet("pending-validation")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetPendingValidation()
    {
        var businessId = GetBusinessId();
        var receipts = await _receiptService.GetPendingValidationAsync(businessId);
        return Ok(receipts);
    }

    [HttpGet("purchase-order/{purchaseOrderId}")]
    public async Task<IActionResult> GetByPurchaseOrder(int purchaseOrderId)
    {
        var businessId = GetBusinessId();
        var receipts = await _receiptService.GetReceiptsForPurchaseOrderAsync(purchaseOrderId, businessId);
        return Ok(receipts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var businessId = GetBusinessId();
        var receipt = await _receiptService.GetReceiptByIdAsync(id, businessId);

        if (receipt == null)
            return NotFound(new { message = "Receipt not found" });

        return Ok(receipt);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateReceiptDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();

        var (success, error, receipt) = await _receiptService.CreateReceiptAsync(createDto, businessId, userId);

        if (!success)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = receipt!.Id }, receipt);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReceiptDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var (success, error) = await _receiptService.UpdateReceiptAsync(id, updateDto, businessId);

        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }

    [HttpGet("{id}/validate")]
    public async Task<IActionResult> Validate(int id)
    {
        var businessId = GetBusinessId();

        try
        {
            var validation = await _receiptService.ValidateReceiptAsync(id, businessId);
            return Ok(validation);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveReceiptDto approveDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();

        var (success, error) = await _receiptService.ApproveReceiptAsync(id, approveDto, businessId, userId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Receipt approved successfully" });
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectReceiptDto rejectDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var businessId = GetBusinessId();
        var userId = GetUserId();

        var (success, error) = await _receiptService.RejectReceiptAsync(id, rejectDto, businessId, userId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Receipt rejected successfully" });
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Complete(int id)
    {
        var businessId = GetBusinessId();
        var userId = GetUserId();

        var (success, error) = await _receiptService.CompleteReceiptAsync(id, businessId, userId);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Receipt completed and inventory updated successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var businessId = GetBusinessId();
        var (success, error) = await _receiptService.DeleteReceiptAsync(id, businessId);

        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }
}
