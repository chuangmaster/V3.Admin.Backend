namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 身分證 OCR 辨識結果回應模型
/// </summary>
/// <remarks>
/// 用於回傳辨識出的姓名、身分證字號與信心度
/// </remarks>
public class OcrResultResponse
{
    /// <summary>
    /// 姓名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 身分證字號/外籍人士格式
    /// </summary>
    public string? IdNumber { get; set; }

    /// <summary>
    /// 信心度 (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 是否為低信心度 (小於 0.8)
    /// </summary>
    public bool IsLowConfidence => Confidence < 0.8d;
}
