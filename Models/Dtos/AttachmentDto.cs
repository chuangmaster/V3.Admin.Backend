namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 附件 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層傳遞服務單附件資訊 (例如身分證明、合約 PDF)
/// </remarks>
public class AttachmentDto
{
    /// <summary>
    /// 附件 ID
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
    /// 是否已刪除 (軟刪除)
    /// </summary>
    public bool IsDeleted { get; set; }
}
