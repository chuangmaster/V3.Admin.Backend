using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 角色業務邏輯服務介面
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// 建立角色
    /// </summary>
    /// <param name="request">建立角色請求</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的角色 DTO</returns>
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, Guid operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 ID 取得角色
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色 DTO，若不存在返回 null</returns>
    Task<RoleDto?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得所有角色（分頁）
    /// </summary>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表和總筆數</returns>
    Task<(List<RoleDto> roles, int totalCount)> GetRolesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得角色詳細資訊（包含權限）
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色詳細資訊 DTO</returns>
    Task<RoleDetailDto?> GetRoleDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="request">更新角色請求</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新後的角色 DTO</returns>
    Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleRequest request, Guid operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除角色
    /// </summary>
    /// <param name="id">角色 ID</param>
    /// <param name="request">刪除角色請求</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否刪除成功</returns>
    Task<bool> DeleteRoleAsync(Guid id, DeleteRoleRequest request, Guid operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 為角色分配權限
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="request">分配權限請求</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否分配成功</returns>
    Task<bool> AssignPermissionsAsync(Guid roleId, AssignRolePermissionsRequest request, Guid operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除角色的特定權限
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="permissionId">權限 ID</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    Task<bool> RemovePermissionAsync(Guid roleId, Guid permissionId, Guid operatorId, CancellationToken cancellationToken = default);
}
