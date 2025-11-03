using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StockManager.API.Controllers;
using StockManager.API.Services;
using StockManager.Core.DTOs;
using StockManager.Core.Enums;
using Xunit;

namespace StockManager.API.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<ProductService> _mockProductService;
    private readonly ProductsController _controller;
    private readonly int _testBusinessId = 1;
    private readonly string _testUserId = "test-user-id";

    public ProductsControllerTests()
    {
        _mockProductService = new Mock<ProductService>(MockBehavior.Default, null!, null!);
        _controller = new ProductsController(_mockProductService.Object);

        // Setup HttpContext with user claims
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("BusinessId", _testBusinessId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _testUserId)
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Product 1", SKU = "SKU001" },
            new ProductDto { Id = 2, Name = "Product 2", SKU = "SKU002" }
        };

        _mockProductService
            .Setup(s => s.GetProductsByBusinessAsync(_testBusinessId))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var products = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        products.Should().HaveCount(2);
        products.Should().BeEquivalentTo(expectedProducts);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithProduct()
    {
        // Arrange
        var productId = 1;
        var expectedProduct = new ProductDto { Id = productId, Name = "Test Product", SKU = "SKU001" };

        _mockProductService
            .Setup(s => s.GetProductByIdAsync(productId, _testBusinessId))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.GetById(productId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var product = okResult.Value.Should().BeAssignableTo<ProductDto>().Subject;
        product.Should().BeEquivalentTo(expectedProduct);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;

        _mockProductService
            .Setup(s => s.GetProductByIdAsync(productId, _testBusinessId))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.GetById(productId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WithValidProduct_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "New Product",
            SKU = "SKU003",
            UnitOfMeasurement = "pieces",
            MinimumStockLevel = 10,
            InitialStock = 100,
            CostPerUnit = 25.50m
        };

        var createdProduct = new ProductDto
        {
            Id = 3,
            Name = createDto.Name,
            SKU = createDto.SKU,
            CurrentStock = createDto.InitialStock
        };

        _mockProductService
            .Setup(s => s.CreateProductAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((true, null, createdProduct));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ProductsController.GetById));
        var product = createdResult.Value.Should().BeAssignableTo<ProductDto>().Subject;
        product.Should().BeEquivalentTo(createdProduct);
    }

    [Fact]
    public async Task Create_WithDuplicateSKU_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "New Product",
            SKU = "SKU001", // Duplicate SKU
            UnitOfMeasurement = "pieces"
        };

        _mockProductService
            .Setup(s => s.CreateProductAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((false, "A product with this SKU already exists", null));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithValidProduct_ReturnsNoContent()
    {
        // Arrange
        var productId = 1;
        var updateDto = new CreateProductDto
        {
            Name = "Updated Product",
            SKU = "SKU001",
            UnitOfMeasurement = "pieces",
            MinimumStockLevel = 10,
            CostPerUnit = 30.00m
        };

        _mockProductService
            .Setup(s => s.UpdateProductAsync(productId, updateDto, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Update(productId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_WithInvalidProduct_ReturnsBadRequest()
    {
        // Arrange
        var productId = 999;
        var updateDto = new CreateProductDto
        {
            Name = "Updated Product",
            SKU = "SKU999",
            UnitOfMeasurement = "pieces"
        };

        _mockProductService
            .Setup(s => s.UpdateProductAsync(productId, updateDto, _testBusinessId))
            .ReturnsAsync((false, "Product not found"));

        // Act
        var result = await _controller.Update(productId, updateDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WithValidProduct_ReturnsNoContent()
    {
        // Arrange
        var productId = 1;

        _mockProductService
            .Setup(s => s.DeleteProductAsync(productId, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Delete(productId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithInvalidProduct_ReturnsBadRequest()
    {
        // Arrange
        var productId = 999;

        _mockProductService
            .Setup(s => s.DeleteProductAsync(productId, _testBusinessId))
            .ReturnsAsync((false, "Product not found"));

        // Act
        var result = await _controller.Delete(productId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkAdjustStock_WithValidAdjustments_ReturnsOkWithCount()
    {
        // Arrange
        var bulkAdjustmentDto = new BulkStockAdjustmentDto
        {
            Adjustments = new List<StockAdjustmentDto>
            {
                new StockAdjustmentDto { ProductId = 1, NewStock = 150 },
                new StockAdjustmentDto { ProductId = 2, NewStock = 200 }
            },
            Reason = "Monthly inventory count"
        };

        _mockProductService
            .Setup(s => s.BulkAdjustStockAsync(bulkAdjustmentDto, _testBusinessId, _testUserId))
            .ReturnsAsync((true, null, 2));

        // Act
        var result = await _controller.BulkAdjustStock(bulkAdjustmentDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        // Verify the service was called with correct parameters
        _mockProductService.Verify(
            s => s.BulkAdjustStockAsync(bulkAdjustmentDto, _testBusinessId, _testUserId),
            Times.Once);
    }

    [Fact]
    public async Task BulkAdjustStock_WithNoAdjustments_ReturnsBadRequest()
    {
        // Arrange
        var bulkAdjustmentDto = new BulkStockAdjustmentDto
        {
            Adjustments = new List<StockAdjustmentDto>(),
            Reason = "Test"
        };

        _mockProductService
            .Setup(s => s.BulkAdjustStockAsync(bulkAdjustmentDto, _testBusinessId, _testUserId))
            .ReturnsAsync((false, "No adjustments provided", 0));

        // Act
        var result = await _controller.BulkAdjustStock(bulkAdjustmentDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkAdjustStock_WithInvalidProduct_ReturnsBadRequest()
    {
        // Arrange
        var bulkAdjustmentDto = new BulkStockAdjustmentDto
        {
            Adjustments = new List<StockAdjustmentDto>
            {
                new StockAdjustmentDto { ProductId = 999, NewStock = 150 }
            },
            Reason = "Test"
        };

        _mockProductService
            .Setup(s => s.BulkAdjustStockAsync(bulkAdjustmentDto, _testBusinessId, _testUserId))
            .ReturnsAsync((false, "Product with ID 999 not found", 0));

        // Act
        var result = await _controller.BulkAdjustStock(bulkAdjustmentDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLowStock_ReturnsOkWithLowStockProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Low Stock Product 1", CurrentStock = 5, MinimumStockLevel = 10 },
            new ProductDto { Id = 2, Name = "Low Stock Product 2", CurrentStock = 2, MinimumStockLevel = 20 }
        };

        _mockProductService
            .Setup(s => s.GetLowStockProductsAsync(_testBusinessId))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetLowStock();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var products = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        products.Should().HaveCount(2);
        products.Should().AllSatisfy(p => p.CurrentStock.Should().BeLessThan(p.MinimumStockLevel));
    }
}
