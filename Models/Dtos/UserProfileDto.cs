namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 用戶個人資料 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層內部傳遞用戶個人資料，包含用戶基本資訊與角色清單
/// </remarks>
public class UserProfileDto
{
    /// <summary>
    /// 用戶名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱（可為 null）
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 角色名稱清單（若無角色則為空陣列）
    /// </summary>
    public List<string> Roles { get; set; } = [];
}
