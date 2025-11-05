using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 用戶角色服務介面
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// 為用戶指派角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="request">指派請求</param>
    /// <param name="operatorId">操作員 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指派成功的角色數量</returns>
    Task<int> AssignRolesAsync(
        Guid userId,
        AssignUserRoleRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 移除用戶的特定角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="request">移除請求</param>
    /// <param name="operatorId">操作員 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功移除</returns>
    Task<bool> RemoveRoleAsync(
        Guid userId,
        RemoveUserRoleRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 查詢用戶的所有角色
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用戶角色 DTO 列表</returns>
    Task<List<UserRoleDto>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
