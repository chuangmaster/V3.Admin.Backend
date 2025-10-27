namespace V3.Admin.Backend.Configuration;

/// <summary>
/// JWT 身份驗證設定模型
/// </summary>
/// <remarks>
/// 對應 appsettings.json 中的 JwtSettings 區段
/// </remarks>
public class JwtSettings
{
    /// <summary>
    /// 組態區段名稱
    /// </summary>
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// 密鑰 (用於 HMAC-SHA256 簽章)
    /// </summary>
    /// <remarks>
    /// 生產環境必須使用強密鑰且儲存於環境變數或 Key Vault
    /// 長度建議至少 32 字元
    /// </remarks>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 發行者 (Issuer)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 受眾 (Audience)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token 有效期 (分鐘)
    /// </summary>
    /// <remarks>
    /// 預設 60 分鐘 (1 小時)
    /// </remarks>
    public int ExpirationMinutes { get; set; } = 60;
}
