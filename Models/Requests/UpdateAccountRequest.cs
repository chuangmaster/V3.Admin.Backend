namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 更新帳號請求模型
/// </summary>
/// <remarks>
/// 用於接收客戶端的更新帳號請求
/// </remarks>
public class UpdateAccountRequest
{
    /// <summary>
    /// 顯示名稱
    /// </summary>
    /// <remarks>
    /// 最大長度 100 字元
    /// </remarks>
    public string DisplayName { get; set; } = string.Empty;
}
