using System.Text.Json;
using Microsoft.Extensions.Logging;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 稽核日誌業務邏輯實現
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLogService> _logger;

    /// <summary>
    /// 初始化稽核日誌服務
    /// </summary>
    /// <param name="auditLogRepository">稽核日誌儲存庫</param>
    /// <param name="logger">日誌記錄器</param>
    public AuditLogService(IAuditLogRepository auditLogRepository, ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// 記錄操作稽核日誌
    /// </summary>
    public async Task<AuditLog> LogOperationAsync(
        Guid? operatorId,
        string operatorName,
        string operationType,
        string targetType,
        Guid? targetId,
        string? beforeState = null,
        string? afterState = null,
        string? traceId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                OperatorId = operatorId,
                OperatorName = operatorName,
                OperationTime = DateTimeOffset.UtcNow.DateTime,
                OperationType = operationType,
                TargetType = targetType,
                TargetId = targetId,
                BeforeState = beforeState,
                AfterState = afterState,
                TraceId = traceId,
            };

            var result = await _auditLogRepository.LogAsync(auditLog, cancellationToken);
            _logger.LogInformation(
                "稽核日誌已記錄: OperationType={OperationType}, TargetType={TargetType}, TargetId={TargetId}, OperatorName={OperatorName}",
                operationType,
                targetType,
                targetId,
                operatorName
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄稽核日誌時發生錯誤: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 根據 ID 查詢稽核日誌
    /// </summary>
    public async Task<AuditLogDto?> GetAuditLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var auditLog = await _auditLogRepository.GetByIdAsync(id, cancellationToken);
            if (auditLog == null)
            {
                return null;
            }

            return MapToDto(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢稽核日誌時發生錯誤: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 查詢稽核日誌列表
    /// </summary>
    public async Task<AuditLogListResponse> GetAuditLogsAsync(
        QueryAuditLogRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var (logs, totalCount) = await _auditLogRepository.GetLogsAsync(
                request.StartTime,
                request.EndTime,
                request.OperatorId,
                request.OperationType,
                request.TargetType,
                request.TargetId,
                request.PageNumber,
                request.PageSize,
                cancellationToken
            );

            var dtos = logs.Select(MapToDto).ToList();

            var response = new AuditLogListResponse
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢稽核日誌列表時發生錯誤: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 根據追蹤 ID 查詢稽核日誌
    /// </summary>
    public async Task<List<AuditLogDto>> GetAuditLogsByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var auditLogs = await _auditLogRepository.GetByTraceIdAsync(traceId, cancellationToken);
            return auditLogs.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根據追蹤 ID 查詢稽核日誌時發生錯誤: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 將稽核日誌實體映射為 DTO
    /// </summary>
    private static AuditLogDto MapToDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            OperatorId = auditLog.OperatorId,
            OperatorName = auditLog.OperatorName,
            OperationTime = auditLog.OperationTime,
            OperationType = auditLog.OperationType,
            TargetType = auditLog.TargetType,
            TargetId = auditLog.TargetId,
            BeforeState = auditLog.BeforeState,
            AfterState = auditLog.AfterState,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            TraceId = auditLog.TraceId,
            AdditionalInfo = auditLog.AdditionalInfo,
        };
    }
}
