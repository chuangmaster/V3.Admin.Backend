namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 帳號資訊回應模型
/// </summary>
/// <remarks>
/// 用於回傳帳號基本資訊給客戶端,不包含敏感資訊
/// </remarks>
public class AccountResponse
{
    /// <summary>
    /// 使用者 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 帳號名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 資料版本號
    /// </summary>
    public int Version { get; set; }
}
