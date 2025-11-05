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

public class ReceiptsControllerTests
{
    private readonly Mock<ReceiptService> _mockService;
    private readonly ReceiptsController _controller;
    private readonly int _testBusinessId = 1;
    private readonly string _testUserId = "test-user-id";

    public ReceiptsControllerTests()
    {
        _mockService = new Mock<ReceiptService>(MockBehavior.Default, null!, null!);
        _controller = new ReceiptsController(_mockService.Object);

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
    public async Task GetAll_ReturnsOkWithAllReceipts()
    {
        // Arrange
        var expectedReceipts = CreateTestReceipts();
        _mockService
            .Setup(s => s.GetAllReceiptsAsync(_testBusinessId))
            .ReturnsAsync(expectedReceipts);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var receipts = okResult.Value.Should().BeAssignableTo<IEnumerable<ReceiptDto>>().Subject;
        receipts.Should().HaveCount(2);
    }

    #endregion

    #region GetPendingValidation Tests

    [Fact]
    public async Task GetPendingValidation_ReturnsOkWithPendingReceipts()
    {
        // Arrange
        var expectedReceipts = CreateTestReceipts()
            .Where(r => r.Status == ReceiptStatus.PendingValidation);

        _mockService
            .Setup(s => s.GetPendingValidationReceiptsAsync(_testBusinessId))
            .ReturnsAsync(expectedReceipts);

        // Act
        var result = await _controller.GetPendingValidation();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var receipts = okResult.Value.Should().BeAssignableTo<IEnumerable<ReceiptDto>>().Subject;
        receipts.Should().OnlyContain(r => r.Status == ReceiptStatus.PendingValidation);
    }

    #endregion

    #region GetByPurchaseOrder Tests

    [Fact]
    public async Task GetByPurchaseOrder_ReturnsOkWithReceipts()
    {
        // Arrange
        var purchaseOrderId = 1;
        var expectedReceipts = CreateTestReceipts();

        _mockService
            .Setup(s => s.GetReceiptsByPurchaseOrderAsync(purchaseOrderId, _testBusinessId))
            .ReturnsAsync(expectedReceipts);

        // Act
        var result = await _controller.GetByPurchaseOrder(purchaseOrderId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var receipts = okResult.Value.Should().BeAssignableTo<IEnumerable<ReceiptDto>>().Subject;
        receipts.Should().NotBeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithReceipt()
    {
        // Arrange
        var expectedReceipt = CreateTestReceipts().First();
        _mockService
            .Setup(s => s.GetReceiptByIdAsync(1, _testBusinessId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var receipt = okResult.Value.Should().BeAssignableTo<ReceiptDto>().Subject;
        receipt.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetReceiptByIdAsync(999, _testBusinessId))
            .ReturnsAsync((ReceiptDto?)null);

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
        var createdReceipt = CreateTestReceipts().First();

        _mockService
            .Setup(s => s.CreateReceiptAsync(createDto, _testBusinessId, _testUserId))
            .ReturnsAsync((true, null, createdReceipt));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        createdResult.RouteValues!["id"].Should().Be(createdReceipt.Id);
        var returnedReceipt = createdResult.Value.Should().BeAssignableTo<ReceiptDto>().Subject;
        returnedReceipt.Id.Should().Be(createdReceipt.Id);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = CreateValidCreateDto();
        var errorMessage = "Purchase order not found";

        _mockService
            .Setup(s => s.CreateReceiptAsync(createDto, _testBusinessId, _testUserId))
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
        var updateDto = new UpdateReceiptRequest
        {
            ReceiptDate = DateTime.UtcNow,
            Lines = new List<CreateReceiptLine>()
        };

        _mockService
            .Setup(s => s.UpdateReceiptAsync(1, updateDto, _testBusinessId))
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
        var updateDto = new UpdateReceiptRequest
        {
            ReceiptDate = DateTime.UtcNow,
            Lines = new List<CreateReceiptLine>()
        };
        var errorMessage = "Receipt not found";

        _mockService
            .Setup(s => s.UpdateReceiptAsync(999, updateDto, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Validate Tests

    [Fact]
    public async Task Validate_WithValidId_ReturnsOkWithValidationResult()
    {
        // Arrange
        var validation = new ReceiptValidationDto
        {
            ReceiptId = 1,
            HasVariances = true,
            Variances = new List<VarianceDto>()
        };

        _mockService
            .Setup(s => s.ValidateReceiptAsync(1, _testBusinessId))
            .ReturnsAsync(validation);

        // Act
        var result = await _controller.Validate(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedValidation = okResult.Value.Should().BeAssignableTo<ReceiptValidationDto>().Subject;
        returnedValidation.ReceiptId.Should().Be(1);
    }

    #endregion

    #region Approve Tests

    [Fact]
    public async Task Approve_WithValidData_ReturnsOk()
    {
        // Arrange
        var approveDto = new ApproveReceiptRequest
        {
            VarianceNotes = "Approved with noted variances"
        };

        _mockService
            .Setup(s => s.ApproveReceiptAsync(1, approveDto, _testBusinessId, _testUserId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Approve(1, approveDto);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Approve_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var approveDto = new ApproveReceiptRequest();
        var errorMessage = "Only receipts pending validation can be approved";

        _mockService
            .Setup(s => s.ApproveReceiptAsync(1, approveDto, _testBusinessId, _testUserId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Approve(1, approveDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Reject Tests

    [Fact]
    public async Task Reject_WithValidData_ReturnsOk()
    {
        // Arrange
        var rejectDto = new RejectReceiptRequest
        {
            Reason = "Items do not match order"
        };

        _mockService
            .Setup(s => s.RejectReceiptAsync(1, rejectDto, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Reject(1, rejectDto);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsBadRequest()
    {
        // Arrange
        var rejectDto = new RejectReceiptRequest
        {
            Reason = ""
        };
        var errorMessage = "Rejection reason is required";

        _mockService
            .Setup(s => s.RejectReceiptAsync(1, rejectDto, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Reject(1, rejectDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Complete Tests

    [Fact]
    public async Task Complete_WithValidId_ReturnsOk()
    {
        // Arrange
        _mockService
            .Setup(s => s.CompleteReceiptAsync(1, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Complete(1);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Complete_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Only validated receipts can be completed";

        _mockService
            .Setup(s => s.CompleteReceiptAsync(1, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Complete(1);

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
            .Setup(s => s.DeleteReceiptAsync(1, _testBusinessId))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithCompletedReceipt_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Completed receipts cannot be deleted";

        _mockService
            .Setup(s => s.DeleteReceiptAsync(1, _testBusinessId))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region Helper Methods

    private List<ReceiptDto> CreateTestReceipts()
    {
        return new List<ReceiptDto>
        {
            new ReceiptDto
            {
                Id = 1,
                BusinessId = _testBusinessId,
                PurchaseOrderId = 1,
                PurchaseOrderNumber = "PO-2024-0001",
                CompanyName = "Test Supplier",
                ReceiptNumber = "REC-2024-0001",
                ReceiptDate = DateTime.UtcNow,
                ReceivedBy = _testUserId,
                ReceivedByName = "Test User",
                Status = ReceiptStatus.Validated,
                HasVariances = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Lines = new List<ReceiptLineDto>()
            },
            new ReceiptDto
            {
                Id = 2,
                BusinessId = _testBusinessId,
                PurchaseOrderId = 1,
                PurchaseOrderNumber = "PO-2024-0001",
                CompanyName = "Test Supplier",
                ReceiptNumber = "REC-2024-0002",
                ReceiptDate = DateTime.UtcNow,
                ReceivedBy = _testUserId,
                ReceivedByName = "Test User",
                Status = ReceiptStatus.PendingValidation,
                HasVariances = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Lines = new List<ReceiptLineDto>()
            }
        };
    }

    private CreateReceiptRequest CreateValidCreateDto()
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
