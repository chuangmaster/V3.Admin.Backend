namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 新增帳號請求模型
/// </summary>
/// <remarks>
/// 用於接收客戶端的新增帳號請求
/// </remarks>
public class CreateAccountRequest
{
    /// <summary>
    /// 帳號名稱
    /// </summary>
    /// <remarks>
    /// 長度限制: 3-20 字元,僅允許英數字與底線
    /// </remarks>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密碼
    /// </summary>
    /// <remarks>
    /// 最少 8 字元,支援所有 Unicode 字元
    /// </remarks>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    /// <remarks>
    /// 最大長度 100 字元
    /// </remarks>
    public string DisplayName { get; set; } = string.Empty;
}
