namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 為角色分配權限的請求 DTO
/// </summary>
public class AssignPermissionsRequest
{
    /// <summary>
    /// 權限 ID 列表
    /// </summary>
    public List<Guid> PermissionIds { get; set; } = new();
}
