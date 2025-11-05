using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 角色權限關聯資料存取層介面
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// 為角色分配權限（支援批次新增）
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="permissionIds">權限 ID 陣列</param>
    /// <param name="assignedBy">分配者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分配的記錄數</returns>
    Task<int> AssignPermissionsAsync(Guid roleId, List<Guid> permissionIds, Guid? assignedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除角色的特定權限
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="permissionId">權限 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    Task<bool> RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得角色的所有權限
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>權限列表</returns>
    Task<List<Permission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除角色的所有權限
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清除的記錄數</returns>
    Task<int> ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查角色是否已擁有特定權限
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="permissionId">權限 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否擁有</returns>
    Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
}
