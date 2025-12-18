namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 身分證 OCR 辨識請求模型
/// </summary>
/// <remarks>
/// 前端上傳身分證圖片 (Base64) 後,由後端呼叫 OCR 服務進行辨識
/// </remarks>
public class OcrIdCardRequest
{
    /// <summary>
    /// 身分證圖片 Base64 (不含 data: 前綴)
    /// </summary>
    public string ImageBase64 { get; set; } = string.Empty;

    /// <summary>
    /// 圖片 MIME 類型
    /// </summary>
    /// <remarks>
    /// 允許: image/jpeg, image/png
    /// </remarks>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 原始檔名
    /// </summary>
    public string? FileName { get; set; }
}
