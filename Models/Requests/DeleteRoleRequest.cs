namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 刪除角色請求
/// </summary>
public class DeleteRoleRequest
{
    /// <summary>
    /// 版本號（用於樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
