using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 稽核日誌業務邏輯介面
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// 記錄操作稽核日誌
    /// </summary>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="operatorName">操作者名稱</param>
    /// <param name="operationType">操作類型（create, update, delete）</param>
    /// <param name="targetType">目標類型（permission, role, user_role, role_permission）</param>
    /// <param name="targetId">目標物件 ID</param>
    /// <param name="beforeState">操作前的狀態（JSON 序列化）</param>
    /// <param name="afterState">操作後的狀態（JSON 序列化）</param>
    /// <param name="traceId">追蹤 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>記錄的稽核日誌</returns>
    Task<AuditLog> LogOperationAsync(
        Guid? operatorId,
        string operatorName,
        string operationType,
        string targetType,
        Guid? targetId,
        string? beforeState = null,
        string? afterState = null,
        string? traceId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 根據 ID 查詢稽核日誌
    /// </summary>
    /// <param name="id">稽核日誌 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>稽核日誌 DTO</returns>
    Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢稽核日誌列表
    /// </summary>
    /// <param name="request">查詢請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>稽核日誌列表響應</returns>
    Task<AuditLogListResponse> GetAuditLogsAsync(
        QueryAuditLogRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 根據追蹤 ID 查詢稽核日誌
    /// </summary>
    /// <param name="traceId">追蹤 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>稽核日誌 DTO 列表</returns>
    Task<List<AuditLogDto>> GetAuditLogsByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default
    );
}
