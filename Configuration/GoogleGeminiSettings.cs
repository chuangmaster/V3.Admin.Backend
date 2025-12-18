using Microsoft.Extensions.Configuration;

namespace V3.Admin.Backend.Configuration;

/// <summary>
/// Google Gemini 設定模型
/// </summary>
public class GoogleGeminiSettings
{
    /// <summary>
    /// Google Gemini API Key
    /// </summary>
    [ConfigurationKeyName("GOOGLE_GEMINI_API_KEY")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gemini 模型名稱
    /// </summary>
    [ConfigurationKeyName("GOOGLE_GEMINI_MODEL")]
    public string Model { get; set; } = "gemini-1.5-flash";

    /// <summary>
    /// Google Generative Language API Base URL
    /// </summary>
    [ConfigurationKeyName("GOOGLE_GEMINI_BASE_URL")]
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";

    /// <summary>
    /// 呼叫逾時秒數
    /// </summary>
    [ConfigurationKeyName("GOOGLE_GEMINI_TIMEOUT_SECONDS")]
    public int TimeoutSeconds { get; set; } = 15;
}
