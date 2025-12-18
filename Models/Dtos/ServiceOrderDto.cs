namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 服務單 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層傳遞服務單核心資訊,可代表收購單或寄賣單
/// </remarks>
public class ServiceOrderDto
{
    /// <summary>
    /// 服務單 ID
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
    /// 版本號 (樂觀並發控制)
    /// </summary>
    public int Version { get; set; }
}
