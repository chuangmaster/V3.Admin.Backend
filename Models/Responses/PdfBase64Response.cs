namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// PDF Base64 回應模型
/// </summary>
/// <remarks>
/// 用於回傳 PDF 內容（Base64 字串），供前端預覽或下一步流程使用。
/// </remarks>
public class PdfBase64Response
{
    /// <summary>
    /// PDF 內容 Base64
    /// </summary>
    public string PdfBase64 { get; set; } = string.Empty;

    /// <summary>
    /// MIME 類型
    /// </summary>
    public string ContentType { get; set; } = "application/pdf";
}
