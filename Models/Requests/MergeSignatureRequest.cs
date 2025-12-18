namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 合併簽章請求模型
/// </summary>
/// <remarks>
/// 用於將 Base64 PNG 簽章依座標合併到指定 PDF 位置
/// </remarks>
public class MergeSignatureRequest
{
    /// <summary>
    /// PDF 內容 Base64
    /// </summary>
    public string PdfBase64 { get; set; } = string.Empty;

    /// <summary>
    /// 簽章圖片 Base64 (PNG)
    /// </summary>
    public string SignatureBase64Png { get; set; } = string.Empty;

    /// <summary>
    /// PDF 頁索引 (從 0 開始)
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// X 座標
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 座標
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 簽章寬度
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 簽章高度
    /// </summary>
    public double Height { get; set; }
}
