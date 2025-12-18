namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 服務單實體
/// </summary>
/// <remarks>
/// 對應資料庫 service_orders 資料表,代表收購單或寄賣單,包含序號/狀態/軟刪除與樂觀並發控制欄位
/// </remarks>
public class ServiceOrder
{
    /// <summary>
    /// 服務單唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 服務日期
    /// </summary>
    public DateTime ServiceDate { get; set; }

    /// <summary>
    /// 當日序號 (1-999)
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// 服務單編號 (BS/CS + YYYYMMDD + 001)
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// 服務單類型 (BUYBACK/CONSIGNMENT)
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// 服務單來源 (OFFLINE/ONLINE)
    /// </summary>
    public string OrderSource { get; set; } = string.Empty;

    /// <summary>
    /// 客戶 ID
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 總金額
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 服務單狀態 (PENDING/COMPLETED/TERMINATED)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 寄賣起始日期 (寄賣單專屬)
    /// </summary>
    public DateTime? ConsignmentStartDate { get; set; }

    /// <summary>
    /// 寄賣結束日期 (寄賣單專屬)
    /// </summary>
    public DateTime? ConsignmentEndDate { get; set; }

    /// <summary>
    /// 續約設定 (寄賣單專屬)
    /// </summary>
    public string? RenewalOption { get; set; }

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
