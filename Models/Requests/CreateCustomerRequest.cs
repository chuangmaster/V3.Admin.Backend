namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 新增客戶請求模型
/// </summary>
/// <remarks>
/// 用於接收客戶端新增客戶資料的請求
/// </remarks>
public class CreateCustomerRequest
{
    /// <summary>
    /// 客戶姓名
    /// </summary>
    /// <remarks>
    /// 最大長度 100 字元
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 聯絡電話
    /// </summary>
    /// <remarks>
    /// 建議格式: 09XXXXXXXX 或 09XX-XXXXXX
    /// </remarks>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 電子郵件
    /// </summary>
    /// <remarks>
    /// 選填,若填寫則必須符合 Email 格式
    /// </remarks>
    public string? Email { get; set; }

    /// <summary>
    /// 身分證字號/外籍人士格式
    /// </summary>
    /// <remarks>
    /// 台灣人士: 1 英文字母 + 9 數字 (例: A123456789)
    /// 外籍人士: 西元出生年月日(8位) + 英文姓名首字前2碼(大寫) (例: 19900115JO)
    /// </remarks>
    public string IdNumber { get; set; } = string.Empty;
}
