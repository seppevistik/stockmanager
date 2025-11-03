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

public class StockMovementsControllerTests
{
    private readonly Mock<StockMovementService> _mockStockMovementService;
    private readonly StockMovementsController _controller;
    private readonly int _testBusinessId = 1;
    private readonly string _testUserId = "test-user-id";

    public StockMovementsControllerTests()
    {
        _mockStockMovementService = new Mock<StockMovementService>(MockBehavior.Default, null!);
        _controller = new StockMovementsController(_mockStockMovementService.Object);

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
    public async Task GetAll_WithoutDateRange_ReturnsAllMovements()
    {
        // Arrange
        var expectedMovements = new List<StockMovementDto>
        {
            new StockMovementDto
            {
                Id = 1,
                ProductId = 1,
                ProductName = "Product 1",
                MovementType = StockMovementType.StockIn,
                Quantity = 100,
                CreatedAt = DateTime.UtcNow
            },
            new StockMovementDto
            {
                Id = 2,
                ProductId = 2,
                ProductName = "Product 2",
                MovementType = StockMovementType.StockOut,
                Quantity = 50,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockStockMovementService
            .Setup(s => s.GetMovementsByBusinessAsync(_testBusinessId, null, null))
            .ReturnsAsync(expectedMovements);

        // Act
        var result = await _controller.GetAll(null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var movements = okResult.Value.Should().BeAssignableTo<IEnumerable<StockMovementDto>>().Subject;
        movements.Should().HaveCount(2);
        movements.Should().BeEquivalentTo(expectedMovements);
    }

    [Fact]
    public async Task GetAll_WithDateRange_ReturnsFilteredMovements()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        var expectedMovements = new List<StockMovementDto>
        {
            new StockMovementDto
            {
                Id = 1,
                ProductId = 1,
                ProductName = "Product 1",
                MovementType = StockMovementType.StockIn,
                Quantity = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        _mockStockMovementService
            .Setup(s => s.GetMovementsByBusinessAsync(_testBusinessId, startDate, endDate))
            .ReturnsAsync(expectedMovements);

        // Act
        var result = await _controller.GetAll(startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var movements = okResult.Value.Should().BeAssignableTo<IEnumerable<StockMovementDto>>().Subject;
        movements.Should().HaveCount(1);
        movements.Should().BeEquivalentTo(expectedMovements);

        // Verify the service was called with the correct date range
        _mockStockMovementService.Verify(
            s => s.GetMovementsByBusinessAsync(_testBusinessId, startDate, endDate),
            Times.Once);
    }

    [Fact]
    public async Task GetByProduct_WithValidProductId_ReturnsProductMovements()
    {
        // Arrange
        var productId = 1;
        var expectedMovements = new List<StockMovementDto>
        {
            new StockMovementDto
            {
                Id = 1,
                ProductId = productId,
                ProductName = "Product 1",
                MovementType = StockMovementType.StockIn,
                Quantity = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new StockMovementDto
            {
                Id = 2,
                ProductId = productId,
                ProductName = "Product 1",
                MovementType = StockMovementType.StockOut,
                Quantity = 30,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        _mockStockMovementService
            .Setup(s => s.GetMovementsByProductAsync(productId, _testBusinessId))
            .ReturnsAsync(expectedMovements);

        // Act
        var result = await _controller.GetByProduct(productId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var movements = okResult.Value.Should().BeAssignableTo<IEnumerable<StockMovementDto>>().Subject;
        movements.Should().HaveCount(2);
        movements.Should().AllSatisfy(m => m.ProductId.Should().Be(productId));
    }

    [Fact]
    public async Task GetByProduct_WithNoMovements_ReturnsEmptyList()
    {
        // Arrange
        var productId = 999;
        var expectedMovements = new List<StockMovementDto>();

        _mockStockMovementService
            .Setup(s => s.GetMovementsByProductAsync(productId, _testBusinessId))
            .ReturnsAsync(expectedMovements);

        // Act
        var result = await _controller.GetByProduct(productId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var movements = okResult.Value.Should().BeAssignableTo<IEnumerable<StockMovementDto>>().Subject;
        movements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecent_WithDefaultCount_Returns10Movements()
    {
        // Arrange
        var expectedMovements = Enumerable.Range(1, 10).Select(i => new StockMovementDto
        {
            Id = i,
            ProductId = i,
            ProductName = $"Product {i}",
            MovementType = StockMovementType.StockIn,
            Quantity = i * 10,
            CreatedAt = DateTime.UtcNow.AddHours(-i)
        }).ToList();

        _mockStockMovementService
            .Setup(s => s.GetRecentMovementsAsync(_testBusinessId, 10))
            .ReturnsAsync(expectedMovements);

        // Act
        var result = await _controller.GetRecent();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var movements = okResult.Value.Should().BeAssignableTo<IEnumerable<StockMovementDto>>().Subject;
        movements.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetRecent_WithCustomCount_ReturnsRequestedNumberOfMovements()
    {
        // Arrange
        var requestedCount = 5;
        var expectedMovements = Enumerable.Range(1, requestedCount).Select(i => new StockMovementDto
        {
            Id = i,
            ProductId = i,
            ProductName = $"Product {i}",
            MovementType = StockMovementType.StockIn,
            Quantity = i * 10,
            CreatedAt = DateTime.UtcNow.AddHours(-i)
        }).ToList();

        _mockStockMovementService
            .Setup(s => s.GetRecentMovementsAsync(_testBusinessId, requestedCount))
            .ReturnsAsync(expectedMovements);

        // Act
        var result = await _controller.GetRecent(requestedCount);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var movements = okResult.Value.Should().BeAssignableTo<IEnumerable<StockMovementDto>>().Subject;
        movements.Should().HaveCount(requestedCount);

        // Verify the service was called with the correct count
        _mockStockMovementService.Verify(
            s => s.GetRecentMovementsAsync(_testBusinessId, requestedCount),
            Times.Once);
    }

    [Fact]
    public async Task Create_WithValidMovement_ReturnsOkWithMovement()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            MovementType = StockMovementType.StockIn,
            Quantity = 100,
            Reason = "Supplier delivery"
        };

        var createdMovement = new StockMovementDto
        {
            Id = 1,
            ProductId = createDto.ProductId,
            ProductName = "Product 1",
            MovementType = createDto.MovementType,
            Quantity = createDto.Quantity,
            PreviousStock = 50,
            NewStock = 150,
            Reason = createDto.Reason,
            UserName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        _mockStockMovementService
            .Setup(s => s.CreateMovementAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((true, null, createdMovement));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var movement = okResult.Value.Should().BeAssignableTo<StockMovementDto>().Subject;
        movement.Should().BeEquivalentTo(createdMovement);
    }

    [Fact]
    public async Task Create_WithInvalidProduct_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 999,
            MovementType = StockMovementType.StockIn,
            Quantity = 100,
            Reason = "Test"
        };

        _mockStockMovementService
            .Setup(s => s.CreateMovementAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((false, "Product not found", null));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            MovementType = StockMovementType.StockOut,
            Quantity = 1000, // More than available
            Reason = "Sale"
        };

        _mockStockMovementService
            .Setup(s => s.CreateMovementAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((false, "Insufficient stock", null));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData(StockMovementType.StockIn)]
    [InlineData(StockMovementType.StockOut)]
    [InlineData(StockMovementType.StockAdjustment)]
    [InlineData(StockMovementType.StockTransfer)]
    public async Task Create_WithDifferentMovementTypes_CallsServiceCorrectly(StockMovementType movementType)
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            MovementType = movementType,
            Quantity = 50,
            Reason = $"Test {movementType}"
        };

        var createdMovement = new StockMovementDto
        {
            Id = 1,
            ProductId = createDto.ProductId,
            MovementType = movementType,
            Quantity = createDto.Quantity,
            CreatedAt = DateTime.UtcNow
        };

        _mockStockMovementService
            .Setup(s => s.CreateMovementAsync(It.IsAny<CreateStockMovementDto>(), _testBusinessId, _testUserId))
            .ReturnsAsync((true, null, createdMovement));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify the service was called with the correct movement type
        _mockStockMovementService.Verify(
            s => s.CreateMovementAsync(
                It.Is<CreateStockMovementDto>(dto => dto.MovementType == movementType),
                _testBusinessId,
                _testUserId),
            Times.Once);
    }
}
