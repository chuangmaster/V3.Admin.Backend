namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 查詢客戶請求模型
/// </summary>
/// <remarks>
/// 用於服務單建立流程中搜尋既有客戶
/// </remarks>
public class SearchCustomerRequest
{
    /// <summary>
    /// 客戶姓名 (模糊搜尋)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 聯絡電話 (精確搜尋)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 電子郵件 (模糊搜尋)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 身分證字號/外籍人士格式 (精確搜尋)
    /// </summary>
    public string? IdNumber { get; set; }
}
