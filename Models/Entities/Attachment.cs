namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 附件實體
/// </summary>
/// <remarks>
/// 對應資料庫 attachments 資料表,用於儲存身分證明文件、合約文件等檔案資訊
/// </remarks>
public class Attachment
{
    /// <summary>
    /// 附件唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 所屬服務單 ID
    /// </summary>
    public Guid ServiceOrderId { get; set; }

    /// <summary>
    /// 附件類型
    /// </summary>
    public string AttachmentType { get; set; } = string.Empty;

    /// <summary>
    /// 原始檔名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Azure Blob Storage 路徑
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// 檔案大小 (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME 類型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 上傳時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 上傳者 ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 最後更新者 ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// 是否已刪除 (軟刪除標記)
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 刪除時間 (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 刪除操作者 ID
    /// </summary>
    public Guid? DeletedBy { get; set; }
}
