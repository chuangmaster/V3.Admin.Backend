using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// AccountService 單元測試
/// 測試用戶檔案查詢功能
/// </summary>
public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<ILogger<AccountService>> _mockLogger;
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _accountService = new AccountService(
            _mockUserRepository.Object,
            _mockUserRoleRepository.Object,
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
            Username = "testuser",
            DisplayName = "Test User",
            IsDeleted = false
        };
        var roles = new List<string> { "Admin", "User" };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
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
            Username = "newuser",
            DisplayName = "New User",
            IsDeleted = false
        };
        var roles = new List<string>();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("newuser");
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
            Username = "testuser",
            DisplayName = "",
            IsDeleted = false
        };
        var roles = new List<string> { "User" };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
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

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

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
            Username = "deleteduser",
            DisplayName = "Deleted User",
            IsDeleted = true
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

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
            Username = "admin",
            DisplayName = "Administrator",
            IsDeleted = false
        };
        var roles = new List<string> { "Admin", "SuperAdmin", "Editor", "Viewer" };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

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
            Username = "john.doe",
            DisplayName = "John Doe",
            IsDeleted = false
        };
        var roles = new List<string> { "Manager", "Approver" };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRoleRepository
            .Setup(x => x.GetRoleNamesByUserIdAsync(userId, default))
            .ReturnsAsync(roles);

        // Act
        var result = await _accountService.GetUserProfileAsync(userId);

        // Assert
        result.Should().BeOfType<UserProfileResponse>();
        result!.Username.Should().Be(user.Username);
        result.DisplayName.Should().Be(user.DisplayName);
        result.Roles.Should().Equal(roles);
    }

    #endregion
}
