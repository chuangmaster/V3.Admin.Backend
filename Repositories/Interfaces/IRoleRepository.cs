using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 角色資料存取層介面
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// 建立角色
    /// </summary>
    /// <param name="role">角色實體</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的角色</returns>
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 ID 取得角色
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色實體，若不存在返回 null</returns>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得所有角色（分頁）
    /// </summary>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表和總筆數</returns>
    Task<(List<Role> roles, int totalCount)> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="role">角色實體</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除角色（軟刪除）
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="deletedBy">刪除操作者 ID</param>
    /// <param name="version">版本號</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否刪除成功</returns>
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查角色是否存在
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查角色是否正在被使用（被任何用戶指派）
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否被使用</returns>
    Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查角色名稱是否已存在
    /// </summary>
    /// <param name="roleName">角色名稱</param>
    /// <param name="excludeId">排除的 ID（用於更新時檢查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已存在</returns>
    Task<bool> RoleNameExistsAsync(string roleName, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
