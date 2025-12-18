using Microsoft.Extensions.Configuration;

namespace V3.Admin.Backend.Configuration;

/// <summary>
/// Azure Vision (OCR) 設定模型
/// </summary>
public class AzureVisionSettings
{
    /// <summary>
    /// Azure Vision Endpoint
    /// </summary>
    [ConfigurationKeyName("AZURE_VISION_ENDPOINT")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure Vision API Key
    /// </summary>
    [ConfigurationKeyName("AZURE_VISION_API_KEY")]
    public string ApiKey { get; set; } = string.Empty;
}
