using Microsoft.Extensions.Configuration;

namespace V3.Admin.Backend.Configuration;

/// <summary>
/// Dropbox Sign 設定模型
/// </summary>
public class DropboxSignSettings
{
    /// <summary>
    /// Dropbox Sign API Key
    /// </summary>
    [ConfigurationKeyName("DROPBOX_SIGN_API_KEY")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Dropbox Sign Webhook 簽章驗證用的 Secret
    /// </summary>
    [ConfigurationKeyName("DROPBOX_SIGN_WEBHOOK_SECRET")]
    public string WebhookSecret { get; set; } = string.Empty;
}
