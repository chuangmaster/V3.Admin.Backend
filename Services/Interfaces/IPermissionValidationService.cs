using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 權限驗證服務介面
/// 負責即時驗證用戶權限
/// </summary>
public interface IPermissionValidationService
{
    /// <summary>
    /// 驗證用戶是否擁有特定權限
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="permissionCode">權限代碼</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否擁有該權限</returns>
    Task<bool> ValidatePermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 查詢用戶的所有有效權限（合併後）
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用戶有效權限 DTO</returns>
    Task<UserEffectivePermissionsDto> GetUserEffectivePermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 記錄權限驗證失敗
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <param name="username">用戶名</param>
    /// <param name="attemptedResource">嘗試訪問的資源</param>
    /// <param name="failureReason">失敗原因</param>
    /// <param name="ipAddress">客戶端 IP</param>
    /// <param name="userAgent">用戶代理</param>
    /// <param name="traceId">追蹤 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否記錄成功</returns>
    Task<bool> LogPermissionFailureAsync(
        Guid userId,
        string username,
        string attemptedResource,
        string failureReason,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default
    );
}
