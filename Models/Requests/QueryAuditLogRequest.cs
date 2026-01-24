namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 查詢稽核日誌請求
/// </summary>
public class QueryAuditLogRequest
{
    /// <summary>
    /// 起始時間（UTC0 格式）
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 結束時間（UTC0 格式）
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 操作者 ID（可選篩選）
    /// </summary>
    public Guid? OperatorId { get; set; }

    /// <summary>
    /// 操作類型（可選篩選：create, update, delete）
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// 目標類型（可選篩選：permission, role, user_role, role_permission）
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// 目標物件 ID（可選篩選）
    /// </summary>
    public Guid? TargetId { get; set; }

    /// <summary>
    /// 分頁頁碼（從 1 開始）
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 每頁筆數（最大 100）
    /// </summary>
    public int PageSize { get; set; } = 20;
}
