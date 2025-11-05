namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 移除用戶角色的請求模型
/// </summary>
public class RemoveUserRoleRequest
{
    /// <summary>
    /// 要移除的角色 ID
    /// </summary>
    public Guid RoleId { get; set; }
}
