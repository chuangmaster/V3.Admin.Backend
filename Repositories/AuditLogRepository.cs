using System.Data;
using Dapper;
using Npgsql;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 稽核日誌數據訪問實現
/// </summary>
/// <remarks>
/// 使用 Dapper 微型 ORM 進行資料庫操作
/// 支持複雜篩選、分頁和索引優化
/// 稽核日誌僅支援新增和查詢，不支援修改和刪除
/// </remarks>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnection _dbConnection;

    /// <summary>
    /// 初始化稽核日誌儲存庫
    /// </summary>
    /// <param name="dbConnection">資料庫連接</param>
    public AuditLogRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    /// <summary>
    /// 記錄新的稽核日誌
    /// </summary>
    public async Task<AuditLog> LogAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default
    )
    {
        if (auditLog.Id == Guid.Empty)
        {
            auditLog.Id = Guid.NewGuid();
        }

        const string sql =
            @"
            INSERT INTO audit_logs (
                id, operator_id, operator_name, operation_time, operation_type,
                target_type, target_id, before_state, after_state,
                ip_address, user_agent, trace_id
            )
            VALUES (
                @Id, @OperatorId, @OperatorName, @OperationTime, @OperationType,
                @TargetType, @TargetId, @BeforeState::text::jsonb, @AfterState::text::jsonb,
                @IpAddress, @UserAgent, @TraceId
            )
            RETURNING id, operator_id, operator_name, operation_time, operation_type,
                      target_type, target_id, before_state::text, after_state::text,
                      ip_address, user_agent, trace_id;";

        var result = await _dbConnection.QuerySingleAsync<AuditLog>(sql, auditLog);
        return result;
    }

    /// <summary>
    /// 根據 ID 查詢稽核日誌
    /// </summary>
    public async Task<AuditLog?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            SELECT id, operator_id, operator_name, operation_time, operation_type,
                   target_type, target_id, before_state, after_state,
                   ip_address, user_agent, trace_id
            FROM audit_logs
            WHERE id = @Id;";

        var result = await _dbConnection.QuerySingleOrDefaultAsync<AuditLog?>(sql, new { Id = id });
        return result;
    }

    /// <summary>
    /// 查詢稽核日誌列表（支援分頁和多條件篩選）
    /// </summary>
    public async Task<(List<AuditLog> Logs, long TotalCount)> GetLogsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        Guid? operatorId = null,
        string? operationType = null,
        string? targetType = null,
        Guid? targetId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        // 建構動態 WHERE 條件
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        if (startTime.HasValue)
        {
            whereConditions.Add("operation_time >= @StartTime");
            parameters.Add("@StartTime", startTime.Value);
        }

        if (endTime.HasValue)
        {
            whereConditions.Add("operation_time <= @EndTime");
            parameters.Add("@EndTime", endTime.Value);
        }

        if (operatorId.HasValue && operatorId.Value != Guid.Empty)
        {
            whereConditions.Add("operator_id = @OperatorId");
            parameters.Add("@OperatorId", operatorId.Value);
        }

        if (!string.IsNullOrEmpty(operationType))
        {
            whereConditions.Add("operation_type = @OperationType");
            parameters.Add("@OperationType", operationType);
        }

        if (!string.IsNullOrEmpty(targetType))
        {
            whereConditions.Add("target_type = @TargetType");
            parameters.Add("@TargetType", targetType);
        }

        if (targetId.HasValue && targetId.Value != Guid.Empty)
        {
            whereConditions.Add("target_id = @TargetId");
            parameters.Add("@TargetId", targetId.Value);
        }

        var whereClause =
            whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // 計算偏移量
        var offset = (pageNumber - 1) * pageSize;
        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", pageSize);

        // 查詢總筆數
        var countSql = $@"SELECT COUNT(*) FROM audit_logs {whereClause};";

        // 查詢分頁資料
        var dataSql =
            $@"
            SELECT id, operator_id, operator_name, operation_time, operation_type,
                   target_type, target_id, before_state, after_state,
                   ip_address, user_agent, trace_id
            FROM audit_logs
            {whereClause}
            ORDER BY operation_time DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var totalCount = await _dbConnection.ExecuteScalarAsync<long>(countSql, parameters);
        var logs = (await _dbConnection.QueryAsync<AuditLog>(dataSql, parameters)).ToList();

        return (logs, totalCount);
    }

    /// <summary>
    /// 根據追蹤 ID 查詢稽核日誌
    /// </summary>
    public async Task<List<AuditLog>> GetByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            SELECT id, operator_id, operator_name, operation_time, operation_type,
                   target_type, target_id, before_state, after_state,
                   ip_address, user_agent, trace_id
            FROM audit_logs
            WHERE trace_id = @TraceId
            ORDER BY operation_time DESC;";

        var results = (
            await _dbConnection.QueryAsync<AuditLog>(sql, new { TraceId = traceId })
        ).ToList();
        return results;
    }
}
