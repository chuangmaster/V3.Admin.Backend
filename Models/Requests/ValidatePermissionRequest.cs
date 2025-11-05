namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 驗證單一權限的請求模型
/// </summary>
public class ValidatePermissionRequest
{
    /// <summary>
    /// 權限代碼
    /// </summary>
    public string PermissionCode { get; set; } = null!;
}
