namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 新增帳號 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層處理新增帳號邏輯
/// </remarks>
public class CreateAccountDto
{
    /// <summary>
    /// 帳號名稱
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 密碼
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
