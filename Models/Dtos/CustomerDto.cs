namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 客戶 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層傳遞客戶基本資訊
/// </remarks>
public class CustomerDto
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
    /// 版本號 (樂觀並發控制)
    /// </summary>
    public int Version { get; set; }
}
