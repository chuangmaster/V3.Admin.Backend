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
    /// 帳號名稱
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱（可為 null）
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 角色名稱清單（若無角色則為空陣列）
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// 使用者擁有的權限代碼清單（聚合所有角色的權限，去重）
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
