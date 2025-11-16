namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 用戶個人資料回應 DTO
/// </summary>
public class UserProfileResponse
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
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 使用者擁有的權限代碼清單（聚合所有角色的權限，去重）
    /// 若無權限則回傳空陣列
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
