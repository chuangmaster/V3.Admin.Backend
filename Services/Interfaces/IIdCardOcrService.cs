namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 身分證 OCR 辨識服務介面
/// </summary>
/// <remarks>
/// 使用 Azure Vision 進行文字擷取,再透過 Google Gemini 進行結構化解析與驗證
/// </remarks>
public interface IIdCardOcrService
{
    /// <summary>
    /// 辨識身分證姓名與身分證字號
    /// </summary>
    /// <param name="imageBytes">身分證圖片位元組</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>姓名、身分證字號、信心度 (0-1)</returns>
    Task<(string? Name, string? IdNumber, double Confidence)> RecognizeAsync(
        byte[] imageBytes,
        CancellationToken cancellationToken = default
    );
}
