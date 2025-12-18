namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 簽名記錄 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層傳遞線下/線上簽名的追蹤資訊
/// </remarks>
public class SignatureRecordDto
{
    /// <summary>
    /// 簽名記錄 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 所屬服務單 ID
    /// </summary>
    public Guid ServiceOrderId { get; set; }

    /// <summary>
    /// 簽名文件類型
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// 簽名方式 (OFFLINE/ONLINE)
    /// </summary>
    public string SignatureType { get; set; } = string.Empty;

    /// <summary>
    /// 線下簽名資料 (Base64 PNG)
    /// </summary>
    public string? SignatureData { get; set; }

    /// <summary>
    /// Dropbox Sign Request ID (線上簽名專屬)
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
    public string? SignerName { get; set; }

    /// <summary>
    /// 簽名時間 (UTC)
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
