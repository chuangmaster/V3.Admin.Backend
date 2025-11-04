namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 刪除權限請求
/// </summary>
public class DeletePermissionRequest
{
    /// <summary>
    /// 版本號（用於樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
