using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 稽核日誌數據訪問介面
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// 記錄新的稽核日誌
    /// </summary>
    /// <param name="auditLog">稽核日誌物件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回新建立的稽核日誌</returns>
    Task<AuditLog> LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 ID 查詢稽核日誌
    /// </summary>
    /// <param name="id">稽核日誌 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>稽核日誌物件，如未找到則返回 null</returns>
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢稽核日誌列表（支援分頁和多條件篩選）
    /// </summary>
    /// <param name="startTime">起始時間（可選）</param>
    /// <param name="endTime">結束時間（可選）</param>
    /// <param name="operatorId">操作者 ID（可選）</param>
    /// <param name="operationType">操作類型（可選）</param>
    /// <param name="targetType">目標類型（可選）</param>
    /// <param name="targetId">目標物件 ID（可選）</param>
    /// <param name="pageNumber">頁碼（從 1 開始）</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含稽核日誌列表和總筆數的元組</returns>
    Task<(List<AuditLog> Logs, long TotalCount)> GetLogsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        Guid? operatorId = null,
        string? operationType = null,
        string? targetType = null,
        Guid? targetId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 根據追蹤 ID 查詢稽核日誌
    /// </summary>
    /// <param name="traceId">追蹤 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>該追蹤 ID 相關的所有稽核日誌</returns>
    Task<List<AuditLog>> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default
    );
}
