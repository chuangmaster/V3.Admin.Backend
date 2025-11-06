using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using V3.Admin.Backend.Services.Interfaces;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

public class PermissionServiceTests
{
    private readonly Mock<IPermissionRepository> _mockRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<PermissionService>> _mockLogger;
    private readonly PermissionService _service;

    public PermissionServiceTests()
    {
        _mockRepository = new Mock<IPermissionRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<PermissionService>>();
        _service = new PermissionService(
            _mockRepository.Object,
            _mockAuditLogService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreatePermissionAsync_WithValidRequest_ReturnsPermissionDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreatePermissionRequest
        {
            PermissionCode = "test.perm.read",
            Name = "Test Permission",
            Description = "Test description",
            PermissionType = "route",
            RoutePath = "/api/test",
        };

        _mockRepository
            .Setup(r => r.IsCodeUniqueAsync(request.PermissionCode, null))
            .ReturnsAsync(true);

        var createdPermission = new Models.Entities.Permission
        {
            Id = Guid.NewGuid(),
            PermissionCode = request.PermissionCode,
            Name = request.Name,
            Description = request.Description,
            PermissionType = request.PermissionType,
            RoutePath = request.RoutePath,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Version = 1,
            IsDeleted = false,
        };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Models.Entities.Permission>()))
            .ReturnsAsync(createdPermission);

        // Act
        var result = await _service.CreatePermissionAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.PermissionCode.Should().Be(request.PermissionCode);
        result.Name.Should().Be(request.Name);
        result.Id.Should().Be(createdPermission.Id);

