namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 更新權限請求
/// </summary>
public class UpdatePermissionRequest
{
    /// <summary>
    /// 權限名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 權限描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 版本號（用於樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
