namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 客戶實體
/// </summary>
/// <remarks>
/// 對應資料庫 customers 資料表,包含客戶基本資訊、軟刪除與樂觀並發控制欄位
/// </remarks>
public class Customer
{
    /// <summary>
    /// 客戶唯一識別碼
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
    /// 建立者 ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 最後更新者 ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// 是否已刪除 (軟刪除標記)
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 刪除時間 (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 刪除操作者 ID
    /// </summary>
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// 版本號 (樂觀並發控制)
    /// </summary>
    public int Version { get; set; }
}
