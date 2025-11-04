namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 建立權限請求
/// </summary>
public class CreatePermissionRequest
{
    /// <summary>
    /// 權限代碼
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 權限名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 權限描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 權限類型 (route 或 function)
    /// </summary>
    public string PermissionType { get; set; } = string.Empty;

    /// <summary>
    /// 路由路徑（僅路由權限使用）
    /// </summary>
    public string? RoutePath { get; set; }
}
