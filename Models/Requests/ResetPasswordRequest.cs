using System.ComponentModel.DataAnnotations;

namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 管理員重設密碼請求模型
/// </summary>
/// <remarks>
/// 管理員無需提供舊密碼即可重設用戶密碼
/// </remarks>
public class ResetPasswordRequest
{
    /// <summary>
    /// 新密碼
    /// </summary>
    /// <remarks>
    /// 必須符合密碼強度要求:
    /// - 長度 8-100 字元
    /// - 至少一個大寫字母
    /// - 至少一個小寫字母
    /// - 至少一個數字
    /// </remarks>
    [Required(ErrorMessage = "新密碼為必填")]
    [MinLength(8, ErrorMessage = "密碼長度至少 8 個字元")]
    [MaxLength(100, ErrorMessage = "密碼長度最多 100 個字元")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 併發控制版本號
    /// </summary>
    /// <remarks>
    /// 用於樂觀並發控制,必須與目標用戶當前 version 一致
    /// </remarks>
    [Required(ErrorMessage = "版本號為必填")]
    public int Version { get; set; }
}
