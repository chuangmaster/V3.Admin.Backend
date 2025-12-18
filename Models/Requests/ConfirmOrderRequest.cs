namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 確認服務單並儲存最終文件請求模型
/// </summary>
/// <remarks>
/// 用於線下簽名流程最後一步：將已合併簽章的 PDF 儲存至 Blob Storage，並建立簽名記錄。
/// </remarks>
public class ConfirmOrderRequest
{
    /// <summary>
    /// 文件類型
    /// </summary>
    /// <remarks>
    /// 例如: BUYBACK_CONTRACT、ONE_TIME_TRADE、CONSIGNMENT_CONTRACT
    /// </remarks>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// 最終 PDF Base64 (不含 data: 前綴)
    /// </summary>
    public string PdfBase64 { get; set; } = string.Empty;

    /// <summary>
    /// 簽名圖片 Base64 (PNG，不含 data: 前綴)
    /// </summary>
    public string? SignatureBase64Png { get; set; }

    /// <summary>
    /// 簽名者姓名
    /// </summary>
    public string? SignerName { get; set; }

    /// <summary>
    /// 原始檔名（選填）
    /// </summary>
    public string? FileName { get; set; }
}
