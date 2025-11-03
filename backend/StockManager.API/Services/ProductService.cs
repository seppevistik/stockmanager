using AutoMapper;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;

namespace StockManager.API.Services;

public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public virtual async Task<IEnumerable<ProductDto>> GetProductsByBusinessAsync(int businessId)
    {
        var products = await _unitOfWork.Products.GetByBusinessIdAsync(businessId);
        return products.Select(p => MapToDto(p));
    }

    public virtual async Task<ProductDto?> GetProductByIdAsync(int id, int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || product.BusinessId != businessId)
            return null;

        return MapToDto(product);
    }

    public virtual async Task<(bool Success, string? Error, ProductDto? Product)> CreateProductAsync(CreateProductDto createDto, int businessId, string userId)
    {
        // Check if SKU already exists
        var existingProduct = await _unitOfWork.Products.GetBySkuAsync(createDto.SKU, businessId);
        if (existingProduct != null)
        {
            return (false, "A product with this SKU already exists", null);
        }

        var product = new Product
        {
            Name = createDto.Name,
            Description = createDto.Description,
            SKU = createDto.SKU,
            CategoryId = createDto.CategoryId,
            UnitOfMeasurement = createDto.UnitOfMeasurement,
            ImageUrl = createDto.ImageUrl,
            Supplier = createDto.Supplier,
            MinimumStockLevel = createDto.MinimumStockLevel,
            CurrentStock = createDto.InitialStock,
            CostPerUnit = createDto.CostPerUnit,
            Location = createDto.Location,
            Status = ProductStatus.Active,
            BusinessId = businessId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Products.AddAsync(product);

        // Create initial stock movement if there's initial stock
        if (createDto.InitialStock > 0)
        {
            var stockMovement = new StockMovement
            {
                ProductId = product.Id,
                MovementType = StockMovementType.StockIn,
                Quantity = createDto.InitialStock,
                PreviousStock = 0,
                NewStock = createDto.InitialStock,
                Reason = "Initial stock",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.StockMovements.AddAsync(stockMovement);
        }

        await _unitOfWork.SaveChangesAsync();

        return (true, null, MapToDto(product));
    }

    public virtual async Task<(bool Success, string? Error)> UpdateProductAsync(int id, CreateProductDto updateDto, int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || product.BusinessId != businessId)
        {
            return (false, "Product not found");
        }

        // Check SKU uniqueness if changed
        if (product.SKU != updateDto.SKU)
        {
            var existingProduct = await _unitOfWork.Products.GetBySkuAsync(updateDto.SKU, businessId);
            if (existingProduct != null)
            {
                return (false, "A product with this SKU already exists");
            }
        }

        product.Name = updateDto.Name;
        product.Description = updateDto.Description;
        product.SKU = updateDto.SKU;
        product.CategoryId = updateDto.CategoryId;
        product.UnitOfMeasurement = updateDto.UnitOfMeasurement;
        product.ImageUrl = updateDto.ImageUrl;
        product.Supplier = updateDto.Supplier;
        product.MinimumStockLevel = updateDto.MinimumStockLevel;
        product.CostPerUnit = updateDto.CostPerUnit;
        product.Location = updateDto.Location;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> DeleteProductAsync(int id, int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || product.BusinessId != businessId)
        {
            return (false, "Product not found");
        }

        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync(int businessId)
    {
        var products = await _unitOfWork.Products.GetLowStockProductsAsync(businessId);
        return products.Select(p => MapToDto(p));
    }

    public virtual async Task<(bool Success, string? Error, int UpdatedCount)> BulkAdjustStockAsync(
        BulkStockAdjustmentDto adjustmentDto,
        int businessId,
        string userId)
    {
        if (adjustmentDto.Adjustments == null || !adjustmentDto.Adjustments.Any())
        {
            return (false, "No adjustments provided", 0);
        }

        var updatedCount = 0;
        var errors = new List<string>();

        foreach (var adjustment in adjustmentDto.Adjustments)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(adjustment.ProductId);

            if (product == null || product.BusinessId != businessId)
            {
                errors.Add($"Product with ID {adjustment.ProductId} not found");
                continue;
            }

            var previousStock = product.CurrentStock;
            var difference = adjustment.NewStock - previousStock;

            // Update the current stock
            product.CurrentStock = adjustment.NewStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Products.UpdateAsync(product);

            // Create stock movement for audit trail
            var stockMovement = new StockMovement
            {
                ProductId = product.Id,
                MovementType = StockMovementType.StockAdjustment,
                Quantity = Math.Abs(difference),
                PreviousStock = previousStock,
                NewStock = adjustment.NewStock,
                Reason = adjustmentDto.Reason,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.StockMovements.AddAsync(stockMovement);
            updatedCount++;
        }

        await _unitOfWork.SaveChangesAsync();

        if (errors.Any() && updatedCount == 0)
        {
            return (false, string.Join("; ", errors), 0);
        }

        return (true, errors.Any() ? string.Join("; ", errors) : null, updatedCount);
    }

    public virtual async Task<(bool Success, string? Error)> AddProductSupplierAsync(
        int productId,
        CreateProductSupplierDto supplierDto,
        int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null || product.BusinessId != businessId)
        {
            return (false, "Product not found");
        }

        var company = await _unitOfWork.Companies.GetByIdAsync(supplierDto.CompanyId);
        if (company == null || company.BusinessId != businessId || !company.IsSupplier)
        {
            return (false, "Invalid supplier");
        }

        // Check if supplier already linked
        var existingLink = product.ProductSuppliers.FirstOrDefault(ps => ps.CompanyId == supplierDto.CompanyId);
        if (existingLink != null)
        {
            return (false, "Supplier already linked to this product");
        }

        // If this is marked as primary, remove primary flag from others
        if (supplierDto.IsPrimarySupplier)
        {
            foreach (var ps in product.ProductSuppliers)
            {
                ps.IsPrimarySupplier = false;
            }
        }

        var productSupplier = new ProductSupplier
        {
            ProductId = productId,
            CompanyId = supplierDto.CompanyId,
            SupplierPrice = supplierDto.SupplierPrice,
            SupplierProductCode = supplierDto.SupplierProductCode,
            LeadTimeDays = supplierDto.LeadTimeDays,
            MinimumOrderQuantity = supplierDto.MinimumOrderQuantity,
            IsPrimarySupplier = supplierDto.IsPrimarySupplier,
            CreatedAt = DateTime.UtcNow
        };

        product.ProductSuppliers.Add(productSupplier);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> UpdateProductSupplierAsync(
        int productId,
        int supplierLinkId,
        CreateProductSupplierDto supplierDto,
        int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null || product.BusinessId != businessId)
        {
            return (false, "Product not found");
        }

        var productSupplier = product.ProductSuppliers.FirstOrDefault(ps => ps.Id == supplierLinkId);
        if (productSupplier == null)
        {
            return (false, "Supplier link not found");
        }

        // If this is being marked as primary, remove primary flag from others
        if (supplierDto.IsPrimarySupplier && !productSupplier.IsPrimarySupplier)
        {
            foreach (var ps in product.ProductSuppliers.Where(ps => ps.Id != supplierLinkId))
            {
                ps.IsPrimarySupplier = false;
            }
        }

        productSupplier.SupplierPrice = supplierDto.SupplierPrice;
        productSupplier.SupplierProductCode = supplierDto.SupplierProductCode;
        productSupplier.LeadTimeDays = supplierDto.LeadTimeDays;
        productSupplier.MinimumOrderQuantity = supplierDto.MinimumOrderQuantity;
        productSupplier.IsPrimarySupplier = supplierDto.IsPrimarySupplier;
        productSupplier.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public virtual async Task<(bool Success, string? Error)> RemoveProductSupplierAsync(
        int productId,
        int supplierLinkId,
        int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null || product.BusinessId != businessId)
        {
            return (false, "Product not found");
        }

        var productSupplier = product.ProductSuppliers.FirstOrDefault(ps => ps.Id == supplierLinkId);
        if (productSupplier == null)
        {
            return (false, "Supplier link not found");
        }

        product.ProductSuppliers.Remove(productSupplier);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    private ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            UnitOfMeasurement = product.UnitOfMeasurement,
            ImageUrl = product.ImageUrl,
            Supplier = product.Supplier,
            MinimumStockLevel = product.MinimumStockLevel,
            CurrentStock = product.CurrentStock,
            CostPerUnit = product.CostPerUnit,
            TotalValue = product.CurrentStock * product.CostPerUnit,
            Status = product.Status,
            Location = product.Location,
            CreatedAt = product.CreatedAt,
            Suppliers = product.ProductSuppliers?.Select(ps => new ProductSupplierDto
            {
                Id = ps.Id,
                ProductId = ps.ProductId,
                CompanyId = ps.CompanyId,
                CompanyName = ps.Company?.Name ?? "",
                SupplierPrice = ps.SupplierPrice,
                SupplierProductCode = ps.SupplierProductCode,
                LeadTimeDays = ps.LeadTimeDays,
                MinimumOrderQuantity = ps.MinimumOrderQuantity,
                IsPrimarySupplier = ps.IsPrimarySupplier
            }).ToList() ?? new List<ProductSupplierDto>()
        };
    }
}
