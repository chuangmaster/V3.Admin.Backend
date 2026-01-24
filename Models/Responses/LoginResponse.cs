namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 登入成功回應模型
/// </summary>
/// <remarks>
/// 包含 JWT Token 與使用者資訊
/// </remarks>
public class LoginResponse
{
    /// <summary>
    /// JWT Access Token
    /// </summary>
    /// <remarks>
    /// 前端應將此 Token 儲存並在後續請求的 Authorization 標頭中攜帶
    /// 格式: Bearer {token}
    /// </remarks>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token 過期時間 (UTC)
    /// </summary>
    /// <remarks>
    /// Token 過期後需要重新登入
    /// 預設有效期為 1 小時
    /// </remarks>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 使用者資訊
    /// </summary>
    public AccountResponse User { get; set; } = new();
}
