using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 權限驗證失敗日誌倉儲介面
/// </summary>
public interface IPermissionFailureLogRepository
{
    /// <summary>
    /// 記錄權限驗證失敗
    /// </summary>
    /// <param name="log">失敗日誌實體</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否記錄成功</returns>
    Task<bool> LogFailureAsync(
        PermissionFailureLog log,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 查詢權限驗證失敗日誌（分頁）
    /// </summary>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日誌列表和總數</returns>
    Task<(List<PermissionFailureLog> logs, int totalCount)> GetFailureLogsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 查詢特定用戶的失敗日誌（分頁）
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>日誌列表和總數</returns>
    Task<(List<PermissionFailureLog> logs, int totalCount)> GetUserFailureLogsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default
    );
}
