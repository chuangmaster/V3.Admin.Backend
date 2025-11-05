namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 更新角色請求
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// 角色名稱
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 版本號（用於樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
