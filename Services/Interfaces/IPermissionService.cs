using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 權限服務介面
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 建立權限
    /// </summary>
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, Guid createdBy);

    /// <summary>
    /// 取得權限
    /// </summary>
    Task<PermissionDto?> GetPermissionByIdAsync(Guid id);

    /// <summary>
    /// 取得所有權限 (分頁)
    /// </summary>
    Task<(List<PermissionDto> Items, int TotalCount)> GetPermissionsAsync(int pageNumber, int pageSize, string? searchKeyword = null, string? permissionType = null);

    /// <summary>
    /// 更新權限
    /// </summary>
    Task<PermissionDto> UpdatePermissionAsync(Guid id, UpdatePermissionRequest request, Guid updatedBy);

    /// <summary>
    /// 刪除權限
    /// </summary>
    Task DeletePermissionAsync(Guid id, DeletePermissionRequest request, Guid deletedBy);
}
