namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 身分證 OCR 辨識結果 DTO
/// </summary>
/// <remarks>
/// 用於回傳 AI 辨識出的姓名與身分證字號,以及信心度 (0-1)
/// </remarks>
public class OcrResultDto
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
