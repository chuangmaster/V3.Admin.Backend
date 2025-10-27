namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 登入 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層處理登入邏輯
/// </remarks>
public class LoginDto
{
    /// <summary>
    /// 帳號名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密碼
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
