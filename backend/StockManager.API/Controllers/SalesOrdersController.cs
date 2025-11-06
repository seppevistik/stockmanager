using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Services;
using StockManager.Core.DTOs;
using System.Security.Claims;

namespace StockManager.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesOrdersController : ControllerBase
{
    private readonly SalesOrderService _salesOrderService;
    private readonly ILogger<SalesOrdersController> _logger;

    public SalesOrdersController(
        SalesOrderService salesOrderService,
        ILogger<SalesOrdersController> logger)
    {
        _salesOrderService = salesOrderService;
        _logger = logger;
    }

    private int GetBusinessId() => int.Parse(User.FindFirstValue("BusinessId")!);
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Get paginated list of sales orders with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesOrderSummaryDto>>> GetSalesOrders([FromQuery] SalesOrderListQuery query)
    {
        try
        {
            var result = await _salesOrderService.GetSalesOrdersAsync(GetBusinessId(), query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales orders");
            return StatusCode(500, new { message = "An error occurred while retrieving sales orders" });
        }
    }

    /// <summary>
    /// Get sales order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SalesOrderDto>> GetSalesOrder(int id)
    {
        try
        {
            var salesOrder = await _salesOrderService.GetSalesOrderByIdAsync(GetBusinessId(), id);

            if (salesOrder == null)
                return NotFound(new { message = "Sales order not found" });

            return Ok(salesOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the sales order" });
        }
    }

    /// <summary>
    /// Create a new sales order in Draft status
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> CreateSalesOrder([FromBody] CreateSalesOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.CreateSalesOrderAsync(
                GetBusinessId(),
                GetUserId(),
                request);

            return CreatedAtAction(nameof(GetSalesOrder), new { id = salesOrder.Id }, salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales order");
            return StatusCode(500, new { message = "An error occurred while creating the sales order" });
        }
    }

    /// <summary>
    /// Update a sales order (only in Draft status)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SalesOrderDto>> UpdateSalesOrder(int id, [FromBody] UpdateSalesOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.UpdateSalesOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the sales order" });
        }
    }

    /// <summary>
    /// Delete a sales order (only in Draft status)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSalesOrder(int id)
    {
        try
        {
            var result = await _salesOrderService.DeleteSalesOrderAsync(
                GetBusinessId(),
                id,
                GetUserId());

            if (!result)
                return NotFound(new { message = "Sales order not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the sales order" });
        }
    }

    /// <summary>
    /// Submit a draft order for review (Draft → Submitted)
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<SalesOrderDto>> SubmitOrder(int id, [FromBody] SubmitOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.SubmitOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while submitting the sales order" });
        }
    }

    /// <summary>
    /// Confirm a submitted order (Submitted → Confirmed)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SalesOrderDto>> ConfirmOrder(int id, [FromBody] ConfirmOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.ConfirmOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while confirming the sales order" });
        }
    }

    /// <summary>
    /// Cancel an order (any status → Cancelled, except Shipped/Delivered)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SalesOrderDto>> CancelOrder(int id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.CancelOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling the sales order" });
        }
    }

    /// <summary>
    /// Put an order on hold (any status → OnHold, except Cancelled/Shipped/Delivered)
    /// </summary>
    [HttpPost("{id}/hold")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SalesOrderDto>> HoldOrder(int id, [FromBody] HoldOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.HoldOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error holding sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while holding the sales order" });
        }
    }

    /// <summary>
    /// Release an order from hold (OnHold → Confirmed)
    /// </summary>
    [HttpPost("{id}/release")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SalesOrderDto>> ReleaseOrder(int id, [FromBody] ReleaseOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.ReleaseOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while releasing the sales order" });
        }
    }

    /// <summary>
    /// Start picking an order (Confirmed → Picking)
    /// </summary>
    [HttpPost("{id}/start-picking")]
    public async Task<ActionResult<SalesOrderDto>> StartPicking(int id, [FromBody] StartPickingRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.StartPickingAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting picking for sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while starting picking" });
        }
    }

    /// <summary>
    /// Complete picking an order (Picking → Picked)
    /// </summary>
    [HttpPost("{id}/complete-picking")]
    public async Task<ActionResult<SalesOrderDto>> CompletePicking(int id, [FromBody] CompletePickingRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.CompletePickingAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing picking for sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while completing picking" });
        }
    }

    /// <summary>
    /// Start packing an order (Picked → Packing)
    /// </summary>
    [HttpPost("{id}/start-packing")]
    public async Task<ActionResult<SalesOrderDto>> StartPacking(int id, [FromBody] StartPackingRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.StartPackingAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting packing for sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while starting packing" });
        }
    }

    /// <summary>
    /// Complete packing an order (Packing → Packed)
    /// </summary>
    [HttpPost("{id}/complete-packing")]
    public async Task<ActionResult<SalesOrderDto>> CompletePacking(int id, [FromBody] CompletePackingRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.CompletePackingAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing packing for sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while completing packing" });
        }
    }

    /// <summary>
    /// Ship an order (Packed → Shipped)
    /// </summary>
    [HttpPost("{id}/ship")]
    public async Task<ActionResult<SalesOrderDto>> ShipOrder(int id, [FromBody] ShipOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.ShipOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while shipping the order" });
        }
    }

    /// <summary>
    /// Mark an order as delivered (Shipped → Delivered)
    /// </summary>
    [HttpPost("{id}/deliver")]
    public async Task<ActionResult<SalesOrderDto>> DeliverOrder(int id, [FromBody] DeliverOrderRequest request)
    {
        try
        {
            var salesOrder = await _salesOrderService.DeliverOrderAsync(
                GetBusinessId(),
                id,
                GetUserId(),
                request);

            return Ok(salesOrder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering sales order {Id}", id);
            return StatusCode(500, new { message = "An error occurred while marking the order as delivered" });
        }
    }

    /// <summary>
    /// Get sales order statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<SalesOrderStatistics>> GetStatistics()
    {
        try
        {
            var statistics = await _salesOrderService.GetStatisticsAsync(GetBusinessId());
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales order statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
        }
    }
}
