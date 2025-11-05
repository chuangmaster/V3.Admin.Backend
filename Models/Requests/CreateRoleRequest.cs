namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 建立角色請求
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// 角色名稱
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }
}
