namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 稽核日誌 DTO
/// </summary>
/// <remarks>
/// 用於 API 回應，包含稽核日誌的所有公開欄位
/// </remarks>
public class AuditLogDto
{
    /// <summary>
    /// 稽核日誌唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 操作者 ID
    /// </summary>
    public Guid? OperatorId { get; set; }

    /// <summary>
    /// 操作者名稱
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 操作發生的時間 (UTC)
    /// </summary>
    public DateTimeOffset OperationTime { get; set; }

    /// <summary>
    /// 操作類型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 目標類型
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 目標物件 ID
    /// </summary>
    public Guid? TargetId { get; set; }

    /// <summary>
    /// 操作前的狀態（JSON）
    /// </summary>
    public string? BeforeState { get; set; }

    /// <summary>
    /// 操作後的狀態（JSON）
    /// </summary>
    public string? AfterState { get; set; }

    /// <summary>
    /// 請求的來源 IP 位址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用戶代理字串
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 分散式追蹤 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 額外資訊（JSON）
    /// </summary>
    public string? AdditionalInfo { get; set; }
}
