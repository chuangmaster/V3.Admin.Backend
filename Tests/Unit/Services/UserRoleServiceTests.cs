using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using V3.Admin.Backend.Services.Interfaces;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// 用戶角色服務單元測試
/// 測試用戶角色指派、移除等業務邏輯
/// </summary>
public class UserRoleServiceTests
{
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<UserRoleService>> _mockLogger;
    private readonly IUserRoleService _userRoleService;

    public UserRoleServiceTests()
    {
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<UserRoleService>>();

        var validator = new AssignUserRoleRequestValidator();

        _userRoleService = new UserRoleService(
            _mockUserRoleRepository.Object,
            _mockRoleRepository.Object,
            _mockUserRepository.Object,
            _mockLogger.Object,
            _mockAuditLogService.Object,
            validator
        );
    }

    [Fact]
    public async Task AssignRolesAsync_WithValidRolesAndUser_SucceedsAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var request = new AssignUserRoleRequest
        {
            RoleIds = new List<Guid> { roleId1, roleId2 },
        };

        // 模擬用戶存在
        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Username = "testuser" });

        // 模擬角色存在
        _mockRoleRepository.Setup(r => r.ExistsAsync(roleId1, default)).ReturnsAsync(true);
        _mockRoleRepository.Setup(r => r.ExistsAsync(roleId2, default)).ReturnsAsync(true);

        // 模擬指派成功
        _mockUserRoleRepository
            .Setup(r => r.AssignRolesAsync(userId, It.IsAny<List<Guid>>(), adminId, default))
            .ReturnsAsync(2);

        // Act
        var result = await _userRoleService.AssignRolesAsync(userId, request, adminId);

        // Assert
        result.Should().Be(2);
        _mockUserRoleRepository.Verify(
            r => r.AssignRolesAsync(userId, It.Is<List<Guid>>(l => l.Count == 2), adminId, default),
            Times.Once
        );
    }

    [Fact]
    public async Task AssignRolesAsync_WithNonexistentUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var request = new AssignUserRoleRequest { RoleIds = new List<Guid> { roleId } };

        // 模擬用戶不存在
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var action = async () => await _userRoleService.AssignRolesAsync(userId, request, adminId);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AssignRolesAsync_WithNonexistentRole_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var request = new AssignUserRoleRequest { RoleIds = new List<Guid> { roleId } };

        // 模擬用戶存在
        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Username = "testuser" });

        // 模擬角色不存在
        _mockRoleRepository.Setup(r => r.ExistsAsync(roleId, default)).ReturnsAsync(false);

        // Act
        var action = async () => await _userRoleService.AssignRolesAsync(userId, request, adminId);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RemoveRoleAsync_WithValidAssignment_SucceedsAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var request = new RemoveUserRoleRequest { RoleId = roleId };

        // 模擬用戶存在
        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Username = "testuser" });

        // 模擬角色存在
        _mockRoleRepository.Setup(r => r.ExistsAsync(roleId, default)).ReturnsAsync(true);

        // 模擬移除成功
        _mockUserRoleRepository
            .Setup(r => r.RemoveRoleAsync(userId, roleId, adminId, default))
            .ReturnsAsync(true);

        // Act
        var result = await _userRoleService.RemoveRoleAsync(userId, request, adminId);

        // Assert
        result.Should().BeTrue();
        _mockUserRoleRepository.Verify(
            r => r.RemoveRoleAsync(userId, roleId, adminId, default),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsUserRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testuser", IsDeleted = false };
        var role1 = new Role { Id = roleId1, RoleName = "Admin", IsDeleted = false };
        var role2 = new Role { Id = roleId2, RoleName = "User", IsDeleted = false };

        var expectedRoles = new List<UserRole>
        {
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId1,
                IsDeleted = false,
            },
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId2,
                IsDeleted = false,
            },
        };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, default))
            .ReturnsAsync(expectedRoles);

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(roleId1, default))
            .ReturnsAsync(role1);

        _mockRoleRepository
            .Setup(r => r.GetByIdAsync(roleId2, default))
            .ReturnsAsync(role2);

        // Act
        var result = await _userRoleService.GetUserRolesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        _mockUserRoleRepository.Verify(r => r.GetUserRolesAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testuser", IsDeleted = false };

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, default))
            .ReturnsAsync(new List<UserRole>());

        // Act
        var result = await _userRoleService.GetUserRolesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }
}
