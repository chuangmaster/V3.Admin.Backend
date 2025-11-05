namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 分配角色權限請求
/// </summary>
public class AssignRolePermissionsRequest
{
    /// <summary>
    /// 權限 ID 陣列
    /// </summary>
    public List<Guid> PermissionIds { get; set; } = new();
}
