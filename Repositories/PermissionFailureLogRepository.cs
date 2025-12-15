using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 權限驗證失敗日誌倉儲實現
/// </summary>
public class PermissionFailureLogRepository : IPermissionFailureLogRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<PermissionFailureLogRepository> _logger;

    /// <summary>
    /// 初始化 PermissionFailureLogRepository
    /// </summary>
    public PermissionFailureLogRepository(
        IDbConnection dbConnection,
        ILogger<PermissionFailureLogRepository> logger
    )
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    /// <summary>
    /// 記錄權限驗證失敗
    /// </summary>
    public async Task<bool> LogFailureAsync(
        PermissionFailureLog log,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            INSERT INTO permission_failure_logs 
            (user_id, username, attempted_resource, failure_reason, attempted_at, ip_address, user_agent, trace_id)
            VALUES (@UserId, @Username, @AttemptedResource, @FailureReason, @AttemptedAt, @IpAddress, @UserAgent, @TraceId);
        ";

        try
        {
            // 使用自動產生的 UUID，不需要手動指定 Id
            var rowsAffected = await _dbConnection.ExecuteAsync(
                sql,
                new
                {
                    log.UserId,
                    log.Username,
                    log.AttemptedResource,
                    log.FailureReason,
                    log.AttemptedAt,
                    log.IpAddress,
                    log.UserAgent,
                    log.TraceId,
                }
            );

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄權限失敗日誌失敗: UserId={UserId}", log.UserId);
            throw;
        }
    }

    /// <summary>
    /// 查詢權限驗證失敗日誌（分頁）
    /// </summary>
    public async Task<(List<PermissionFailureLog> logs, int totalCount)> GetFailureLogsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        const string countSql = "SELECT COUNT(*) FROM permission_failure_logs;";
        const string querySql =
            @"
            SELECT id, user_id, username, attempted_resource, failure_reason, attempted_at, 
                   ip_address, user_agent, trace_id
            FROM permission_failure_logs
            ORDER BY attempted_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        ";

        try
        {
            int totalCount = await _dbConnection.QuerySingleAsync<int>(
                countSql,
                commandTimeout: null
            );
            int offset = (pageNumber - 1) * pageSize;

            var logs = (
                await _dbConnection.QueryAsync<PermissionFailureLog>(
                    querySql,
                    new { Offset = offset, PageSize = pageSize }
                )
            ).ToList();

            return (logs, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢權限失敗日誌失敗");
            throw;
        }
    }

    /// <summary>
    /// 查詢特定用戶的失敗日誌（分頁）
    /// </summary>
    public async Task<(List<PermissionFailureLog> logs, int totalCount)> GetUserFailureLogsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        const string countSql =
            "SELECT COUNT(*) FROM permission_failure_logs WHERE user_id = @UserId;";
        const string querySql =
            @"
            SELECT id, user_id, username, attempted_resource, failure_reason, attempted_at, 
                   ip_address, user_agent, trace_id
            FROM permission_failure_logs
            WHERE user_id = @UserId
            ORDER BY attempted_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        ";

        try
        {
            int totalCount = await _dbConnection.QuerySingleAsync<int>(
                countSql,
                new { UserId = userId },
                commandTimeout: null
            );
            int offset = (pageNumber - 1) * pageSize;

            var logs = (
                await _dbConnection.QueryAsync<PermissionFailureLog>(
                    querySql,
                    new
                    {
                        UserId = userId,
                        Offset = offset,
                        PageSize = pageSize,
                    }
                )
            ).ToList();

            return (logs, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢用戶權限失敗日誌失敗: UserId={UserId}", userId);
            throw;
        }
    }
}
