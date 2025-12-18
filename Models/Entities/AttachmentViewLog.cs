namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 附件查看日誌實體
/// </summary>
/// <remarks>
/// 對應資料庫 attachment_view_logs 資料表,用於個資稽核(查看/下載敏感附件)
/// </remarks>
public class AttachmentViewLog
{
    /// <summary>
    /// 日誌唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 附件 ID
    /// </summary>
    public Guid AttachmentId { get; set; }

    /// <summary>
    /// 服務單 ID
    /// </summary>
    public Guid ServiceOrderId { get; set; }

    /// <summary>
    /// 查看者 ID
    /// </summary>
    public Guid ViewedBy { get; set; }

    /// <summary>
    /// 查看時間 (UTC)
    /// </summary>
    public DateTime ViewedAt { get; set; }

    /// <summary>
    /// 操作類型 (VIEW/DOWNLOAD)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 來源 IP
    /// </summary>
    public string? IpAddress { get; set; }
}
