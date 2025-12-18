using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using V3.Admin.Backend.Services.Interfaces;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// 客戶服務單元測試
/// </summary>
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepository;
    private readonly Mock<IAuditLogService> _auditLogService;
    private readonly Mock<ILogger<CustomerService>> _logger;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _customerRepository = new Mock<ICustomerRepository>();
        _auditLogService = new Mock<IAuditLogService>();
        _logger = new Mock<ILogger<CustomerService>>();

        _service = new CustomerService(
            _customerRepository.Object,
            _auditLogService.Object,
            _logger.Object
        );
    }

    /// <summary>
    /// 搜尋時頁碼與每頁筆數會被修正到合理範圍
    /// </summary>
    [Fact]
    public async Task SearchCustomersAsync_WithInvalidPageParameters_ShouldClamp()
    {
        var operatorId = Guid.NewGuid();
        var request = new SearchCustomerRequest
        {
            Name = "王",
        };

        _customerRepository
            .Setup(r => r.SearchAsync(1, 20, request.Name, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        PagedResultDto<CustomerDto> result = await _service.SearchCustomersAsync(
            0,
            0,
            request,
            operatorId,
            CancellationToken.None
        );

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
        _customerRepository.Verify(
            r => r.SearchAsync(1, 20, request.Name, null, null, null, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    /// <summary>
    /// 新增客戶時，若身分證字號已存在應丟出例外
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WithDuplicateIdNumber_ShouldThrow()
    {
        var operatorId = Guid.NewGuid();
        var request = new CreateCustomerRequest
        {
            Name = "王小明",
            PhoneNumber = "0912345678",
            Email = "test@example.com",
            IdNumber = "A123456789",
        };

        _customerRepository
            .Setup(r => r.GetByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = Guid.NewGuid(), IdNumber = request.IdNumber });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCustomerAsync(request, operatorId, CancellationToken.None)
        );

        _customerRepository.Verify(
            r => r.CreateAsync(It.IsAny<Customer>(), null, It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    /// <summary>
    /// 新增客戶成功應回傳 DTO
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WithValidRequest_ShouldReturnDto()
    {
        var operatorId = Guid.NewGuid();
        var request = new CreateCustomerRequest
        {
            Name = "王小明",
            PhoneNumber = "0912345678",
            Email = "test@example.com",
            IdNumber = "A123456789",
        };

        _customerRepository
            .Setup(r => r.GetByIdNumberAsync(request.IdNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var createdEntity = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            IdNumber = request.IdNumber,
            CreatedAt = DateTime.UtcNow,
            Version = 1,
            IsDeleted = false,
        };

        _customerRepository
            .Setup(r => r.CreateAsync(It.IsAny<Customer>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        CustomerDto dto = await _service.CreateCustomerAsync(request, operatorId, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(createdEntity.Id);
        dto.Name.Should().Be(request.Name);
        dto.IdNumber.Should().Be(request.IdNumber);

        _customerRepository.Verify(
            r => r.CreateAsync(It.IsAny<Customer>(), null, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
