using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 權限資料存取介面
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// 建立權限
    /// </summary>
    Task<Permission> CreateAsync(Permission permission);

    /// <summary>
    /// 取得權限 (根據 ID)
    /// </summary>
    Task<Permission?> GetByIdAsync(Guid id);

    /// <summary>
    /// 取得權限 (根據 PermissionCode)
    /// </summary>
    Task<Permission?> GetByCodeAsync(string permissionCode);

    /// <summary>
    /// 取得所有權限 (分頁)
    /// </summary>
    Task<(List<Permission> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? searchKeyword = null, string? permissionType = null);

    /// <summary>
    /// 更新權限
    /// </summary>
    Task<bool> UpdateAsync(Permission permission);

    /// <summary>
    /// 刪除權限 (軟刪除)
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid deletedBy);

    /// <summary>
    /// 檢查權限是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// 檢查權限是否被使用 (在任何角色中)
    /// </summary>
    Task<bool> IsInUseAsync(Guid id);

    /// <summary>
    /// 檢查權限代碼是否唯一
    /// </summary>
    Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null);
}
