using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManager.Data.Contexts;

namespace StockManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetBusinessId() => int.Parse(User.FindFirst("BusinessId")?.Value ?? "0");

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var businessId = GetBusinessId();

        var totalProducts = await _context.Products
            .Where(p => p.BusinessId == businessId && !p.IsDeleted)
            .CountAsync();

        var lowStockCount = await _context.Products
            .Where(p => p.BusinessId == businessId && !p.IsDeleted && p.CurrentStock <= p.MinimumStockLevel)
            .CountAsync();

        var outOfStockCount = await _context.Products
            .Where(p => p.BusinessId == businessId && !p.IsDeleted && p.CurrentStock == 0)
            .CountAsync();

        var totalInventoryValue = await _context.Products
            .Where(p => p.BusinessId == businessId && !p.IsDeleted)
            .SumAsync(p => p.CurrentStock * p.CostPerUnit);

        return Ok(new
        {
            totalProducts,
            lowStockCount,
            outOfStockCount,
            totalInventoryValue
        });
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int count = 5)
    {
        var businessId = GetBusinessId();

        var recentMovements = await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.User)
            .Where(sm => sm.Product.BusinessId == businessId && !sm.IsDeleted)
            .OrderByDescending(sm => sm.CreatedAt)
            .Take(count)
            .Select(sm => new
            {
                sm.Id,
                productName = sm.Product.Name,
                movementType = sm.MovementType.ToString(),
                sm.Quantity,
                userName = sm.User.FirstName + " " + sm.User.LastName,
                sm.CreatedAt
            })
            .ToListAsync();

        return Ok(recentMovements);
    }

    [HttpGet("stock-summary")]
    public async Task<IActionResult> GetStockSummary()
    {
        var businessId = GetBusinessId();

        var products = await _context.Products
            .Where(p => p.BusinessId == businessId && !p.IsDeleted)
            .ToListAsync();

        var inStock = products.Count(p => p.CurrentStock > p.MinimumStockLevel);
        var lowStock = products.Count(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStockLevel);
        var outOfStock = products.Count(p => p.CurrentStock == 0);

        return Ok(new
        {
            inStock,
            lowStock,
            outOfStock
        });
    }

    [HttpGet("sales-costs")]
    public async Task<IActionResult> GetSalesCostsData([FromQuery] int days = 30)
    {
        var businessId = GetBusinessId();
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        // Get sales orders with their lines and products for the specified period
        var salesOrders = await _context.SalesOrders
            .Include(so => so.Lines)
            .ThenInclude(line => line.Product)
            .Where(so => so.BusinessId == businessId
                && !so.IsDeleted
                && so.OrderDate >= startDate
                && (so.Status == StockManager.Core.Enums.SalesOrderStatus.Shipped
                    || so.Status == StockManager.Core.Enums.SalesOrderStatus.Delivered))
            .ToListAsync();

        // Group by date and calculate daily sales and costs
        var dailyData = salesOrders
            .GroupBy(so => so.OrderDate.Date)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                sales = g.Sum(so => so.TotalAmount),
                costs = g.SelectMany(so => so.Lines)
                    .Sum(line => line.QuantityShipped * line.Product.CostPerUnit)
            })
            .OrderBy(d => d.date)
            .ToList();

        // Fill in missing dates with zero values
        var result = new List<object>();
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var dateStr = date.ToString("yyyy-MM-dd");
            var existingData = dailyData.FirstOrDefault(d => d.date == dateStr);

            result.Add(new
            {
                date = dateStr,
                sales = existingData?.sales ?? 0m,
                costs = existingData?.costs ?? 0m
            });
        }

        return Ok(result);
    }
}
