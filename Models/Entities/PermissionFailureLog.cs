namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 權限驗證失敗日誌實體
/// 記錄用戶權限驗證失敗的嘗試
/// </summary>
public class PermissionFailureLog
{
    /// <summary>
    /// 日誌記錄 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用戶 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用戶名稱
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// 嘗試訪問的資源
    /// </summary>
    public string AttemptedResource { get; set; } = null!;

    /// <summary>
    /// 失敗原因
    /// </summary>
    public string FailureReason { get; set; } = null!;

    /// <summary>
    /// 嘗試時間（UTC）
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// 客戶端 IP 位址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用戶代理程式
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 請求追蹤 ID
    /// </summary>
    public string? TraceId { get; set; }
}
