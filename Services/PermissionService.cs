using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 權限服務實作
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(IPermissionRepository permissionRepository, ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, Guid createdBy)
    {
        // 檢查權限代碼唯一性
        var isUnique = await _permissionRepository.IsCodeUniqueAsync(request.PermissionCode);
        if (!isUnique)
        {
            throw new InvalidOperationException($"權限代碼 '{request.PermissionCode}' 已存在");
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionCode = request.PermissionCode,
            Name = request.Name,
            Description = request.Description,
            PermissionType = request.PermissionType,
            RoutePath = request.RoutePath,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            Version = 1
        };

        var created = await _permissionRepository.CreateAsync(permission);
        return MapToDto(created);
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(Guid id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        return permission != null ? MapToDto(permission) : null;
    }

    public async Task<(List<PermissionDto> Items, int TotalCount)> GetPermissionsAsync(
        int pageNumber, 
        int pageSize, 
        string? searchKeyword = null, 
        string? permissionType = null)
    {
        var (items, totalCount) = await _permissionRepository.GetAllAsync(pageNumber, pageSize, searchKeyword, permissionType);
        return (items.Select(MapToDto).ToList(), totalCount);
    }

    public async Task<PermissionDto> UpdatePermissionAsync(Guid id, UpdatePermissionRequest request, Guid updatedBy)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            throw new KeyNotFoundException($"權限 '{id}' 不存在");
        }

        permission.Name = request.Name;
        permission.Description = request.Description;
        permission.RoutePath = request.RoutePath;
        permission.UpdatedAt = DateTime.UtcNow;
        permission.UpdatedBy = updatedBy;
        permission.Version = request.Version;

        var success = await _permissionRepository.UpdateAsync(permission);
        if (!success)
        {
            throw new InvalidOperationException("權限更新失敗，可能是版本不符或已被刪除");
        }

        // 重新取得更新後的權限
        var updated = await _permissionRepository.GetByIdAsync(id);
        return MapToDto(updated!);
    }

    public async Task DeletePermissionAsync(Guid id, DeletePermissionRequest request, Guid deletedBy)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            throw new KeyNotFoundException($"權限 '{id}' 不存在");
        }

        // 檢查版本號
        if (permission.Version != request.Version)
        {
            throw new InvalidOperationException("版本不符，權限已被其他使用者修改");
        }

        // 檢查是否被使用
        var isInUse = await _permissionRepository.IsInUseAsync(id);
        if (isInUse)
        {
            throw new InvalidOperationException("該權限正被角色使用，無法刪除");
        }

        var success = await _permissionRepository.DeleteAsync(id, deletedBy);
        if (!success)
        {
            throw new InvalidOperationException("權限刪除失敗");
        }

        _logger.LogInformation("權限已刪除: {Id}", id);
    }

    /// <summary>
    /// 將 Permission 實體映射為 PermissionDto
    /// </summary>
    private static PermissionDto MapToDto(Permission permission) =>
        new()
        {
            Id = permission.Id,
            PermissionCode = permission.PermissionCode,
            Name = permission.Name,
            Description = permission.Description,
            PermissionType = permission.PermissionType,
            RoutePath = permission.RoutePath,
            CreatedAt = permission.CreatedAt,
            Version = permission.Version
        };
}
