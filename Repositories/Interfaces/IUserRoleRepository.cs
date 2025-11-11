using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 用戶角色倉儲介面
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>
    /// 為用戶指派角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="roleIds">角色 ID 列表</param>
    /// <param name="assignedBy">指派者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功指派的角色數量</returns>
    Task<int> AssignRolesAsync(
        Guid userId,
        List<Guid> roleIds,
        Guid assignedBy,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 移除用戶的特定角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="roleId">角色 ID</param>
    /// <param name="deletedBy">刪除者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功移除</returns>
    Task<bool> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        Guid deletedBy,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 查詢用戶的所有角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用戶角色列表</returns>
    Task<List<UserRole>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 檢查用戶是否擁有特定角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="roleId">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否擁有該角色</returns>
    Task<bool> HasRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 清除用戶的所有角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="deletedBy">刪除者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清除的角色數量</returns>
    Task<int> ClearUserRolesAsync(
        Guid userId,
        Guid deletedBy,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 查詢用戶的所有角色名稱
    /// 使用 LEFT JOIN 以確保沒有角色的用戶也能正確查詢
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色名稱清單（若無角色則回傳空清單）</returns>
    Task<List<string>> GetRoleNamesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
