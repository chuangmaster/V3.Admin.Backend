using Microsoft.Extensions.Configuration;

namespace V3.Admin.Backend.Configuration;

/// <summary>
/// Azure Blob Storage 設定模型
/// </summary>
public class AzureBlobStorageSettings
{
    /// <summary>
    /// Azure Blob Storage 連線字串
    /// </summary>
    [ConfigurationKeyName("AZURE_BLOB_CONNECTION_STRING")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Blob Container 名稱
    /// </summary>
    /// <remarks>
    /// 若未設定則使用預設容器名稱,避免在程式碼中散落魔術字串
    /// </remarks>
    [ConfigurationKeyName("AZURE_BLOB_CONTAINER_NAME")]
    public string ContainerName { get; set; } = "service-order-attachments";
}
