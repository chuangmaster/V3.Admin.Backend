using System.Globalization;
using System.Text.Json;
using FluentValidation;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 角色業務邏輯服務實作
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IValidator<CreateRoleRequest> _createValidator;
    private readonly IValidator<UpdateRoleRequest> _updateValidator;
    private readonly IValidator<AssignRolePermissionsRequest> _assignValidator;
    private readonly ILogger<RoleService> _logger;

    /// <summary>
    /// 初始化 RoleService
    /// </summary>
    public RoleService(
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IPermissionRepository permissionRepository,
        IAuditLogService auditLogService,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator,
        IValidator<AssignRolePermissionsRequest> assignValidator,
        ILogger<RoleService> logger
    )
    {
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _permissionRepository = permissionRepository;
        _auditLogService = auditLogService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
        _logger = logger;
    }

    public async Task<RoleDto> CreateRoleAsync(
        CreateRoleRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        // 驗證請求
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 檢查角色名稱唯一性
        var roleNameExists = await _roleRepository.RoleNameExistsAsync(
            request.RoleName,
            null,
            cancellationToken
        );
        if (roleNameExists)
        {
            _logger.LogWarning("角色建立失敗：角色名稱已存在: {RoleName}", request.RoleName);
            throw new InvalidOperationException($"角色名稱 '{request.RoleName}' 已存在");
        }

        // 建立角色
        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = request.RoleName,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = operatorId,
            IsDeleted = false,
            Version = 1,
        };

        var createdRole = await _roleRepository.CreateAsync(role, cancellationToken);
        _logger.LogInformation(
            "角色建立成功: {RoleName} ({Id})",
            createdRole.RoleName,
            createdRole.Id
        );

        // TODO: 記錄稽核日誌（T095-T097 中實作）

        return MapToDto(createdRole);
    }

    public async Task<RoleDto?> GetRoleByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        return role != null ? MapToDto(role) : null;
    }

    public async Task<(List<RoleDto> roles, int totalCount)> GetRolesAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        var (roles, totalCount) = await _roleRepository.GetAllAsync(
            pageNumber,
            pageSize,
            cancellationToken
        );
        var roleDtos = roles.Select(MapToDto).ToList();
        return (roleDtos, totalCount);
    }

    public async Task<RoleDetailDto?> GetRoleDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        if (role == null)
        {
            return null;
        }

        var permissions = await _rolePermissionRepository.GetRolePermissionsAsync(
            id,
            cancellationToken
        );
        var permissionDtos = permissions.Select(MapPermissionToDto).ToList();

        return new RoleDetailDto
        {
            Id = role.Id,
            RoleName = role.RoleName,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            Version = role.Version,
            Permissions = permissionDtos,
        };
    }

    public async Task<RoleDto> UpdateRoleAsync(
        Guid id,
        UpdateRoleRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        // 驗證請求
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 取得現有角色
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"角色 {id} 不存在");
        }

        // 檢查角色名稱唯一性（排除自己）
        if (role.RoleName != request.RoleName)
        {
            var roleNameExists = await _roleRepository.RoleNameExistsAsync(
                request.RoleName,
                id,
                cancellationToken
            );
            if (roleNameExists)
            {
                throw new InvalidOperationException($"角色名稱 '{request.RoleName}' 已存在");
            }
        }

        // 更新角色
        role.RoleName = request.RoleName;
        role.Description = request.Description;
        role.UpdatedBy = operatorId;
        role.Version = request.Version;

        var success = await _roleRepository.UpdateAsync(role, cancellationToken);
        if (!success)
        {
            _logger.LogWarning("角色更新失敗（版本衝突）: {Id}", id);
            throw new InvalidOperationException("角色已被其他使用者修改，請重新載入後再試");
        }

        // 重新取得更新後的角色
        var updatedRole = await _roleRepository.GetByIdAsync(id, cancellationToken);
        _logger.LogInformation(
            "角色更新成功: {RoleName} ({Id})",
            updatedRole!.RoleName,
            updatedRole.Id
        );

        // TODO: 記錄稽核日誌（T095-T097 中實作）

        return MapToDto(updatedRole);
    }

    public async Task<bool> DeleteRoleAsync(
        Guid id,
        DeleteRoleRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        // 取得角色
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        if (role == null)
        {
            return false;
        }

        // 檢查角色是否正在被使用
        var isInUse = await _roleRepository.IsInUseAsync(id, cancellationToken);
        if (isInUse)
        {
            _logger.LogWarning(
                "角色刪除失敗：角色正在被使用: {RoleName} ({Id})",
                role.RoleName,
                id
            );
            throw new InvalidOperationException($"該角色正被用戶指派，無法刪除");
        }

        // 刪除角色
        var success = await _roleRepository.DeleteAsync(
            id,
            operatorId,
            request.Version,
            cancellationToken
        );
        if (!success)
        {
            _logger.LogWarning("角色刪除失敗（版本衝突）: {Id}", id);
            throw new InvalidOperationException("角色已被其他使用者修改，請重新載入後再試");
        }

        _logger.LogInformation("角色已刪除: {RoleName} ({Id})", role.RoleName, id);

        // TODO: 記錄稽核日誌（T095-T097 中實作）

        return true;
    }

    public async Task<bool> AssignPermissionsAsync(
        Guid roleId,
        AssignRolePermissionsRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        // 驗證請求
        var validationResult = await _assignValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 檢查角色是否存在
        var roleExists = await _roleRepository.ExistsAsync(roleId, cancellationToken);
        if (!roleExists)
        {
            throw new KeyNotFoundException($"角色 {roleId} 不存在");
        }

        // 檢查所有權限是否存在
        foreach (var permissionId in request.PermissionIds)
        {
            var permissionExists = await _permissionRepository.ExistsAsync(permissionId);
            if (!permissionExists)
            {
                throw new KeyNotFoundException($"權限 {permissionId} 不存在");
            }
        }

        // 分配權限
        var count = await _rolePermissionRepository.AssignPermissionsAsync(
            roleId,
            request.PermissionIds,
            operatorId,
            cancellationToken
        );
        _logger.LogInformation("為角色分配了 {Count} 個權限: {RoleId}", count, roleId);

        // TODO: 記錄稽核日誌（T095-T097 中實作）

        return true;
    }

    public async Task<bool> RemovePermissionAsync(
        Guid roleId,
        Guid permissionId,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        // 檢查角色是否存在
        var roleExists = await _roleRepository.ExistsAsync(roleId, cancellationToken);
        if (!roleExists)
        {
            throw new KeyNotFoundException($"角色 {roleId} 不存在");
        }

        // 檢查權限是否存在
        var permissionExists = await _permissionRepository.ExistsAsync(permissionId);
        if (!permissionExists)
        {
            throw new KeyNotFoundException($"權限 {permissionId} 不存在");
        }

        // 移除權限
        var success = await _rolePermissionRepository.RemovePermissionAsync(
            roleId,
            permissionId,
            cancellationToken
        );
        if (success)
        {
            _logger.LogInformation(
                "已移除角色權限: {RoleId} - {PermissionId}",
                roleId,
                permissionId
            );
        }

        // TODO: 記錄稽核日誌（T095-T097 中實作）

        return success;
    }

    private RoleDto MapToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            RoleName = role.RoleName,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            Version = role.Version,
        };
    }

    private PermissionDto MapPermissionToDto(Permission permission)
    {
        return new PermissionDto
        {
            Id = permission.Id,
            PermissionCode = permission.PermissionCode,
            Name = permission.Name,
            Description = permission.Description,
            PermissionType = permission.PermissionType,
            CreatedAt = permission.CreatedAt,
            Version = permission.Version,
        };
    }
}
