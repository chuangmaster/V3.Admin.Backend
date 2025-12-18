namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// Dropbox Sign Webhook 事件實體
/// </summary>
/// <remarks>
/// 對應資料庫 dropbox_sign_webhook_events 資料表,用於防止事件重複處理
/// </remarks>
public class DropboxSignWebhookEvent
{
    /// <summary>
    /// 事件唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 事件雜湊值
    /// </summary>
    public string EventHash { get; set; } = string.Empty;

    /// <summary>
    /// 處理時間 (UTC)
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
