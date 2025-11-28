using System.Text.Json;
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
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IAuditLogService auditLogService,
        ILogger<PermissionService> logger
    )
    {
        _permissionRepository = permissionRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<PermissionDto> CreatePermissionAsync(
        CreatePermissionRequest request,
        Guid createdBy
    )
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
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            Version = 1,
        };

        var created = await _permissionRepository.CreateAsync(permission);

        // 同步記錄稽核日誌
        try
        {
            var afterState = JsonSerializer.Serialize(
                new
                {
                    created.Id,
                    created.PermissionCode,
                    created.Name,
                    created.Description,
                    created.PermissionType,
                    created.CreatedAt,
                }
            );

            await _auditLogService.LogOperationAsync(
                createdBy,
                "system",
                "create",
                "permission",
                created.Id,
                beforeState: null,
                afterState: afterState
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄稽核日誌失敗: PermissionId={PermissionId}", created.Id);
        }

        return MapToDto(created);
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(Guid id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        return permission != null ? MapToDto(permission) : null;
    }

    public async Task<PagedResultDto<PermissionDto>> GetPermissionsAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null,
        string? permissionType = null
    )
    {
        var (items, totalCount) = await _permissionRepository.GetAllAsync(
            pageNumber,
            pageSize,
            searchKeyword,
            permissionType
        );
        return new PagedResultDto<PermissionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<PermissionDto> UpdatePermissionAsync(
        Guid id,
        UpdatePermissionRequest request,
        Guid updatedBy
    )
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            throw new KeyNotFoundException($"權限 '{id}' 不存在");
        }

        // 記錄更新前的狀態
        var beforeState = JsonSerializer.Serialize(
            new
            {
                permission.Id,
                permission.PermissionCode,
                permission.Name,
                permission.Description,
                permission.Version,
            }
        );

        permission.Name = request.Name;
        permission.Description = request.Description;
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

        // 同步記錄稽核日誌
        try
        {
            var afterState = JsonSerializer.Serialize(
                new
                {
                    updated!.Id,
                    updated.PermissionCode,
                    updated.Name,
                    updated.Description,
                    updated.Version,
                }
            );

            await _auditLogService.LogOperationAsync(
                updatedBy,
                "system",
                "update",
                "permission",
                id,
                beforeState: beforeState,
                afterState: afterState
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄稽核日誌失敗: PermissionId={PermissionId}", id);
        }

        return MapToDto(updated!);
    }

    public async Task DeletePermissionAsync(
        Guid id,
        DeletePermissionRequest request,
        Guid deletedBy
    )
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            throw new KeyNotFoundException($"權限 '{id}' 不存在");
        }

        // 記錄刪除前的狀態
        var beforeState = JsonSerializer.Serialize(
            new
            {
                permission.Id,
                permission.PermissionCode,
                permission.Name,
                permission.Description,
                permission.Version,
            }
        );

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

        // 同步記錄稽核日誌
        try
        {
            await _auditLogService.LogOperationAsync(
                deletedBy,
                "system",
                "delete",
                "permission",
                id,
                beforeState: beforeState,
                afterState: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄稽核日誌失敗: PermissionId={PermissionId}", id);
        }
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
            CreatedAt = permission.CreatedAt,
            Version = permission.Version,
        };
}
