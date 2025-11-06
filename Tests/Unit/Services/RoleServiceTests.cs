using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using V3.Admin.Backend.Services.Interfaces;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// 角色服務單元測試
/// 測試角色 CRUD 操作和業務邏輯
/// </summary>
public class RoleServiceTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IRolePermissionRepository> _mockRolePermissionRepository;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IValidator<CreateRoleRequest>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateRoleRequest>> _mockUpdateValidator;
    private readonly Mock<IValidator<AssignRolePermissionsRequest>> _mockAssignValidator;
    private readonly Mock<ILogger<RoleService>> _mockLogger;
    private readonly IRoleService _roleService;

    public RoleServiceTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockRolePermissionRepository = new Mock<IRolePermissionRepository>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockCreateValidator = new Mock<IValidator<CreateRoleRequest>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateRoleRequest>>();
        _mockAssignValidator = new Mock<IValidator<AssignRolePermissionsRequest>>();
        _mockLogger = new Mock<ILogger<RoleService>>();

        _roleService = new RoleService(
            _mockRoleRepository.Object,
            _mockRolePermissionRepository.Object,
            _mockPermissionRepository.Object,
            _mockAuditLogService.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockAssignValidator.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidRequest_SucceedsAsync()
    {
        // Arrange
        var request = new CreateRoleRequest { RoleName = "TestRole", Description = "Test" };
        var adminId = Guid.NewGuid();

        _mockCreateValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleRepository.Setup(r => r.RoleNameExistsAsync("TestRole", null, default))
            .ReturnsAsync(false);
        _mockRoleRepository.Setup(r => r.CreateAsync(It.IsAny<Role>(), default))
            .ReturnsAsync(new Role { Id = Guid.NewGuid(), RoleName = "TestRole" });

        // Act
        var result = await _roleService.CreateRoleAsync(request, adminId);

        // Assert
        result.Should().NotBeNull();
        result.RoleName.Should().Be("TestRole");
    }

    [Fact]
    public async Task DeleteRoleAsync_WithRoleInUse_FailsAsync()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var request = new DeleteRoleRequest { Version = 1 };

        _mockRoleRepository.Setup(r => r.GetByIdAsync(roleId, default))
            .ReturnsAsync(new Role { Id = roleId, RoleName = "TestRole", Version = 1 });
        _mockRoleRepository.Setup(r => r.IsInUseAsync(roleId, default))
            .ReturnsAsync(true);

        // Act & Assert
        var action = async () => await _roleService.DeleteRoleAsync(roleId, request, adminId);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
