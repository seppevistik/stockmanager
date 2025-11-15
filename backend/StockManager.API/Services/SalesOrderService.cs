using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Data.Contexts;

namespace StockManager.API.Services;

public class SalesOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SalesOrderService> _logger;
    private readonly StockMovementService _stockMovementService;

    public SalesOrderService(
        ApplicationDbContext context,
        ILogger<SalesOrderService> logger,
        StockMovementService stockMovementService)
    {
        _context = context;
        _logger = logger;
        _stockMovementService = stockMovementService;
    }

    // List and Search
    public async Task<PagedResult<SalesOrderSummaryDto>> GetSalesOrdersAsync(
        int businessId,
        SalesOrderListQuery query)
    {
        var queryable = _context.SalesOrders
            .Where(so => so.BusinessId == businessId)
            .Include(so => so.Customer)
            .Include(so => so.Lines)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(so =>
                so.OrderNumber.ToLower().Contains(searchTerm) ||
                (so.Customer != null && so.Customer.Name.ToLower().Contains(searchTerm)) ||
                so.CustomerReference!.ToLower().Contains(searchTerm));
        }

        if (query.CustomerId.HasValue)
        {
            queryable = queryable.Where(so => so.CustomerId == query.CustomerId.Value);
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(so => so.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            queryable = queryable.Where(so => so.Priority == query.Priority.Value);
        }

        if (query.OrderDateFrom.HasValue)
        {
            queryable = queryable.Where(so => so.OrderDate >= query.OrderDateFrom.Value);
        }

        if (query.OrderDateTo.HasValue)
        {
            queryable = queryable.Where(so => so.OrderDate <= query.OrderDateTo.Value);
        }

        if (query.RequiredDateFrom.HasValue)
        {
            queryable = queryable.Where(so => so.RequiredDate >= query.RequiredDateFrom.Value);
        }

        if (query.RequiredDateTo.HasValue)
        {
            queryable = queryable.Where(so => so.RequiredDate <= query.RequiredDateTo.Value);
        }

        // Apply sorting
        queryable = ApplySorting(queryable, query.SortBy, query.SortDirection);

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(so => new SalesOrderSummaryDto
            {
                Id = so.Id,
                OrderNumber = so.OrderNumber,
                CustomerId = so.CustomerId,
                CustomerName = so.Customer != null ? so.Customer.Name : null,
                OrderDate = so.OrderDate,
                RequiredDate = so.RequiredDate,
                Status = so.Status,
                StatusDisplay = so.Status.ToString(),
                Priority = so.Priority,
                PriorityDisplay = so.Priority.ToString(),
                TotalAmount = so.TotalAmount,
                TotalLineItems = so.Lines.Count,
                ShipToCity = so.ShipToCity,
                ShipToState = so.ShipToState
            })
            .ToListAsync();

        return new PagedResult<SalesOrderSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // Get by ID
    public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(int businessId, int id)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .Include(so => so.Customer)
            .Include(so => so.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            return null;

        return MapToDto(salesOrder);
    }

    // Create
    public async Task<SalesOrderDto> CreateSalesOrderAsync(
        int businessId,
        string userId,
        CreateSalesOrderRequest request)
    {
        // Validate customer exists and belongs to business (if provided)
        if (request.CustomerId.HasValue)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && c.BusinessId == businessId);

            if (customer == null)
                throw new InvalidOperationException("Customer not found");
        }

        // Validate products exist and belong to business
        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.BusinessId == businessId)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more products not found");

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync(businessId);

        // Calculate totals
        decimal subTotal = 0;
        var lines = new List<SalesOrderLine>();

        foreach (var lineRequest in request.Lines)
        {
            var product = products[lineRequest.ProductId];
            var lineTotal = lineRequest.QuantityOrdered * lineRequest.UnitPrice;
            var discount = lineTotal * (lineRequest.DiscountPercent / 100);
            lineTotal -= discount;

            subTotal += lineTotal;

            lines.Add(new SalesOrderLine
            {
                ProductId = lineRequest.ProductId,
                ProductName = product.Name,
                ProductSku = product.SKU,
                QuantityOrdered = lineRequest.QuantityOrdered,
                QuantityPicked = 0,
                QuantityShipped = 0,
                QuantityOutstanding = lineRequest.QuantityOrdered,
                UnitPrice = lineRequest.UnitPrice,
                DiscountPercent = lineRequest.DiscountPercent,
                LineTotal = lineTotal,
                Status = SalesOrderLineStatus.Pending,
                Notes = lineRequest.Notes
            });
        }

        var taxAmount = subTotal * (request.TaxRate / 100);
        var totalAmount = subTotal + taxAmount + request.ShippingCost - request.DiscountAmount;

        // Create sales order
        var salesOrder = new SalesOrder
        {
            BusinessId = businessId,
            OrderNumber = orderNumber,
            CustomerId = request.CustomerId,
            ShipToName = request.ShipToName,
            ShipToAddress = request.ShipToAddress,
            ShipToCity = request.ShipToCity,
            ShipToState = request.ShipToState,
            ShipToPostalCode = request.ShipToPostalCode,
            ShipToCountry = request.ShipToCountry,
            ShipToPhone = request.ShipToPhone,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            TaxRate = request.TaxRate,
            ShippingCost = request.ShippingCost,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = totalAmount,
            Status = SalesOrderStatus.Draft,
            Priority = request.Priority,
            OrderDate = DateTime.UtcNow,
            RequiredDate = request.RequiredDate,
            PromisedDate = request.PromisedDate,
            ShippingMethod = request.ShippingMethod,
            CustomerReference = request.CustomerReference,
            Notes = request.Notes,
            InternalNotes = request.InternalNotes,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };

        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} created for customer {CustomerId} by user {UserId}",
            orderNumber, request.CustomerId, userId);

        return (await GetSalesOrderByIdAsync(businessId, salesOrder.Id))!;
    }

    // Update (only for Draft status)
    public async Task<SalesOrderDto> UpdateSalesOrderAsync(
        int businessId,
        int id,
        string userId,
        UpdateSalesOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .Include(so => so.Lines)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Only draft orders can be updated");

        // Validate customer exists and belongs to business (if provided)
        if (request.CustomerId.HasValue)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && c.BusinessId == businessId);

            if (customer == null)
                throw new InvalidOperationException("Customer not found");
        }

        // Validate products exist and belong to business
        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.BusinessId == businessId)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more products not found");

        // Update header
        salesOrder.CustomerId = request.CustomerId;
        salesOrder.ShipToName = request.ShipToName;
        salesOrder.ShipToAddress = request.ShipToAddress;
        salesOrder.ShipToCity = request.ShipToCity;
        salesOrder.ShipToState = request.ShipToState;
        salesOrder.ShipToPostalCode = request.ShipToPostalCode;
        salesOrder.ShipToCountry = request.ShipToCountry;
        salesOrder.ShipToPhone = request.ShipToPhone;
        salesOrder.TaxRate = request.TaxRate;
        salesOrder.ShippingCost = request.ShippingCost;
        salesOrder.DiscountAmount = request.DiscountAmount;
        salesOrder.Priority = request.Priority;
        salesOrder.RequiredDate = request.RequiredDate;
        salesOrder.PromisedDate = request.PromisedDate;
        salesOrder.ShippingMethod = request.ShippingMethod;
        salesOrder.CustomerReference = request.CustomerReference;
        salesOrder.Notes = request.Notes;
        salesOrder.InternalNotes = request.InternalNotes;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        // Update lines - remove existing and add new
        _context.SalesOrderLines.RemoveRange(salesOrder.Lines);

        decimal subTotal = 0;
        var newLines = new List<SalesOrderLine>();

        foreach (var lineRequest in request.Lines)
        {
            var product = products[lineRequest.ProductId];
            var lineTotal = lineRequest.QuantityOrdered * lineRequest.UnitPrice;
            var discount = lineTotal * (lineRequest.DiscountPercent / 100);
            lineTotal -= discount;

            subTotal += lineTotal;

            newLines.Add(new SalesOrderLine
            {
                SalesOrderId = salesOrder.Id,
                ProductId = lineRequest.ProductId,
                ProductName = product.Name,
                ProductSku = product.SKU,
                QuantityOrdered = lineRequest.QuantityOrdered,
                QuantityPicked = 0,
                QuantityShipped = 0,
                QuantityOutstanding = lineRequest.QuantityOrdered,
                UnitPrice = lineRequest.UnitPrice,
                DiscountPercent = lineRequest.DiscountPercent,
                LineTotal = lineTotal,
                Status = SalesOrderLineStatus.Pending,
                Notes = lineRequest.Notes
            });
        }

        salesOrder.Lines = newLines;

        // Recalculate totals
        var taxAmount = subTotal * (request.TaxRate / 100);
        salesOrder.SubTotal = subTotal;
        salesOrder.TaxAmount = taxAmount;
        salesOrder.TotalAmount = subTotal + taxAmount + request.ShippingCost - request.DiscountAmount;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} updated by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Delete (only Draft status)
    public async Task<bool> DeleteSalesOrderAsync(int businessId, int id, string userId)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            return false;

        if (salesOrder.Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Only draft orders can be deleted");

        _context.SalesOrders.Remove(salesOrder);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} deleted by user {UserId}",
            salesOrder.OrderNumber, userId);

        return true;
    }

    // Workflow: Submit Order
    public async Task<SalesOrderDto> SubmitOrderAsync(
        int businessId,
        int id,
        string userId,
        SubmitOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .Include(so => so.Lines)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Only draft orders can be submitted");

        if (!salesOrder.Lines.Any())
            throw new InvalidOperationException("Cannot submit order without line items");

        salesOrder.Status = SalesOrderStatus.Submitted;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? request.Notes
                : $"{salesOrder.InternalNotes}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} submitted by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Confirm Order
    public async Task<SalesOrderDto> ConfirmOrderAsync(
        int businessId,
        int id,
        string userId,
        ConfirmOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Submitted)
            throw new InvalidOperationException("Only submitted orders can be confirmed");

        salesOrder.Status = SalesOrderStatus.Confirmed;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (request.PromisedDate.HasValue)
            salesOrder.PromisedDate = request.PromisedDate;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? request.Notes
                : $"{salesOrder.InternalNotes}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} confirmed by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Cancel Order
    public async Task<SalesOrderDto> CancelOrderAsync(
        int businessId,
        int id,
        string userId,
        CancelOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status == SalesOrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled");

        if (salesOrder.Status == SalesOrderStatus.Shipped || salesOrder.Status == SalesOrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel shipped or delivered orders");

        salesOrder.Status = SalesOrderStatus.Cancelled;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
            ? $"Cancelled: {request.Reason}"
            : $"{salesOrder.InternalNotes}\nCancelled: {request.Reason}";

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} cancelled by user {UserId}. Reason: {Reason}",
            salesOrder.OrderNumber, userId, request.Reason);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Hold Order
    public async Task<SalesOrderDto> HoldOrderAsync(
        int businessId,
        int id,
        string userId,
        HoldOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status == SalesOrderStatus.Cancelled ||
            salesOrder.Status == SalesOrderStatus.Shipped ||
            salesOrder.Status == SalesOrderStatus.Delivered)
            throw new InvalidOperationException("Cannot hold order in current status");

        if (salesOrder.Status == SalesOrderStatus.OnHold)
            throw new InvalidOperationException("Order is already on hold");

        salesOrder.Status = SalesOrderStatus.OnHold;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
            ? $"On Hold: {request.Reason}"
            : $"{salesOrder.InternalNotes}\nOn Hold: {request.Reason}";

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} placed on hold by user {UserId}. Reason: {Reason}",
            salesOrder.OrderNumber, userId, request.Reason);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Release from Hold
    public async Task<SalesOrderDto> ReleaseOrderAsync(
        int businessId,
        int id,
        string userId,
        ReleaseOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.OnHold)
            throw new InvalidOperationException("Only orders on hold can be released");

        // Return to Confirmed status
        salesOrder.Status = SalesOrderStatus.Confirmed;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? $"Released: {request.Notes}"
                : $"{salesOrder.InternalNotes}\nReleased: {request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} released from hold by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Start Picking
    public async Task<SalesOrderDto> StartPickingAsync(
        int businessId,
        int id,
        string userId,
        StartPickingRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Confirmed &&
            salesOrder.Status != SalesOrderStatus.AwaitingPickup)
            throw new InvalidOperationException("Order must be confirmed before picking");

        salesOrder.Status = SalesOrderStatus.Picking;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? request.Notes
                : $"{salesOrder.InternalNotes}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} picking started by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Complete Picking
    public async Task<SalesOrderDto> CompletePickingAsync(
        int businessId,
        int id,
        string userId,
        CompletePickingRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .Include(so => so.Lines)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Picking)
            throw new InvalidOperationException("Order must be in picking status");

        // Validate all lines have picking data
        foreach (var pickedLine in request.PickedLines)
        {
            var line = salesOrder.Lines.FirstOrDefault(l => l.Id == pickedLine.LineId);
            if (line == null)
                throw new InvalidOperationException($"Line {pickedLine.LineId} not found");

            if (pickedLine.QuantityPicked > line.QuantityOrdered)
                throw new InvalidOperationException(
                    $"Picked quantity cannot exceed ordered quantity for line {pickedLine.LineId}");

            line.QuantityPicked = pickedLine.QuantityPicked;
            line.QuantityOutstanding = line.QuantityOrdered - pickedLine.QuantityPicked;
            line.Location = pickedLine.Location;
            line.PickedBy = userId;
            line.PickedAt = DateTime.UtcNow;
            line.Status = SalesOrderLineStatus.Picked;

            if (!string.IsNullOrWhiteSpace(pickedLine.Notes))
            {
                line.Notes = string.IsNullOrWhiteSpace(line.Notes)
                    ? pickedLine.Notes
                    : $"{line.Notes}\n{pickedLine.Notes}";
            }
        }

        salesOrder.Status = SalesOrderStatus.Picked;
        salesOrder.PickedBy = userId;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? request.Notes
                : $"{salesOrder.InternalNotes}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} picking completed by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Start Packing
    public async Task<SalesOrderDto> StartPackingAsync(
        int businessId,
        int id,
        string userId,
        StartPackingRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Picked)
            throw new InvalidOperationException("Order must be picked before packing");

        salesOrder.Status = SalesOrderStatus.Packing;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? request.Notes
                : $"{salesOrder.InternalNotes}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} packing started by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Complete Packing
    public async Task<SalesOrderDto> CompletePackingAsync(
        int businessId,
        int id,
        string userId,
        CompletePackingRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .Include(so => so.Lines)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Packing)
            throw new InvalidOperationException("Order must be in packing status");

        // Update all lines to Packed status
        foreach (var line in salesOrder.Lines)
        {
            line.Status = SalesOrderLineStatus.Packed;
        }

        salesOrder.Status = SalesOrderStatus.Packed;
        salesOrder.PackedBy = userId;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? request.Notes
                : $"{salesOrder.InternalNotes}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} packing completed by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Ship Order
    public async Task<SalesOrderDto> ShipOrderAsync(
        int businessId,
        int id,
        string userId,
        ShipOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .Include(so => so.Lines)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Packed)
            throw new InvalidOperationException("Order must be packed before shipping");

        // Create stock movements for each line before updating order status
        var stockMovementErrors = new List<string>();
        foreach (var line in salesOrder.Lines)
        {
            if (line.QuantityPicked > 0)
            {
                var createMovementDto = new CreateStockMovementDto
                {
                    ProductId = line.ProductId,
                    MovementType = StockMovementType.StockOut,
                    Quantity = line.QuantityPicked,
                    Reason = $"Sales Order {salesOrder.OrderNumber}",
                    Notes = $"Shipped to {salesOrder.ShipToName ?? "customer"}",
                    FromLocation = line.Location
                };

                var (success, error, _) = await _stockMovementService.CreateMovementAsync(
                    createMovementDto,
                    businessId,
                    userId);

                if (!success)
                {
                    stockMovementErrors.Add($"Product {line.ProductSku}: {error}");
                }
            }
        }

        // If there were any stock movement errors, throw an exception
        if (stockMovementErrors.Any())
        {
            throw new InvalidOperationException(
                $"Failed to create stock movements: {string.Join(", ", stockMovementErrors)}");
        }

        // Update all lines to Shipped status
        foreach (var line in salesOrder.Lines)
        {
            line.QuantityShipped = line.QuantityPicked;
            line.QuantityOutstanding = 0;
            line.Status = SalesOrderLineStatus.Shipped;
        }

        salesOrder.Status = SalesOrderStatus.Shipped;
        salesOrder.ShippedDate = request.ShippedDate;
        salesOrder.Carrier = request.Carrier;
        salesOrder.TrackingNumber = request.TrackingNumber;
        salesOrder.ShippedBy = userId;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? request.Notes
                : $"{salesOrder.InternalNotes}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} shipped by user {UserId}. Carrier: {Carrier}, Tracking: {Tracking}. Stock movements created for {LineCount} lines.",
            salesOrder.OrderNumber, userId, request.Carrier, request.TrackingNumber, salesOrder.Lines.Count);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Workflow: Deliver Order
    public async Task<SalesOrderDto> DeliverOrderAsync(
        int businessId,
        int id,
        string userId,
        DeliverOrderRequest request)
    {
        var salesOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.Id == id)
            .FirstOrDefaultAsync();

        if (salesOrder == null)
            throw new InvalidOperationException("Sales order not found");

        if (salesOrder.Status != SalesOrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be marked as delivered");

        salesOrder.Status = SalesOrderStatus.Delivered;
        salesOrder.DeliveredDate = request.DeliveredDate;
        salesOrder.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.ReceivedBy))
        {
            var note = $"Delivered - Received by: {request.ReceivedBy}";
            if (!string.IsNullOrWhiteSpace(request.Notes))
                note += $" - {request.Notes}";

            salesOrder.InternalNotes = string.IsNullOrWhiteSpace(salesOrder.InternalNotes)
                ? note
                : $"{salesOrder.InternalNotes}\n{note}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Sales order {OrderNumber} marked as delivered by user {UserId}",
            salesOrder.OrderNumber, userId);

        return (await GetSalesOrderByIdAsync(businessId, id))!;
    }

    // Statistics
    public async Task<SalesOrderStatistics> GetStatisticsAsync(int businessId)
    {
        var orders = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        return new SalesOrderStatistics
        {
            TotalOrders = orders.Count,
            DraftOrders = orders.Count(o => o.Status == SalesOrderStatus.Draft),
            SubmittedOrders = orders.Count(o => o.Status == SalesOrderStatus.Submitted),
            ConfirmedOrders = orders.Count(o => o.Status == SalesOrderStatus.Confirmed),
            InProgress = orders.Count(o =>
                o.Status == SalesOrderStatus.Picking ||
                o.Status == SalesOrderStatus.Picked ||
                o.Status == SalesOrderStatus.Packing ||
                o.Status == SalesOrderStatus.Packed),
            ShippedToday = orders.Count(o =>
                o.Status == SalesOrderStatus.Shipped &&
                o.ShippedDate.HasValue &&
                o.ShippedDate.Value.Date == today),
            OverdueOrders = orders.Count(o =>
                o.RequiredDate.HasValue &&
                o.RequiredDate.Value.Date < today &&
                o.Status != SalesOrderStatus.Shipped &&
                o.Status != SalesOrderStatus.Delivered &&
                o.Status != SalesOrderStatus.Cancelled),
            TotalRevenue = orders
                .Where(o => o.Status == SalesOrderStatus.Shipped || o.Status == SalesOrderStatus.Delivered)
                .Sum(o => o.TotalAmount),
            PendingRevenue = orders
                .Where(o => o.Status != SalesOrderStatus.Cancelled &&
                           o.Status != SalesOrderStatus.Shipped &&
                           o.Status != SalesOrderStatus.Delivered)
                .Sum(o => o.TotalAmount)
        };
    }

    // Helper Methods
    private async Task<string> GenerateOrderNumberAsync(int businessId)
    {
        var prefix = "SO";
        var date = DateTime.UtcNow;
        var dateStr = date.ToString("yyyyMMdd");

        var lastOrder = await _context.SalesOrders
            .Where(so => so.BusinessId == businessId && so.OrderNumber.StartsWith($"{prefix}-{dateStr}"))
            .OrderByDescending(so => so.OrderNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastOrder != null)
        {
            var lastSequence = lastOrder.OrderNumber.Split('-').Last();
            if (int.TryParse(lastSequence, out var lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        return $"{prefix}-{dateStr}-{sequence:D4}";
    }

    private IQueryable<SalesOrder> ApplySorting(
        IQueryable<SalesOrder> queryable,
        string? sortBy,
        string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "ordernumber" => isDescending
                ? queryable.OrderByDescending(so => so.OrderNumber)
                : queryable.OrderBy(so => so.OrderNumber),
            "customer" => isDescending
                ? queryable.OrderByDescending(so => so.Customer != null ? so.Customer.Name : "")
                : queryable.OrderBy(so => so.Customer != null ? so.Customer.Name : ""),
            "status" => isDescending
                ? queryable.OrderByDescending(so => so.Status)
                : queryable.OrderBy(so => so.Status),
            "totalamount" => isDescending
                ? queryable.OrderByDescending(so => so.TotalAmount)
                : queryable.OrderBy(so => so.TotalAmount),
            "requireddate" => isDescending
                ? queryable.OrderByDescending(so => so.RequiredDate)
                : queryable.OrderBy(so => so.RequiredDate),
            _ => isDescending
                ? queryable.OrderByDescending(so => so.OrderDate)
                : queryable.OrderBy(so => so.OrderDate)
        };
    }

    private SalesOrderDto MapToDto(SalesOrder salesOrder)
    {
        return new SalesOrderDto
        {
            Id = salesOrder.Id,
            OrderNumber = salesOrder.OrderNumber,
            BusinessId = salesOrder.BusinessId,
            CustomerId = salesOrder.CustomerId,
            CustomerName = salesOrder.Customer?.Name,
            CustomerEmail = salesOrder.Customer?.Email,
            ShipToName = salesOrder.ShipToName,
            ShipToAddress = salesOrder.ShipToAddress,
            ShipToCity = salesOrder.ShipToCity,
            ShipToState = salesOrder.ShipToState,
            ShipToPostalCode = salesOrder.ShipToPostalCode,
            ShipToCountry = salesOrder.ShipToCountry,
            ShipToPhone = salesOrder.ShipToPhone,
            SubTotal = salesOrder.SubTotal,
            TaxAmount = salesOrder.TaxAmount,
            TaxRate = salesOrder.TaxRate,
            ShippingCost = salesOrder.ShippingCost,
            DiscountAmount = salesOrder.DiscountAmount,
            TotalAmount = salesOrder.TotalAmount,
            Status = salesOrder.Status,
            StatusDisplay = salesOrder.Status.ToString(),
            Priority = salesOrder.Priority,
            PriorityDisplay = salesOrder.Priority.ToString(),
            OrderDate = salesOrder.OrderDate,
            RequiredDate = salesOrder.RequiredDate,
            PromisedDate = salesOrder.PromisedDate,
            ShippedDate = salesOrder.ShippedDate,
            DeliveredDate = salesOrder.DeliveredDate,
            ShippingMethod = salesOrder.ShippingMethod,
            TrackingNumber = salesOrder.TrackingNumber,
            Carrier = salesOrder.Carrier,
            PickedBy = salesOrder.PickedBy,
            PackedBy = salesOrder.PackedBy,
            ShippedBy = salesOrder.ShippedBy,
            CustomerReference = salesOrder.CustomerReference,
            Notes = salesOrder.Notes,
            InternalNotes = salesOrder.InternalNotes,
            CreatedBy = salesOrder.CreatedBy,
            CreatedAt = salesOrder.CreatedAt,
            UpdatedAt = salesOrder.UpdatedAt,
            Lines = salesOrder.Lines.Select(l => new SalesOrderLineDto
            {
                Id = l.Id,
                SalesOrderId = l.SalesOrderId,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                ProductSku = l.ProductSku,
                ProductDescription = l.Product?.Description,
                QuantityOrdered = l.QuantityOrdered,
                QuantityPicked = l.QuantityPicked,
                QuantityShipped = l.QuantityShipped,
                QuantityOutstanding = l.QuantityOutstanding,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                LineTotal = l.LineTotal,
                Status = l.Status,
                StatusDisplay = l.Status.ToString(),
                Location = l.Location,
                PickedBy = l.PickedBy,
                PickedAt = l.PickedAt,
                Notes = l.Notes
            }).ToList(),
            TotalLineItems = salesOrder.Lines.Count,
            TotalQuantityOrdered = salesOrder.Lines.Sum(l => l.QuantityOrdered),
            TotalQuantityPicked = salesOrder.Lines.Sum(l => l.QuantityPicked),
            TotalQuantityShipped = salesOrder.Lines.Sum(l => l.QuantityShipped),
            TotalQuantityOutstanding = salesOrder.Lines.Sum(l => l.QuantityOutstanding)
        };
    }
}
