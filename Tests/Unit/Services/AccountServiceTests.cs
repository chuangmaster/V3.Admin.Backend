using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// AccountService 單元測試
/// 測試用戶檔案查詢功能和密碼管理功能
/// </summary>
public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<ILogger<AccountService>> _mockLogger;
    private readonly Mock<IRolePermissionRepository> _mockRolePermissionRepository;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _mockRolePermissionRepository = new Mock<IRolePermissionRepository>();
        _mockCache = new Mock<IDistributedCache>();
        _accountService = new AccountService(
            _mockUserRepository.Object,
            _mockUserRoleRepository.Object,
            _mockRolePermissionRepository.Object,
            _mockCache.Object,
            _mockLogger.Object
        );
    }

    #region GetUserProfileAsync Tests

    /// <summary>
    /// 測試：當用戶存在且有角色時，返回完整的用戶檔案
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithValidUserAndRoles_ReturnsCompleteProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Account = "testuser",
            DisplayName = "Test User",
            IsDeleted = false,
        };
        var roles = new List<string> { "Admin", "User" };
        var userRoles = new List<UserRole>();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Account.Should().Be("testuser");
        result.DisplayName.Should().Be("Test User");
        result.Roles.Should().HaveCount(2);
        result.Roles.Should().Contain(new[] { "Admin", "User" });
    }

    /// <summary>
    /// 測試：當用戶存在但沒有角色時，返回空角色列表
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithValidUserNoRoles_ReturnsProfileWithEmptyRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Account = "newuser",
            DisplayName = "New User",
            IsDeleted = false,
        };
        var roles = new List<string>();
        var userRoles = new List<UserRole>();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Account.Should().Be("newuser");
        result.DisplayName.Should().Be("New User");
        result.Roles.Should().BeEmpty();
    }

    /// <summary>
    /// 測試：當用戶存在但 DisplayName 為 null 時，正確處理
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithNullDisplayName_ReturnsProfileWithNullDisplayName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Account = "testuser",
            DisplayName = "",
            IsDeleted = false,
        };
        var roles = new List<string> { "User" };
        var userRoles = new List<UserRole>();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Account.Should().Be("testuser");
        result.DisplayName.Should().BeEmpty();
        result.Roles.Should().HaveCount(1);
    }

    /// <summary>
    /// 測試：當用戶不存在時，返回 null
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithNonexistentUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 測試：當用戶已被刪除（軟刪除）時，返回 null
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithDeletedUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Account = "deleteduser",
            DisplayName = "Deleted User",
            IsDeleted = true,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 測試：當用戶有多個角色時，正確返回所有角色
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithMultipleRoles_ReturnsAllRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Account = "admin",
            DisplayName = "Administrator",
            IsDeleted = false,
        };
        var roles = new List<string> { "Admin", "SuperAdmin", "Editor", "Viewer" };
        var userRoles = new List<UserRole>();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Roles.Should().HaveCount(4);
        result.Roles.Should().Equal(new[] { "Admin", "SuperAdmin", "Editor", "Viewer" });
    }

    /// <summary>
    /// 測試：UserProfileResponse DTO 正確映射
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_VerifiesDTOMappingCorrectness()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Account = "john.doe",
            DisplayName = "John Doe",
            IsDeleted = false,
        };
        var roles = new List<string> { "Manager", "Approver" };
        var userRoles = new List<UserRole>();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().BeOfType<UserProfileDto>();
        result!.Account.Should().Be(user.Account);
        result.DisplayName.Should().Be(user.DisplayName);
        result.Roles.Should().Equal(roles);
    }

    /// <summary>
    /// 測試：當用戶有角色和權限時，正確聚合所有權限
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithRolesAndPermissions_ReturnsAggregatedPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Account = "admin",
            DisplayName = "Administrator",
            IsDeleted = false,
        };

        var roles = new List<string> { "Admin", "Manager" };
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 },
        };

        var rolePermissions1 = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), PermissionCode = "user.read" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "user.write" },
        };

        var rolePermissions2 = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), PermissionCode = "report.read" },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId1, default))
            .ReturnsAsync(rolePermissions1);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId2, default))
            .ReturnsAsync(rolePermissions2);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().HaveCount(3);
        result.Permissions.Should().Contain(new[] { "user.read", "user.write", "report.read" });
        result.Permissions.Should().BeInAscendingOrder();
    }

    /// <summary>
    /// 測試：當多個角色有重複的權限時，去重後只保留一份
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithDuplicatePermissions_RemovesDuplicates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Account = "moderator",
            DisplayName = "Moderator",
            IsDeleted = false,
        };

        var roles = new List<string> { "Moderator", "Editor" };
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 },
        };

        var rolePermissions1 = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), PermissionCode = "post.read" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "post.write" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "comment.delete" },
        };

        var rolePermissions2 = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), PermissionCode = "post.read" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "post.write" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "file.upload" },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId1, default))
            .ReturnsAsync(rolePermissions1);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId2, default))
            .ReturnsAsync(rolePermissions2);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().HaveCount(4);
        result
            .Permissions.Should()
            .Contain(new[] { "post.read", "post.write", "comment.delete", "file.upload" });
    }

    /// <summary>
    /// 測試：當用戶有角色但角色沒有權限時，返回空權限列表
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WithRolesButNoPermissions_ReturnsEmptyPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Account = "viewer",
            DisplayName = "Viewer",
            IsDeleted = false,
        };

        var roles = new List<string> { "Viewer" };
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId },
        };

        var rolePermissions = new List<Permission>();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId, default))
            .ReturnsAsync(rolePermissions);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().BeEmpty();
    }

    /// <summary>
    /// 測試：當查詢角色權限時發生例外，略過該角色並繼續處理其他角色的權限
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_WhenRolePermissionQueryFails_SkipsFailedRoleAndContinues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            DisplayName = "Test User",
            IsDeleted = false,
        };

        var roles = new List<string> { "Role1", "Role2" };
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 },
        };

        var rolePermissions2 = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), PermissionCode = "action.read" },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId1, default))
            .ThrowsAsync(new InvalidOperationException("角色權限查詢失敗"));

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId2, default))
            .ReturnsAsync(rolePermissions2);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().HaveCount(1);
        result.Permissions.Should().Contain("action.read");
    }

    /// <summary>
    /// 測試：當角色權限列表中有 null 或空白的 PermissionCode 時，正確過濾
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_IgnoresNullAndEmptyPermissionCodes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            DisplayName = "Test User",
            IsDeleted = false,
        };

        var roles = new List<string> { "TestRole" };
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId },
        };

        var rolePermissions = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), PermissionCode = "valid.permission" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "   " },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "another.valid" },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId, default))
            .ReturnsAsync(rolePermissions);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().HaveCount(2);
        result.Permissions.Should().Contain(new[] { "valid.permission", "another.valid" });
    }

    /// <summary>
    /// 測試：權限清單應按照字母順序排序(不區分大小寫)
    /// </summary>
    [Fact]
    public async Task GetUserProfileAsync_PermissionsShouldBeSorted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            DisplayName = "Test User",
            IsDeleted = false,
        };

        var roles = new List<string> { "TestRole" };
        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = roleId },
        };

        var rolePermissions = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), PermissionCode = "zebra.read" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "apple.write" },
            new Permission { Id = Guid.NewGuid(), PermissionCode = "Banana.delete" },
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        _mockUserRoleRepository
            .Setup(x => x.GetUserRolesAsync(userId, default))
            .ReturnsAsync(userRoles);

        _mockRolePermissionRepository
            .Setup(x => x.GetRolePermissionsAsync(roleId, default))
            .ReturnsAsync(rolePermissions);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().HaveCount(3);
        result.Permissions.Should().Equal(new[] { "apple.write", "Banana.delete", "zebra.read" });
    }

    #endregion

    #region ChangePasswordAsync Tests

    /// <summary>
    /// 測試：當舊密碼正確且版本匹配時,成功變更密碼
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithValidOldPassword_SuccessfullyChangesPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "OldPassword123";
        var newPassword = "NewPassword456";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword, workFactor: 12);

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            PasswordHash = passwordHash,
            Version = 1,
            IsDeleted = false,
        };

        var dto = new ChangePasswordDto
        {
            Id = userId,
            OldPassword = oldPassword,
            NewPassword = newPassword,
            Version = 1,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>(), 1)).ReturnsAsync(true);
        _mockCache.Setup(x => x.RemoveAsync($"user_version:{userId}", default)).Returns(Task.CompletedTask);

        // Act
        await _accountService.ChangePasswordAsync(dto);

        // Assert
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Id == userId), 1), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync($"user_version:{userId}", default), Times.Once);
    }

    /// <summary>
    /// 測試：當舊密碼錯誤時,拋出 UnauthorizedAccessException
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithInvalidOldPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "OldPassword123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword, workFactor: 12);

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            PasswordHash = passwordHash,
            Version = 1,
            IsDeleted = false,
        };

        var dto = new ChangePasswordDto
        {
            Id = userId,
            OldPassword = "WrongPassword",
            NewPassword = "NewPassword456",
            Version = 1,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _accountService.ChangePasswordAsync(dto)
        );

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// 測試：當新密碼與舊密碼相同時,拋出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithSamePassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var password = "SamePassword123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            PasswordHash = passwordHash,
            Version = 1,
            IsDeleted = false,
        };

        var dto = new ChangePasswordDto
        {
            Id = userId,
            OldPassword = password,
            NewPassword = password,
            Version = 1,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _accountService.ChangePasswordAsync(dto)
        );

        exception.Message.Should().Contain("新密碼不可與舊密碼相同");
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// 測試：當版本號不匹配時,拋出 InvalidOperationException (樂觀並發控制)
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithVersionMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "OldPassword123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword, workFactor: 12);

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            PasswordHash = passwordHash,
            Version = 2, // 資料庫中的版本是 2
            IsDeleted = false,
        };

        var dto = new ChangePasswordDto
        {
            Id = userId,
            OldPassword = oldPassword,
            NewPassword = "NewPassword456",
            Version = 1, // 期望的版本是 1,發生衝突
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _accountService.ChangePasswordAsync(dto)
        );

        exception.Message.Should().Contain("資料已被其他使用者更新");
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// 測試：當用戶不存在時,拋出 KeyNotFoundException
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithNonexistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new ChangePasswordDto
        {
            Id = userId,
            OldPassword = "OldPassword123",
            NewPassword = "NewPassword456",
            Version = 1,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.ChangePasswordAsync(dto)
        );

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// 測試：當資料庫更新失敗時,拋出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenDatabaseUpdateFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "OldPassword123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword, workFactor: 12);

        var user = new User
        {
            Id = userId,
            Account = "testuser",
            PasswordHash = passwordHash,
            Version = 1,
            IsDeleted = false,
        };

        var dto = new ChangePasswordDto
        {
            Id = userId,
            OldPassword = oldPassword,
            NewPassword = "NewPassword456",
            Version = 1,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>(), 1)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _accountService.ChangePasswordAsync(dto)
        );

        exception.Message.Should().Contain("資料已被其他使用者更新");
    }

    #endregion

    #region ResetPasswordAsync Tests

    /// <summary>
    /// 測試：管理員成功重設用戶密碼
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WithValidData_SuccessfullyResetsPassword()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var newPassword = "NewPassword456";

        var targetUser = new User
        {
            Id = targetUserId,
            Account = "targetuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123", workFactor: 12),
            Version = 1,
            IsDeleted = false,
        };

        var dto = new ResetPasswordDto
        {
            TargetUserId = targetUserId,
            NewPassword = newPassword,
            Version = 1,
            OperatorId = operatorId,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>(), 1)).ReturnsAsync(true);
        _mockCache
            .Setup(x => x.RemoveAsync($"user_version:{targetUserId}", default))
            .Returns(Task.CompletedTask);

        // Act
        await _accountService.ResetPasswordAsync(dto);

        // Assert
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Id == targetUserId), 1), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync($"user_version:{targetUserId}", default), Times.Once);
    }

    /// <summary>
    /// 測試：當目標用戶不存在時,拋出 KeyNotFoundException
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WithNonexistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var dto = new ResetPasswordDto
        {
            TargetUserId = targetUserId,
            NewPassword = "NewPassword456",
            Version = 1,
            OperatorId = operatorId,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(targetUserId)).ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.ResetPasswordAsync(dto)
        );

        exception.Message.Should().Contain(targetUserId.ToString());
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// 測試：當版本號不匹配時,拋出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WithVersionMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var targetUser = new User
        {
            Id = targetUserId,
            Account = "targetuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123", workFactor: 12),
            Version = 2, // 資料庫中的版本是 2
            IsDeleted = false,
        };

        var dto = new ResetPasswordDto
        {
            TargetUserId = targetUserId,
            NewPassword = "NewPassword456",
            Version = 1, // 期望的版本是 1,發生衝突
            OperatorId = operatorId,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _accountService.ResetPasswordAsync(dto)
        );

        exception.Message.Should().Contain("資料已被其他使用者更新");
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// 測試：當資料庫更新失敗時,拋出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WhenDatabaseUpdateFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var targetUser = new User
        {
            Id = targetUserId,
            Account = "targetuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123", workFactor: 12),
            Version = 1,
            IsDeleted = false,
        };

        var dto = new ResetPasswordDto
        {
            TargetUserId = targetUserId,
            NewPassword = "NewPassword456",
            Version = 1,
            OperatorId = operatorId,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>(), 1)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _accountService.ResetPasswordAsync(dto)
        );

        exception.Message.Should().Contain("資料已被其他使用者更新");
    }

    /// <summary>
    /// 測試：當目標用戶已被刪除時,拋出 KeyNotFoundException
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WithDeletedUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var targetUser = new User
        {
            Id = targetUserId,
            Account = "targetuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123", workFactor: 12),
            Version = 1,
            IsDeleted = true, // 用戶已被刪除
        };

        var dto = new ResetPasswordDto
        {
            TargetUserId = targetUserId,
            NewPassword = "NewPassword456",
            Version = 1,
            OperatorId = operatorId,
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _accountService.ResetPasswordAsync(dto)
        );

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    #endregion
}
