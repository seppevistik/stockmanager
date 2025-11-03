namespace StockManager.Core.DTOs;

public class StockAdjustmentDto
{
    public int ProductId { get; set; }
    public decimal NewStock { get; set; }
}

public class BulkStockAdjustmentDto
{
    public List<StockAdjustmentDto> Adjustments { get; set; } = new();
    public string Reason { get; set; } = "Bulk stock adjustment";
}
