namespace V3.Admin.Backend.Configuration;

/// <summary>
/// 資料庫連線設定模型
/// </summary>
/// <remarks>
/// 對應 appsettings.json 中的 ConnectionStrings 區段
/// </remarks>
public class DatabaseSettings
{
    /// <summary>
    /// 組態區段名稱
    /// </summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// 預設連線字串名稱
    /// </summary>
    public string DefaultConnection { get; set; } = string.Empty;
}
