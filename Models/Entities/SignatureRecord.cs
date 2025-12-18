namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 簽名記錄實體
/// </summary>
/// <remarks>
/// 對應資料庫 signature_records 資料表,用於統一管理線下(Base64 PNG)與線上(Dropbox Sign)簽名
/// </remarks>
public class SignatureRecord
{
    /// <summary>
    /// 簽名記錄唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 所屬服務單 ID
    /// </summary>
    public Guid ServiceOrderId { get; set; }

    /// <summary>
    /// 文件類型
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// 簽名方式 (OFFLINE/ONLINE)
    /// </summary>
    public string SignatureType { get; set; } = string.Empty;

    /// <summary>
    /// 簽名資料 (線下: Base64 PNG)
    /// </summary>
    public string? SignatureData { get; set; }

    /// <summary>
    /// Dropbox Sign 請求 ID (線上專屬)
    /// </summary>
    public string? DropboxSignRequestId { get; set; }

    /// <summary>
    /// Dropbox Sign 狀態
    /// </summary>
    public string? DropboxSignStatus { get; set; }

    /// <summary>
    /// Dropbox Sign 簽名連結
    /// </summary>
    public string? DropboxSignUrl { get; set; }

    /// <summary>
    /// 簽名者姓名
    /// </summary>
    public string SignerName { get; set; } = string.Empty;

    /// <summary>
    /// 簽名時間 (UTC)
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 建立者 ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 最後更新者 ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}
