namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 為用戶指派角色的請求模型
/// </summary>
public class AssignUserRoleRequest
{
    /// <summary>
    /// 角色 ID 陣列
    /// </summary>
    public List<Guid> RoleIds { get; set; } = [];
}
