using FluentAssertions;
using Moq;
using StockManager.API.Services;
using StockManager.Core.DTOs;
using StockManager.Core.Entities;
using StockManager.Core.Enums;
using StockManager.Core.Interfaces;
using Xunit;

namespace StockManager.API.Tests.Services;

public class ReceiptServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReceiptRepository> _mockReceiptRepository;
    private readonly Mock<IPurchaseOrderRepository> _mockPurchaseOrderRepository;
    private readonly Mock<InventoryUpdateService> _mockInventoryService;
    private readonly ReceiptService _service;
    private readonly int _testBusinessId = 1;
    private readonly string _testUserId = "test-user-123";

    public ReceiptServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReceiptRepository = new Mock<IReceiptRepository>();
        _mockPurchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
        _mockInventoryService = new Mock<InventoryUpdateService>(MockBehavior.Default, null!);

        _mockUnitOfWork.Setup(u => u.Receipts).Returns(_mockReceiptRepository.Object);
        _mockUnitOfWork.Setup(u => u.PurchaseOrders).Returns(_mockPurchaseOrderRepository.Object);

        _service = new ReceiptService(_mockUnitOfWork.Object, _mockInventoryService.Object);
    }

    #region GetAllReceiptsAsync Tests

    [Fact]
    public async Task GetAllReceiptsAsync_ReturnsAllReceipts()
    {
        // Arrange
        var receipts = CreateTestReceipts();
        _mockReceiptRepository
            .Setup(r => r.GetByBusinessIdWithDetailsAsync(_testBusinessId))
            .ReturnsAsync(receipts);

        // Act
        var result = await _service.GetAllReceiptsAsync(_testBusinessId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.BusinessId == _testBusinessId);
    }

    #endregion

    #region GetReceiptByIdAsync Tests

    [Fact]
    public async Task GetReceiptByIdAsync_WithValidId_ReturnsReceipt()
    {
        // Arrange
        var receipt = CreateTestReceipts().First();
        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(receipt);

        // Act
        var result = await _service.GetReceiptByIdAsync(1, _testBusinessId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.BusinessId.Should().Be(_testBusinessId);
    }

    [Fact]
    public async Task GetReceiptByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(999, _testBusinessId))
            .ReturnsAsync((Receipt?)null);

        // Act
        var result = await _service.GetReceiptByIdAsync(999, _testBusinessId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateReceiptAsync Tests

    [Fact]
    public async Task CreateReceiptAsync_WithValidData_CreatesReceipt()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        var createDto = CreateValidCreateReceiptDto();

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        _mockReceiptRepository
            .Setup(r => r.GenerateReceiptNumberAsync(_testBusinessId))
            .ReturnsAsync("REC-2024-0001");

        _mockReceiptRepository
            .Setup(r => r.AddAsync(It.IsAny<Receipt>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateReceiptAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Receipt.Should().NotBeNull();
        result.Receipt!.ReceiptNumber.Should().Be("REC-2024-0001");

        _mockReceiptRepository.Verify(r => r.AddAsync(It.IsAny<Receipt>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateReceiptAsync_WithInvalidPurchaseOrder_ReturnsError()
    {
        // Arrange
        var createDto = CreateValidCreateReceiptDto();

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync((PurchaseOrder?)null);

        // Act
        var result = await _service.CreateReceiptAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Purchase order not found");
        result.Receipt.Should().BeNull();
    }

    [Fact]
    public async Task CreateReceiptAsync_WithDraftPurchaseOrder_ReturnsError()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        purchaseOrder.Status = PurchaseOrderStatus.Draft;
        var createDto = CreateValidCreateReceiptDto();

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.CreateReceiptAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Cannot receive goods for draft purchase orders");
    }

    [Fact]
    public async Task CreateReceiptAsync_WithNoLineItems_ReturnsError()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        var createDto = new CreateReceiptRequest
        {
            PurchaseOrderId = 1,
            ReceiptDate = DateTime.UtcNow,
            Lines = new List<CreateReceiptLine>()
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        // Act
        var result = await _service.CreateReceiptAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Receipt must have at least one line item");
    }

    [Fact]
    public async Task CreateReceiptAsync_WithVariances_SetsStatusToPendingValidation()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        var createDto = new CreateReceiptRequest
        {
            PurchaseOrderId = 1,
            ReceiptDate = DateTime.UtcNow,
            Lines = new List<CreateReceiptLine>
            {
                new CreateReceiptLine
                {
                    PurchaseOrderLineId = 10,
                    QuantityReceived = 5, // Only half of what was ordered
                    Condition = ItemCondition.Good
                }
            }
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        _mockReceiptRepository
            .Setup(r => r.GenerateReceiptNumberAsync(_testBusinessId))
            .ReturnsAsync("REC-2024-0001");

        _mockReceiptRepository
            .Setup(r => r.AddAsync(It.IsAny<Receipt>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateReceiptAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Receipt!.HasVariances.Should().BeTrue();
        result.Receipt.Status.Should().Be(ReceiptStatus.PendingValidation);
    }

    [Fact]
    public async Task CreateReceiptAsync_WithDamagedItems_SetsStatusToPendingValidation()
    {
        // Arrange
        var purchaseOrder = CreateTestPurchaseOrder();
        var createDto = new CreateReceiptRequest
        {
            PurchaseOrderId = 1,
            ReceiptDate = DateTime.UtcNow,
            Lines = new List<CreateReceiptLine>
            {
                new CreateReceiptLine
                {
                    PurchaseOrderLineId = 10,
                    QuantityReceived = 10,
                    Condition = ItemCondition.Damaged,
                    DamageNotes = "Box was crushed"
                }
            }
        };

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        _mockReceiptRepository
            .Setup(r => r.GenerateReceiptNumberAsync(_testBusinessId))
            .ReturnsAsync("REC-2024-0001");

        _mockReceiptRepository
            .Setup(r => r.AddAsync(It.IsAny<Receipt>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateReceiptAsync(createDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Receipt!.HasVariances.Should().BeTrue();
        result.Receipt.Status.Should().Be(ReceiptStatus.PendingValidation);
    }

    #endregion

    #region ValidateReceiptAsync Tests

    [Fact]
    public async Task ValidateReceiptAsync_WithVariances_ReturnsVarianceDetails()
    {
        // Arrange
        var receipt = CreateTestReceipts().First(r => r.HasVariances);
        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(2, _testBusinessId))
            .ReturnsAsync(receipt);

        // Act
        var result = await _service.ValidateReceiptAsync(2, _testBusinessId);

        // Assert
        result.Should().NotBeNull();
        result.HasVariances.Should().BeTrue();
        result.Variances.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateReceiptAsync_WithNoVariances_ReturnsEmptyVariances()
    {
        // Arrange
        var receipt = CreateTestReceipts().First(r => !r.HasVariances);
        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(receipt);

        // Act
        var result = await _service.ValidateReceiptAsync(1, _testBusinessId);

        // Assert
        result.Should().NotBeNull();
        result.HasVariances.Should().BeFalse();
        result.Variances.Should().BeEmpty();
    }

    #endregion

    #region ApproveReceiptAsync Tests

    [Fact]
    public async Task ApproveReceiptAsync_WithPendingValidation_ApprovesSuccessfully()
    {
        // Arrange
        var receipt = CreateTestReceipt(2, ReceiptStatus.PendingValidation);
        var approveDto = new ApproveReceiptRequest
        {
            VarianceNotes = "Approved with noted variances"
        };

        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(2, _testBusinessId))
            .ReturnsAsync(receipt);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.ApproveReceiptAsync(2, approveDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        receipt.Status.Should().Be(ReceiptStatus.Validated);
        receipt.VarianceNotes.Should().Be(approveDto.VarianceNotes);
        receipt.ValidatedBy.Should().Be(_testUserId);
        receipt.ValidatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveReceiptAsync_WithNonPendingReceipt_ReturnsError()
    {
        // Arrange
        var receipt = CreateTestReceipt(1, ReceiptStatus.Completed);
        var approveDto = new ApproveReceiptRequest();

        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(receipt);

        // Act
        var result = await _service.ApproveReceiptAsync(1, approveDto, _testBusinessId, _testUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Only receipts pending validation can be approved");
    }

    #endregion

    #region RejectReceiptAsync Tests

    [Fact]
    public async Task RejectReceiptAsync_WithPendingValidation_RejectsSuccessfully()
    {
        // Arrange
        var receipt = CreateTestReceipt(2, ReceiptStatus.PendingValidation);
        var rejectDto = new RejectReceiptRequest
        {
            Reason = "Items do not match order"
        };

        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(2, _testBusinessId))
            .ReturnsAsync(receipt);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.RejectReceiptAsync(2, rejectDto, _testBusinessId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        receipt.Status.Should().Be(ReceiptStatus.Rejected);
        receipt.VarianceNotes.Should().Contain(rejectDto.Reason);
    }

    [Fact]
    public async Task RejectReceiptAsync_WithoutReason_ReturnsError()
    {
        // Arrange
        var receipt = CreateTestReceipt(2, ReceiptStatus.PendingValidation);
        var rejectDto = new RejectReceiptRequest
        {
            Reason = ""
        };

        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(2, _testBusinessId))
            .ReturnsAsync(receipt);

        // Act
        var result = await _service.RejectReceiptAsync(2, rejectDto, _testBusinessId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Rejection reason is required");
    }

    #endregion

    #region CompleteReceiptAsync Tests

    [Fact]
    public async Task CompleteReceiptAsync_WithValidatedReceipt_CompletesSuccessfully()
    {
        // Arrange
        var receipt = CreateTestReceipt(1, ReceiptStatus.Validated);
        var purchaseOrder = CreateTestPurchaseOrder();

        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(1, _testBusinessId))
            .ReturnsAsync(receipt);

        _mockPurchaseOrderRepository
            .Setup(r => r.GetByIdWithDetailsAsync(receipt.PurchaseOrderId, _testBusinessId))
            .ReturnsAsync(purchaseOrder);

        _mockInventoryService
            .Setup(s => s.UpdateInventoryFromReceiptAsync(It.IsAny<Receipt>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CompleteReceiptAsync(1, _testBusinessId);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        receipt.Status.Should().Be(ReceiptStatus.Completed);
        receipt.CompletedAt.Should().NotBeNull();

        _mockInventoryService.Verify(s => s.UpdateInventoryFromReceiptAsync(receipt, _testBusinessId), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CompleteReceiptAsync_WithNonValidatedReceipt_ReturnsError()
    {
        // Arrange
        var receipt = CreateTestReceipt(2, ReceiptStatus.PendingValidation);

        _mockReceiptRepository
            .Setup(r => r.GetByIdWithDetailsAsync(2, _testBusinessId))
            .ReturnsAsync(receipt);

        // Act
        var result = await _service.CompleteReceiptAsync(2, _testBusinessId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Only validated receipts can be completed");
    }

    #endregion

    #region Helper Methods

    private List<Receipt> CreateTestReceipts()
    {
        return new List<Receipt>
        {
            CreateTestReceipt(1, ReceiptStatus.Validated),
            CreateTestReceipt(2, ReceiptStatus.PendingValidation)
        };
    }

    private Receipt CreateTestReceipt(int id, ReceiptStatus status)
    {
        var hasVariances = status == ReceiptStatus.PendingValidation;

        return new Receipt
        {
            Id = id,
            BusinessId = _testBusinessId,
            PurchaseOrderId = 1,
            ReceiptNumber = $"REC-2024-{id:D4}",
            ReceiptDate = DateTime.UtcNow,
            ReceivedBy = _testUserId,
            Status = status,
            HasVariances = hasVariances,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = new List<ReceiptLine>
            {
                new ReceiptLine
                {
                    Id = id * 10,
                    ReceiptId = id,
                    PurchaseOrderLineId = 10,
                    ProductId = 1,
                    QuantityOrdered = 10,
                    QuantityReceived = hasVariances ? 8 : 10,
                    QuantityVariance = hasVariances ? -2 : 0,
                    UnitPriceOrdered = 100.00m,
                    PriceVariance = 0,
                    Condition = ItemCondition.Good,
                    Product = new Product
                    {
                        Id = 1,
                        Name = "Test Product",
                        SKU = "SKU001",
                        BusinessId = _testBusinessId
                    }
                }
            },
            PurchaseOrder = new PurchaseOrder
            {
                Id = 1,
                OrderNumber = "PO-2024-0001",
                BusinessId = _testBusinessId,
                CompanyId = 1,
                Company = new Company
                {
                    Id = 1,
                    Name = "Test Supplier",
                    BusinessId = _testBusinessId
                }
            }
        };
    }

    private PurchaseOrder CreateTestPurchaseOrder()
    {
        return new PurchaseOrder
        {
            Id = 1,
            BusinessId = _testBusinessId,
            CompanyId = 1,
            OrderNumber = "PO-2024-0001",
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Confirmed,
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
                    Id = 10,
                    PurchaseOrderId = 1,
                    ProductId = 1,
                    ProductName = "Test Product",
                    ProductSku = "SKU001",
                    QuantityOrdered = 10,
                    UnitPrice = 100.00m,
                    LineTotal = 1000.00m,
                    QuantityReceived = 0,
                    QuantityOutstanding = 10,
                    Status = LineItemStatus.Pending,
                    Product = new Product
                    {
                        Id = 1,
                        Name = "Test Product",
                        SKU = "SKU001",
                        BusinessId = _testBusinessId
                    }
                }
            }
        };
    }

    private CreateReceiptRequest CreateValidCreateReceiptDto()
    {
        return new CreateReceiptRequest
        {
            PurchaseOrderId = 1,
            ReceiptDate = DateTime.UtcNow,
            SupplierDeliveryNote = "DN-12345",
            Notes = "Test receipt",
            Lines = new List<CreateReceiptLine>
            {
                new CreateReceiptLine
                {
                    PurchaseOrderLineId = 10,
                    QuantityReceived = 10,
                    Condition = ItemCondition.Good
                }
            }
        };
    }

    #endregion
}
