using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// 權限驗證服務單元測試
/// 測試合併邏輯、失敗記錄、效能基準
/// </summary>
public class PermissionValidationServiceTests
{
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IRolePermissionRepository> _mockRolePermissionRepository;
    private readonly Mock<IPermissionFailureLogRepository> _mockFailureLogRepository;
    private readonly Mock<ILogger<PermissionValidationService>> _mockLogger;
    private readonly PermissionValidationService _service;

    public PermissionValidationServiceTests()
    {
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockRolePermissionRepository = new Mock<IRolePermissionRepository>();
        _mockFailureLogRepository = new Mock<IPermissionFailureLogRepository>();
        _mockLogger = new Mock<ILogger<PermissionValidationService>>();

        _service = new PermissionValidationService(
            _mockPermissionRepository.Object,
            _mockUserRoleRepository.Object,
            _mockRoleRepository.Object,
            _mockRolePermissionRepository.Object,
            _mockFailureLogRepository.Object,
            _mockLogger.Object);
    }

    /// <summary>
    /// 測試：多角色權限合併
    /// 預期：返回兩個角色所有不重複的權限
    /// </summary>
    [Fact]
    public async Task GetUserEffectivePermissions_WithMultipleRoles_ReturnsMergedPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var permId1 = Guid.NewGuid();
        var permId2 = Guid.NewGuid();

        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId1, AssignedAt = DateTime.UtcNow },
            new UserRole { UserId = userId, RoleId = roleId2, AssignedAt = DateTime.UtcNow }
        };

        var rolePermissions1 = new List<Permission>
        {
            new Permission
            {
                Id = permId1,
                PermissionCode = "permission.read",
                Name = "Read Permission",
                Description = "Read",
                PermissionType = "function",
                CreatedAt = DateTime.UtcNow,
                Version = 1
            }
        };

        var rolePermissions2 = new List<Permission>
        {
            new Permission
            {
                Id = permId2,
                PermissionCode = "permission.write",
                Name = "Write Permission",
                Description = "Write",
                PermissionType = "function",
                CreatedAt = DateTime.UtcNow,
                Version = 1
            }
        };

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(r => r.GetRolePermissionsAsync(roleId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolePermissions1);

        _mockRolePermissionRepository
            .Setup(r => r.GetRolePermissionsAsync(roleId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolePermissions2);

        // Act
        var result = await _service.GetUserEffectivePermissionsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Permissions.Should().HaveCount(2);

        var permCodes = result.Permissions.Select(p => p.PermissionCode).ToList();
        permCodes.Should().Contain("permission.read");
        permCodes.Should().Contain("permission.write");

        _mockUserRoleRepository.Verify(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRolePermissionRepository.Verify(r => r.GetRolePermissionsAsync(roleId1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRolePermissionRepository.Verify(r => r.GetRolePermissionsAsync(roleId2, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 測試：重複權限去重
    /// 預期：兩個角色擁有相同權限時，只返回一個
    /// </summary>
    [Fact]
    public async Task GetUserEffectivePermissions_WithDuplicatePermissions_ReturnsDeduplicatedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var permId1 = Guid.NewGuid();

        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId1, AssignedAt = DateTime.UtcNow },
            new UserRole { UserId = userId, RoleId = roleId2, AssignedAt = DateTime.UtcNow }
        };

        var sharedPermission = new Permission
        {
            Id = permId1,
            PermissionCode = "permission.shared",
            Name = "Shared Permission",
            Description = "Shared",
            PermissionType = "function",
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };

        var rolePermissions1 = new List<Permission> { sharedPermission };
        var rolePermissions2 = new List<Permission> { sharedPermission };

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(r => r.GetRolePermissionsAsync(roleId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolePermissions1);

        _mockRolePermissionRepository
            .Setup(r => r.GetRolePermissionsAsync(roleId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolePermissions2);

        // Act
        var result = await _service.GetUserEffectivePermissionsAsync(userId);

        // Assert
        result.Permissions.Should().HaveCount(1);
        result.Permissions[0].PermissionCode.Should().Be("permission.shared");
    }

    /// <summary>
    /// 測試：無角色用戶返回空權限
    /// 預期：返回空的權限列表
    /// </summary>
    [Fact]
    public async Task GetUserEffectivePermissions_UserWithNoRoles_ReturnsEmptyPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyRoles = new List<UserRole>();

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyRoles);

        // Act
        var result = await _service.GetUserEffectivePermissionsAsync(userId);

        // Assert
        result.UserId.Should().Be(userId);
        result.Permissions.Should().BeEmpty();

        _mockUserRoleRepository.Verify(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRolePermissionRepository.Verify(r => r.GetRolePermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 測試：權限驗證成功
    /// 預期：用戶擁有該權限時返回 true
    /// </summary>
    [Fact]
    public async Task ValidatePermissionAsync_UserHasPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionCode = "permission.read";

        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId, AssignedAt = DateTime.UtcNow }
        };

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionCode = permissionCode,
            Name = "Read",
            Description = "Read",
            PermissionType = "function",
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(r => r.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { permission });

        // Act
        var result = await _service.ValidatePermissionAsync(userId, permissionCode);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 測試：權限驗證失敗
    /// 預期：用戶不擁有該權限時返回 false
    /// </summary>
    [Fact]
    public async Task ValidatePermissionAsync_UserDoesNotHavePermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionCode = "permission.nonexistent";

        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId, AssignedAt = DateTime.UtcNow }
        };

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionCode = "permission.read",
            Name = "Read",
            Description = "Read",
            PermissionType = "function",
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(r => r.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { permission });

        // Act
        var result = await _service.ValidatePermissionAsync(userId, permissionCode);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 測試：效能基準
    /// 預期：驗證操作在 100ms 以內完成
    /// </summary>
    [Fact]
    public async Task ValidatePermissionAsync_PerformanceBaseline_CompletesWithin100Ms()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionCode = "permission.read";

        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId, AssignedAt = DateTime.UtcNow }
        };

        var permissions = Enumerable.Range(0, 50)
            .Select(i => new Permission
            {
                Id = Guid.NewGuid(),
                PermissionCode = $"permission.{i}",
                Name = $"Permission {i}",
                Description = $"Desc {i}",
                PermissionType = "function",
                CreatedAt = DateTime.UtcNow,
                Version = 1
            })
            .ToList();

        _mockUserRoleRepository
            .Setup(r => r.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(r => r.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.ValidatePermissionAsync(userId, permissionCode);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    /// <summary>
    /// 測試：失敗日誌記錄
    /// 預期：調用失敗日誌存儲庫並傳遞正確的參數
    /// </summary>
    [Fact]
    public async Task LogPermissionFailureAsync_WithValidData_CallsRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var resource = "/api/protected";
        var reason = "Permission denied";
        var ipAddress = "127.0.0.1";
        var userAgent = "Mozilla/5.0";
        var traceId = "trace-123";

        _mockFailureLogRepository
            .Setup(r => r.LogFailureAsync(It.IsAny<PermissionFailureLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.LogPermissionFailureAsync(userId, username, resource, reason, ipAddress, userAgent, traceId);

        // Assert
        _mockFailureLogRepository.Verify(
            r => r.LogFailureAsync(It.Is<PermissionFailureLog>(l =>
                l.UserId == userId &&
                l.Username == username &&
                l.AttemptedResource == resource &&
                l.FailureReason == reason &&
                l.IpAddress == ipAddress &&
                l.UserAgent == userAgent &&
                l.TraceId == traceId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
