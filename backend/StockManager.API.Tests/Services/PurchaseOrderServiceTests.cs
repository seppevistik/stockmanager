using FluentAssertions;
using Moq;
using StockManager.API.Services;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;
using Xunit;

namespace StockManager.API.Tests.Services;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPurchaseOrderRepository> _mockPurchaseOrderRepository;
    private readonly Mock<ICompanyRepository> _mockCompanyRepository;
    private readonly PurchaseOrderService _service;
    private readonly int _testBusinessId = 1;
    private readonly string _testUserId = "test-user-123";

    public PurchaseOrderServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPurchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
        _mockCompanyRepository = new Mock<ICompanyRepository>();

        _mockUnitOfWork.Setup(u => u.PurchaseOrders).Returns(_mockPurchaseOrderRepository.Object);
        _mockUnitOfWork.Setup(u => u.Companies).Returns(_mockCompanyRepository.Object);

        _service = new PurchaseOrderService(_mockUnitOfWork.Object);
    }

    #region GetPurchaseOrdersAsync Tests

    [Fact]
    public async Task GetPurchaseOrdersAsync_WithNoFilter_ReturnsAllPurchaseOrders()
    {
        // Arrange
        var purchaseOrders = CreateTestPurchaseOrders();
        _mockPurchaseOrderRepository
            .Setup(r => r.GetByBusinessIdWithDetailsAsync(_testBusinessId))
            .ReturnsAsync(purchaseOrders);

        // Act
        var result = await _service.GetPurchaseOrdersAsync(_testBusinessId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(po => po.BusinessId == _testBusinessId);
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_WithStatusFilter_ReturnsFilteredPurchaseOrders()
    {
        // Arrange
        var purchaseOrders = CreateTestPurchaseOrders().Where(po => po.Status == PurchaseOrderStatus.Draft);
        var filter = new PurchaseOrderFilterDto { Status = PurchaseOrderStatus.Draft };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByStatusAsync(_testBusinessId, PurchaseOrderStatus.Draft))
            .ReturnsAsync(purchaseOrders);

        // Act
        var result = await _service.GetPurchaseOrdersAsync(_testBusinessId, filter);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(po => po.Status == PurchaseOrderStatus.Draft);
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_WithCompanyFilter_ReturnsFilteredPurchaseOrders()
    {
        // Arrange
        var companyId = 1;
        var purchaseOrders = CreateTestPurchaseOrders().Where(po => po.CompanyId == companyId);
        var filter = new PurchaseOrderFilterDto { CompanyId = companyId };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetBySupplierAsync(_testBusinessId, companyId))
            .ReturnsAsync(purchaseOrders);

        // Act
        var result = await _service.GetPurchaseOrdersAsync(_testBusinessId, filter);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(po => po.CompanyId == companyId);
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_WithSearchFilter_ReturnsMatchingPurchaseOrders()
    {
        // Arrange
        var searchTerm = "PO-2024";
        var purchaseOrders = CreateTestPurchaseOrders();
        var filter = new PurchaseOrderFilterDto { Search = searchTerm };

        _mockPurchaseOrderRepository
            .Setup(r => r.SearchAsync(_testBusinessId, searchTerm))
            .ReturnsAsync(purchaseOrders);

        // Act
        var result = await _service.GetPurchaseOrdersAsync(_testBusinessId, filter);

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion

    #region GetOutstandingOrdersAsync Tests

    [Fact]
    public async Task GetOutstandingOrdersAsync_ReturnsOnlyOutstandingOrders()
    {
        // Arrange
        var outstandingOrders = CreateTestPurchaseOrders()
            .Where(po => po.Status == PurchaseOrderStatus.Confirmed ||
                        po.Status == PurchaseOrderStatus.PartiallyReceived);

        _mockPurchaseOrderRepository
            .Setup(r => r.GetOutstandingOrdersAsync(_testBusinessId))
            .ReturnsAsync(outstandingOrders);

        // Act
        var result = await _service.GetOutstandingOrdersAsync(_testBusinessId);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(po =>
            po.Status == PurchaseOrderStatus.Confirmed ||
            po.Status == PurchaseOrderStatus.PartiallyReceived);
    }

    #endregion

    #region GetPurchaseOrderByIdAsync Tests

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_WithValidId_ReturnsPurchaseOrder()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrders().First();
        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.GetPurchaseOrderByIdAsync(1, _testBusinessId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.BusinessId.Should().Be(_testBusinessId);
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(999, _testBusinessId))
            .ReturnsAsync((PurchaseOrder?)null);

        // Act
        var result = await _service.GetPurchaseOrderByIdAsync(999, _testBusinessId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreatePurchaseOrderAsync Tests

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithValidData_CreatesPurchaseOrder()
    {
        // Arrange
        var company = CreateTestCompany();
        var createDto = CreateValidCreateDto();

        _mockCompanyRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), _testBusinessId))
            .ReturnsAsync(company);

        _mockPurchaseOrderRepository
            .Setup(r => r.GenerateOrderNumberAsync(_testBusinessId))
            .ReturnsAsync("PO-2024-0001");

        _mockPurchaseOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreatePurchaseOrderAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.PurchaseOrder.Should().NotBeNull();
        result.PurchaseOrder!.OrderNumber.Should().Be("PO-2024-0001");
        result.PurchaseOrder.Status.Should().Be(PurchaseOrderStatus.Draft);

        _mockPurchaseOrderRepository.Verify(r => r.AddAsync(It.IsAny<PurchaseOrder>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithInvalidCompany_ReturnsError()
    {
        // Arrange
        var createDto = CreateValidCreateDto();

        _mockCompanyRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), _testBusinessId))
            .ReturnsAsync((Company?)null);

        // Act
        var result = await _service.CreatePurchaseOrderAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Company not found");
        result.PurchaseOrder.Should().BeNull();

        _mockPurchaseOrderRepository.Verify(r => r.AddAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithNoLineItems_ReturnsError()
    {
        // Arrange
        var company = CreateTestCompany();
        var createDto = new CreatePurchaseOrderRequest
        {
            CompanyId = 1,
            Lines = new List<CreatePurchaseOrderLine>()
        };

        _mockCompanyRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), _testBusinessId))
            .ReturnsAsync(company);

        // Act
        var result = await _service.CreatePurchaseOrderAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Purchase order must have at least one line item");
        result.PurchaseOrder.Should().BeNull();
    }

    #endregion

    #region SubmitPurchaseOrderAsync Tests

    [Fact]
    public async Task SubmitPurchaseOrderAsync_WithDraftOrder_SubmitsSuccessfully()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrders().First(po => po.Status == PurchaseOrderStatus.Draft);
        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.SubmitPurchaseOrderAsync(1, _testBusinessId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        purchaseOrder.Status.Should().Be(PurchaseOrderStatus.Submitted);
        purchaseOrder.SubmittedAt.Should().NotBeNull();

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitPurchaseOrderAsync_WithNonDraftOrder_ReturnsError()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrders().First(po => po.Status == PurchaseOrderStatus.Confirmed);
        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(2, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.SubmitPurchaseOrderAsync(2, _testBusinessId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Only draft purchase orders can be submitted");

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitPurchaseOrderAsync_WithInvalidId_ReturnsError()
    {
        // Arrange
        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(999, _testBusinessId))
            .ReturnsAsync((PurchaseOrder?)null);

        // Act
        var result = await _service.SubmitPurchaseOrderAsync(999, _testBusinessId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Purchase order not found");
    }

    #endregion

    #region ConfirmPurchaseOrderAsync Tests

    [Fact]
    public async Task ConfirmPurchaseOrderAsync_WithSubmittedOrder_ConfirmsSuccessfully()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder(2, PurchaseOrderStatus.Submitted);
        var confirmDto = new ConfirmPurchaseOrderRequest
        {
            ConfirmedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(2, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.ConfirmPurchaseOrderAsync(2, confirmDto, _testBusinessId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        purchaseOrder.Status.Should().Be(PurchaseOrderStatus.Confirmed);
        purchaseOrder.ConfirmedDeliveryDate.Should().Be(confirmDto.ConfirmedDeliveryDate);
    }

    [Fact]
    public async Task ConfirmPurchaseOrderAsync_WithNonSubmittedOrder_ReturnsError()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrders().First(po => po.Status == PurchaseOrderStatus.Draft);
        var confirmDto = new ConfirmPurchaseOrderRequest
        {
            ConfirmedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.ConfirmPurchaseOrderAsync(1, confirmDto, _testBusinessId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Only submitted purchase orders can be confirmed");
    }

    #endregion

    #region CancelPurchaseOrderAsync Tests

    [Fact]
    public async Task CancelPurchaseOrderAsync_WithValidOrder_CancelsSuccessfully()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrders().First(po => po.Status == PurchaseOrderStatus.Draft);
        var cancelDto = new CancelPurchaseOrderRequest
        {
            Reason = "Test cancellation reason"
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CancelPurchaseOrderAsync(1, cancelDto, _testBusinessId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        purchaseOrder.Status.Should().Be(PurchaseOrderStatus.Cancelled);
        purchaseOrder.CancellationReason.Should().Be(cancelDto.Reason);
        purchaseOrder.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelPurchaseOrderAsync_WithCompletedOrder_ReturnsError()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder(3, PurchaseOrderStatus.Completed);
        var cancelDto = new CancelPurchaseOrderRequest
        {
            Reason = "Test cancellation reason"
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(3, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.CancelPurchaseOrderAsync(3, cancelDto, _testBusinessId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Completed purchase orders cannot be cancelled");
    }

    [Fact]
    public async Task CancelPurchaseOrderAsync_WithoutReason_ReturnsError()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrders().First();
        var cancelDto = new CancelPurchaseOrderRequest
        {
            Reason = ""
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.CancelPurchaseOrderAsync(1, cancelDto, _testBusinessId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Cancellation reason is required");
    }

    #endregion

    #region Helper Methods

    private List<PurchaseOrder> CreateTestPurchaseOrders()
    {
        return new List<PurchaseOrder>
        {
            CreateTestPurchaseOrder(1, PurchaseOrderStatus.Draft),
            CreateTestPurchaseOrder(2, PurchaseOrderStatus.Confirmed)
        };
    }

    private PurchaseOrder CreateTestPurchaseOrder(int id, PurchaseOrderStatus status)
    {
        return new PurchaseOrder
        {
            Id = id,
            BusinessId = _testBusinessId,
            CompanyId = 1,
            OrderNumber = $"PO-2024-{id:D4}",
            OrderDate = DateTime.UtcNow,
            Status = status,
            SubTotal = 1000.00m,
            TaxAmount = 100.00m,
            ShippingCost = 50.00m,
            TotalAmount = 1150.00m,
            CreatedBy = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = new List<PurchaseOrderLine>
            {
                new PurchaseOrderLine
                {
                    Id = id * 10,
                    PurchaseOrderId = id,
                    ProductId = 1,
                    ProductName = "Test Product",
                    ProductSku = "SKU001",
                    QuantityOrdered = 10,
                    UnitPrice = 100.00m,
                    LineTotal = 1000.00m,
                    QuantityReceived = 0,
                    QuantityOutstanding = 10,
                    Status = LineItemStatus.Pending
                }
            },
            Company = new Company
            {
                Id = 1,
                Name = "Test Supplier",
                BusinessId = _testBusinessId,
                IsSupplier = true
            }
        };
    }

    private Company CreateTestCompany()
    {
        return new Company
        {
            Id = 1,
            Name = "Test Supplier",
            BusinessId = _testBusinessId,
            IsSupplier = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private CreatePurchaseOrderRequest CreateValidCreateDto()
    {
        return new CreatePurchaseOrderRequest
        {
            CompanyId = 1,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            TaxAmount = 100.00m,
            ShippingCost = 50.00m,
            Notes = "Test purchase order",
            Lines = new List<CreatePurchaseOrderLine>
            {
                new CreatePurchaseOrderLine
                {
                    ProductId = 1,
                    QuantityOrdered = 10,
                    UnitPrice = 100.00m
                }
            }
        };
    }

    #endregion
}
