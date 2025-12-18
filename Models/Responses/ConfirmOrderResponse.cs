namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 確認服務單並儲存最終文件回應模型
/// </summary>
/// <remarks>
/// 回傳已儲存的合約附件資訊與可下載的 SAS 連結。
/// </remarks>
public class ConfirmOrderResponse
{
    /// <summary>
    /// 新建立的附件 ID
    /// </summary>
    public Guid AttachmentId { get; set; }

    /// <summary>
    /// 新建立的簽名記錄 ID
    /// </summary>
    public Guid SignatureRecordId { get; set; }

    /// <summary>
    /// Blob 路徑
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// SAS 下載連結
    /// </summary>
    public string SasUrl { get; set; } = string.Empty;

    /// <summary>
    /// SAS 過期時間 (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