        _mockRepository.Verify(r => r.IsCodeUniqueAsync(request.PermissionCode, null), Times.Once);
        _mockRepository.Verify(
            r => r.CreateAsync(It.IsAny<Models.Entities.Permission>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CreatePermissionAsync_WithDuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreatePermissionRequest
        {
            PermissionCode = "duplicate.code",
            Name = "Test",
            Description = "Test",
            PermissionType = "route",
            RoutePath = "/api/test",
        };

        _mockRepository
            .Setup(r => r.IsCodeUniqueAsync(request.PermissionCode, null))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePermissionAsync(request, userId)
        );

        _mockRepository.Verify(
            r => r.CreateAsync(It.IsAny<Models.Entities.Permission>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetPermissionByIdAsync_WithValidId_ReturnsPermissionDto()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var permission = new Models.Entities.Permission
        {
            Id = permissionId,
            PermissionCode = "test.read",
            Name = "Test Permission",
            Description = "Test",
            PermissionType = "route",
            RoutePath = "/api/test",
            CreatedAt = DateTime.UtcNow,
            Version = 1,
            IsDeleted = false,
        };

        _mockRepository.Setup(r => r.GetByIdAsync(permissionId)).ReturnsAsync(permission);

        // Act
        var result = await _service.GetPermissionByIdAsync(permissionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(permissionId);
        result.PermissionCode.Should().Be("test.read");

        _mockRepository.Verify(r => r.GetByIdAsync(permissionId), Times.Once);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(invalidId))
            .ReturnsAsync((Models.Entities.Permission?)null);

        // Act
        var result = await _service.GetPermissionByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPermissionsAsync_WithValidParameters_ReturnsPaginatedResult()
    {
        // Arrange
        var permissions = new List<Models.Entities.Permission>
        {
            new Models.Entities.Permission
            {
                Id = Guid.NewGuid(),
                PermissionCode = "perm.1",
                Name = "Permission 1",
                Description = "Desc 1",
                PermissionType = "route",
                RoutePath = "/api/perm1",
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                IsDeleted = false,
            },
            new Models.Entities.Permission
            {
                Id = Guid.NewGuid(),
                PermissionCode = "perm.2",
                Name = "Permission 2",
                Description = "Desc 2",
                PermissionType = "function",
                RoutePath = null,
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                IsDeleted = false,
            },
        };

        _mockRepository.Setup(r => r.GetAllAsync(1, 20, null, null)).ReturnsAsync((permissions, 2));

        // Act
        var (items, totalCount) = await _service.GetPermissionsAsync(1, 20, null, null);

        // Assert
        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
        items[0].PermissionCode.Should().Be("perm.1");
        items[1].PermissionCode.Should().Be("perm.2");
    }

    [Fact]
    public async Task UpdatePermissionAsync_WithValidRequest_ReturnsUpdatedPermissionDto()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        int version = 1;

        var existingPermission = new Models.Entities.Permission
        {
            Id = permissionId,
            PermissionCode = "test.perm",
            Name = "Original Name",
            Description = "Original",
            PermissionType = "route",
            RoutePath = "/api/original",
            CreatedAt = DateTime.UtcNow,
            Version = version,
            IsDeleted = false,
        };

        var updateRequest = new UpdatePermissionRequest
        {
            Name = "Updated Name",
            Description = "Updated",
            RoutePath = "/api/updated",
            Version = version,
        };

        _mockRepository.Setup(r => r.GetByIdAsync(permissionId)).ReturnsAsync(existingPermission);

        var updatedPermission = new Models.Entities.Permission
        {
            Id = permissionId,
            PermissionCode = existingPermission.PermissionCode,
            Name = updateRequest.Name,
            Description = updateRequest.Description,
            PermissionType = existingPermission.PermissionType,
            RoutePath = updateRequest.RoutePath,
            CreatedAt = existingPermission.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            Version = version + 1,
            IsDeleted = false,
        };

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Models.Entities.Permission>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdatePermissionAsync(permissionId, updateRequest, userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated");

        _mockRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Models.Entities.Permission>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdatePermissionAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdatePermissionRequest
        {
            Name = "Updated",
            Description = "Updated",
            RoutePath = "/api/updated",
            Version = 1,
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(invalidId))
            .ReturnsAsync((Models.Entities.Permission?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdatePermissionAsync(invalidId, updateRequest, userId)
        );
    }

    [Fact]
    public async Task UpdatePermissionAsync_WithVersionMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var existingPermission = new Models.Entities.Permission
        {
            Id = permissionId,
            PermissionCode = "test.perm",
            Name = "Original Name",
            Description = "Original",
            PermissionType = "route",
            RoutePath = "/api/original",
            CreatedAt = DateTime.UtcNow,
            Version = 1,
            IsDeleted = false,
        };

        var updateRequest = new UpdatePermissionRequest
        {
            Name = "Updated",
            Description = "Updated",
            RoutePath = "/api/updated",
            Version = 999,
        };

        _mockRepository.Setup(r => r.GetByIdAsync(permissionId)).ReturnsAsync(existingPermission);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdatePermissionAsync(permissionId, updateRequest, userId)
        );
    }

    [Fact]
    public async Task DeletePermissionAsync_WithValidRequest_Succeeds()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        int version = 1;

        var existingPermission = new Models.Entities.Permission
        {
            Id = permissionId,
            PermissionCode = "test.perm",
            Name = "Permission to Delete",
            Description = "To delete",
            PermissionType = "route",
            RoutePath = "/api/test",
            CreatedAt = DateTime.UtcNow,
            Version = version,
            IsDeleted = false,
        };

        var deleteRequest = new DeletePermissionRequest { Version = version };

        _mockRepository.Setup(r => r.GetByIdAsync(permissionId)).ReturnsAsync(existingPermission);

        _mockRepository.Setup(r => r.IsInUseAsync(permissionId)).ReturnsAsync(false);

        _mockRepository.Setup(r => r.DeleteAsync(permissionId, userId)).ReturnsAsync(true);

        // Act
        await _service.DeletePermissionAsync(permissionId, deleteRequest, userId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(permissionId, userId), Times.Once);
    }

    [Fact]
    public async Task DeletePermissionAsync_WithInUsePermission_ThrowsInvalidOperationException()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        int version = 1;

        var existingPermission = new Models.Entities.Permission
        {
            Id = permissionId,
            PermissionCode = "in.use.perm",
            Name = "Permission In Use",
            Description = "In use",
            PermissionType = "route",
            RoutePath = "/api/test",
            CreatedAt = DateTime.UtcNow,
            Version = version,
            IsDeleted = false,
        };

        var deleteRequest = new DeletePermissionRequest { Version = version };

        _mockRepository.Setup(r => r.GetByIdAsync(permissionId)).ReturnsAsync(existingPermission);

        _mockRepository.Setup(r => r.IsInUseAsync(permissionId)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeletePermissionAsync(permissionId, deleteRequest, userId)
        );

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeletePermissionAsync_WithVersionMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var existingPermission = new Models.Entities.Permission
        {
            Id = permissionId,
            PermissionCode = "test.perm",
            Name = "Permission",
            Description = "Desc",
            PermissionType = "route",
            RoutePath = "/api/test",
            CreatedAt = DateTime.UtcNow,
            Version = 1,
            IsDeleted = false,
        };

        var deleteRequest = new DeletePermissionRequest { Version = 999 };

        _mockRepository.Setup(r => r.GetByIdAsync(permissionId)).ReturnsAsync(existingPermission);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeletePermissionAsync(permissionId, deleteRequest, userId)
        );
    }
}
