namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 客戶資訊回應模型
/// </summary>
/// <remarks>
/// 用於回傳客戶基本資訊給客戶端
/// </remarks>
public class CustomerResponse
{
    /// <summary>
    /// 客戶 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 客戶姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 聯絡電話
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 電子郵件
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 身分證字號/外籍人士格式
    /// </summary>
    public string IdNumber { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 資料版本號
    /// </summary>
    public int Version { get; set; }
}
