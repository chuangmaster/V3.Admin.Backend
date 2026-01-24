namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 登入結果 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// Service 層返回給 Controller 層的登入結果
/// </remarks>
public class LoginResultDto
{
    /// <summary>
    /// JWT Access Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token 過期時間 (UTC)
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 使用者資訊
    /// </summary>
    public AccountDto User { get; set; } = new();
}
