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

public class PurchaseOrdersControllerTests
{
    private readonly Mock<PurchaseOrderService> _mockService;
    private readonly PurchaseOrdersController _controller;
    private readonly int _testBusinessId = 1;
    private readonly string _testUserId = "test-user-id";

    public PurchaseOrdersControllerTests()
    {
        _mockService = new Mock<PurchaseOrderService>(MockBehavior.Default, null!);
        _controller = new PurchaseOrdersController(_mockService.Object);

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

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithNoFilter_ReturnsOkWithAllPurchaseOrders()
    {
        // Arrange
        var expectedOrders = CreateTestPurchaseOrders();
        _mockService
            .Setup(s => s.GetPurchaseOrdersAsync(_testBusinessId, null))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetAll(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<IEnumerable<PurchaseOrderDto>>().Subject;
        orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var filter = new PurchaseOrderFilterDto { Status = PurchaseOrderStatus.Draft };
        var expectedOrders = CreateTestPurchaseOrders().Where(o => o.Status == PurchaseOrderStatus.Draft);

        _mockService
            .Setup(s => s.GetPurchaseOrdersAsync(_testBusinessId, filter))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetAll(filter);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<IEnumerable<PurchaseOrderDto>>().Subject;
        orders.Should().HaveCount(1);
        orders.Should().OnlyContain(o => o.Status == PurchaseOrderStatus.Draft);
    }

    #endregion

    #region GetOutstanding Tests

    [Fact]
    public async Task GetOutstanding_ReturnsOkWithOutstandingOrders()
    {
        // Arrange
        var expectedOrders = CreateTestPurchaseOrders()
            .Where(o => o.Status == PurchaseOrderStatus.Confirmed);

        _mockService
            .Setup(s => s.GetOutstandingOrdersAsync(_testBusinessId))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetOutstanding();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<IEnumerable<PurchaseOrderDto>>().Subject;
        orders.Should().NotBeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithPurchaseOrder()
    {
        // Arrange
        var expectedOrder = CreateTestPurchaseOrders().First();
        _mockService
            .Setup(s => s.GetPurchaseOrderByIdAsync(1, _testBusinessId))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var order = okResult.Value.Should().BeAssignableTo<PurchaseOrderDto>().Subject;
        order.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetPurchaseOrderByIdAsync(999, _testBusinessId))
            .ReturnsAsync((PurchaseOrderDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = CreateValidCreateDto();
        var createdOrder = CreateTestPurchaseOrders().First();

        _mockService
            .Setup(s => s.CreatePurchaseOrderAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((true, null, createdOrder));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.RouteValues!["id"].Should().Be(createdOrder.Id);
        var returnedOrder = createdResult.Value.Should().BeAssignableTo<PurchaseOrderDto>().Subject;
        returnedOrder.Id.Should().Be(createdOrder.Id);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = CreateValidCreateDto();
        var errorMessage = "Company not found";

        _mockService
            .Setup(s => s.CreatePurchaseOrderAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((false, errorMessage, null));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdatePurchaseOrderRequest
        {
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            TaxAmount = 100m,
            ShippingCost = 50m
        };

        _mockService
            .Setup(s => s.UpdatePurchaseOrderAsync(1, updateDto, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdatePurchaseOrderRequest
        {
            TaxAmount = 100m,
            ShippingCost = 50m
        };
        var errorMessage = "Purchase order not found";

        _mockService
            .Setup(s => s.UpdatePurchaseOrderAsync(999, updateDto, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Submit Tests

    [Fact]
    public async Task Submit_WithValidId_ReturnsOk()
    {
        // Arrange
        _mockService
            .Setup(s => s.SubmitPurchaseOrderAsync(1, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Submit(1);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Submit_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Only draft purchase orders can be submitted";

        _mockService
            .Setup(s => s.SubmitPurchaseOrderAsync(1, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Submit(1);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public async Task Confirm_WithValidData_ReturnsOk()
    {
        // Arrange
        var confirmDto = new ConfirmPurchaseOrderRequest
        {
            ConfirmedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        _mockService
            .Setup(s => s.ConfirmPurchaseOrderAsync(1, confirmDto, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Confirm(1, confirmDto);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Confirm_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var confirmDto = new ConfirmPurchaseOrderRequest
        {
            ConfirmedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };
        var errorMessage = "Only submitted purchase orders can be confirmed";

        _mockService
            .Setup(s => s.ConfirmPurchaseOrderAsync(1, confirmDto, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Confirm(1, confirmDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_WithValidData_ReturnsOk()
    {
        // Arrange
        var cancelDto = new CancelPurchaseOrderRequest
        {
            Reason = "Supplier cannot fulfill order"
        };

        _mockService
            .Setup(s => s.CancelPurchaseOrderAsync(1, cancelDto, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Cancel(1, cancelDto);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Cancel_WithoutReason_ReturnsBadRequest()
    {
        // Arrange
        var cancelDto = new CancelPurchaseOrderRequest
        {
            Reason = ""
        };
        var errorMessage = "Cancellation reason is required";

        _mockService
            .Setup(s => s.CancelPurchaseOrderAsync(1, cancelDto, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Cancel(1, cancelDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _mockService
            .Setup(s => s.DeletePurchaseOrderAsync(1, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Only draft purchase orders can be deleted";

        _mockService
            .Setup(s => s.DeletePurchaseOrderAsync(1, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Helper Methods

    private List<PurchaseOrderDto> CreateTestPurchaseOrders()
    {
        return new List<PurchaseOrderDto>
        {
            new PurchaseOrderDto
            {
                Id = 1,
                BusinessId = _testBusinessId,
                CompanyId = 1,
                CompanyName = "Test Supplier",
                OrderNumber = "PO-2024-0001",
                OrderDate = DateTime.UtcNow,
                Status = PurchaseOrderStatus.Draft,
                SubTotal = 1000m,
                TaxAmount = 100m,
                ShippingCost = 50m,
                TotalAmount = 1150m,
                CreatedBy = _testUserId,
                CreatedByName = "Test User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Lines = new List<PurchaseOrderLineDto>()
            },
            new PurchaseOrderDto
            {
                Id = 2,
                BusinessId = _testBusinessId,
                CompanyId = 1,
                CompanyName = "Test Supplier",
                OrderNumber = "PO-2024-0002",
                OrderDate = DateTime.UtcNow,
                Status = PurchaseOrderStatus.Confirmed,
                SubTotal = 2000m,
                TaxAmount = 200m,
                ShippingCost = 75m,
                TotalAmount = 2275m,
                CreatedBy = _testUserId,
                CreatedByName = "Test User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Lines = new List<PurchaseOrderLineDto>()
            }
        };
    }

    private CreatePurchaseOrderRequest CreateValidCreateDto()
    {
        return new CreatePurchaseOrderRequest
        {
            CompanyId = 1,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            TaxAmount = 100m,
            ShippingCost = 50m,
            Notes = "Test purchase order",
            Lines = new List<CreatePurchaseOrderLine>
            {
                new CreatePurchaseOrderLine
                {
                    ProductId = 1,
                    QuantityOrdered = 10,
                    UnitPrice = 100m
                }
            }
        };
    }

    #endregion
}
