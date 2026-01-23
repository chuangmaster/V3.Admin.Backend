namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 登入請求模型
/// </summary>
/// <remarks>
/// 用於接收客戶端的登入請求
/// </remarks>
public class LoginRequest
{
    /// <summary>
    /// 帳號名稱
    /// </summary>
    /// <remarks>
    /// 長度限制: 3-20 字元
    /// </remarks>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 密碼
    /// </summary>
    /// <remarks>
    /// 最少 8 字元
    /// </remarks>
    public string Password { get; set; } = string.Empty;
}
