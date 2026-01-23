namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 重設密碼 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層處理管理員重設密碼邏輯
/// </remarks>
public class ResetPasswordDto
{
    /// <summary>
    /// 目標用戶 ID
    /// </summary>
    public Guid TargetUserId { get; set; }

    /// <summary>
    /// 新密碼
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 當前版本號 (樂觀並發控制)
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 操作者 ID
    /// </summary>
    public Guid OperatorId { get; set; }
}
