namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 稽核日誌實體
/// </summary>
/// <remarks>
/// 對應資料庫 audit_logs 資料表，記錄系統中所有權限管理相關的操作
/// 包括權限、角色、用戶角色的新增、修改、刪除操作
/// 稽核日誌僅可新增和查詢，不可修改或刪除，用於合規性追蹤
/// </remarks>
public class AuditLog
{
    /// <summary>
    /// 稽核日誌唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 操作者 ID（執行操作的使用者）
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
    /// 操作類型（create, update, delete）
    /// </summary>
    /// <remarks>
    /// 取值：
    /// - create: 新增操作
    /// - update: 修改操作
    /// - delete: 刪除操作
    /// </remarks>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 目標類型（permission, role, user_role）
    /// </summary>
    /// <remarks>
    /// 取值：
    /// - permission: 權限管理
    /// - role: 角色管理
    /// - role_permission: 角色權限分配
    /// - user_role: 用戶角色指派
    /// </remarks>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 目標物件 ID（被操作的物件 ID）
    /// </summary>
    public Guid? TargetId { get; set; }

    /// <summary>
    /// 操作前的狀態（JSON 格式）
    /// </summary>
    /// <remarks>
    /// 儲存為 JSONB，包含操作前物件的所有相關欄位
    /// 用於審計追蹤和回滾驗證
    /// </remarks>
    public string? BeforeState { get; set; }

    /// <summary>
    /// 操作後的狀態（JSON 格式）
    /// </summary>
    /// <remarks>
    /// 儲存為 JSONB，包含操作後物件的所有相關欄位
    /// </remarks>
    public string? AfterState { get; set; }

    /// <summary>
    /// 請求的來源 IP 位址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用戶代理字串（User-Agent）
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 分散式追蹤 ID
    /// </summary>
    /// <remarks>
    /// 用於關聯同一請求中的多條操作日誌
    /// </remarks>
    public string? TraceId { get; set; }

    /// <summary>
    /// 額外資訊（JSON 格式，可選）
    /// </summary>
    /// <remarks>
    /// 用於儲存操作相關的其他上下文資訊
    /// </remarks>
    public string? AdditionalInfo { get; set; }
}
