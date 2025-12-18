namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 確認服務單並儲存最終文件結果 DTO
/// </summary>
/// <remarks>
/// 由 Service 層回傳合約附件與簽名記錄資訊，供 Controller 轉換為回應模型。
/// </remarks>
public class ConfirmOrderResultDto
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
    public Uri SasUri { get; set; } = new Uri("about:blank");

    /// <summary>
    /// SAS 過期時間 (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
