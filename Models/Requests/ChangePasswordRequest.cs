namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 變更密碼請求模型
/// </summary>
/// <remarks>
/// 用於接收客戶端的變更密碼請求
/// </remarks>
public class ChangePasswordRequest
{
    /// <summary>
    /// 舊密碼
    /// </summary>
    /// <remarks>
    /// 用於驗證使用者身份
    /// </remarks>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密碼
    /// </summary>
    /// <remarks>
    /// 最少 8 字元,不可與舊密碼相同
    /// </remarks>
    public string NewPassword { get; set; } = string.Empty;
}
