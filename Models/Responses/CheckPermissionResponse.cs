namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 檢查權限回應 DTO
/// </summary>
/// <remarks>
/// 供前端查詢用戶是否擁有特定權限
/// </remarks>
public class CheckPermissionResponse
{
    /// <summary>
    /// 權限代碼
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 權限類型
    /// </summary>
    public string? PermissionType { get; set; }

    /// <summary>
    /// 用戶是否擁有此權限
    /// </summary>
    public bool HasPermission { get; set; }
}
