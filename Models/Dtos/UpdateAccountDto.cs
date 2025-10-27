namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 更新帳號 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層處理更新帳號邏輯
/// </remarks>
public class UpdateAccountDto
{
    /// <summary>
    /// 帳號 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 當前版本號 (樂觀並發控制)
    /// </summary>
    public int Version { get; set; }
}
