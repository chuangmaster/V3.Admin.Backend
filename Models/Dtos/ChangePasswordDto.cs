namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 變更密碼 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層處理變更密碼邏輯
/// </remarks>
public class ChangePasswordDto
{
    /// <summary>
    /// 帳號 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 舊密碼
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密碼
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 當前版本號 (樂觀並發控制)
    /// </summary>
    public int Version { get; set; }
}
