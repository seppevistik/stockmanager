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

    public async Task<IEnumerable<ProductDto>> GetProductsByBusinessAsync(int businessId)
    {
        var products = await _unitOfWork.Products.GetByBusinessIdAsync(businessId);
        return products.Select(p => MapToDto(p));
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, int businessId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null || product.BusinessId != businessId)
            return null;

        return MapToDto(product);
    }

    public async Task<(bool Success, string? Error, ProductDto? Product)> CreateProductAsync(CreateProductDto createDto, int businessId, string userId)
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

    public async Task<(bool Success, string? Error)> UpdateProductAsync(int id, CreateProductDto updateDto, int businessId)
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

    public async Task<(bool Success, string? Error)> DeleteProductAsync(int id, int businessId)
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

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync(int businessId)
    {
        var products = await _unitOfWork.Products.GetLowStockProductsAsync(businessId);
        return products.Select(p => MapToDto(p));
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
            CreatedAt = product.CreatedAt
        };
    }
}
