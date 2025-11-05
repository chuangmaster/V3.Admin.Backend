using System.Diagnostics;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 權限驗證服務實現
/// 實時查詢權限配置、合併多角色權限、記錄驗證失敗
/// </summary>
public class PermissionValidationService : IPermissionValidationService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IPermissionFailureLogRepository _failureLogRepository;
    private readonly ILogger<PermissionValidationService> _logger;

    /// <summary>
    /// 初始化 PermissionValidationService
    /// </summary>
    public PermissionValidationService(
        IPermissionRepository permissionRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IPermissionFailureLogRepository failureLogRepository,
        ILogger<PermissionValidationService> logger
    )
    {
        _permissionRepository = permissionRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _failureLogRepository = failureLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// 驗證用戶是否擁有特定權限
    /// 性能目標: 小於 100ms
    /// </summary>
    public async Task<bool> ValidatePermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 查詢用戶的所有有效權限
            var effectivePermissions = await GetUserEffectivePermissionsAsync(
                userId,
                cancellationToken
            );

            // 檢查是否包含該權限
            bool hasPermission = effectivePermissions.Permissions.Any(p =>
                p.PermissionCode == permissionCode
            );

            stopwatch.Stop();
            _logger.LogInformation(
                "權限驗證完成: UserId={UserId}, PermissionCode={PermissionCode}, HasPermission={HasPermission}, ElapsedMs={ElapsedMs}",
                userId,
                permissionCode,
                hasPermission,
                stopwatch.ElapsedMilliseconds
            );

            return hasPermission;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "權限驗證失敗: UserId={UserId}, PermissionCode={PermissionCode}, ElapsedMs={ElapsedMs}",
                userId,
                permissionCode,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }

    /// <summary>
    /// 查詢用戶的所有有效權限（多角色合併聯集）
    /// </summary>
    public async Task<UserEffectivePermissionsDto> GetUserEffectivePermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var result = new UserEffectivePermissionsDto { UserId = userId };

        try
        {
            // 查詢用戶的所有角色
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);

            if (userRoles.Count == 0)
            {
                _logger.LogInformation("用戶沒有任何角色: UserId={UserId}", userId);
                return result;
            }

            // 收集所有角色的權限
            var allPermissions = new Dictionary<Guid, PermissionDto>();

            foreach (var userRole in userRoles)
            {
                // 查詢角色的所有權限
                var rolePermissions = await _rolePermissionRepository.GetRolePermissionsAsync(
                    userRole.RoleId,
                    cancellationToken
                );

                foreach (var permission in rolePermissions)
                {
                    // 聯集處理：若已存在相同 ID 的權限，則跳過；否則新增
                    if (!allPermissions.ContainsKey(permission.Id))
                    {
                        // 轉換 Permission 實體到 PermissionDto
                        allPermissions[permission.Id] = new PermissionDto
                        {
                            Id = permission.Id,
                            PermissionCode = permission.PermissionCode,
                            Name = permission.Name,
                            Description = permission.Description,
                            PermissionType = permission.PermissionType,
                            RoutePath = permission.RoutePath,
                            CreatedAt = permission.CreatedAt,
                            Version = permission.Version,
                        };
                    }
                }
            }

            // 轉換為結果列表
            result.Permissions = allPermissions.Values.ToList();

            _logger.LogInformation(
                "用戶有效權限查詢完成: UserId={UserId}, RoleCount={RoleCount}, PermissionCount={PermissionCount}",
                userId,
                userRoles.Count,
                result.Permissions.Count
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢用戶有效權限失敗: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 記錄權限驗證失敗
    /// </summary>
    public async Task<bool> LogPermissionFailureAsync(
        Guid userId,
        string username,
        string attemptedResource,
        string failureReason,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default
    )
    {
        var log = new PermissionFailureLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            AttemptedResource = attemptedResource,
            FailureReason = failureReason,
            AttemptedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TraceId = traceId,
        };

        try
        {
            bool success = await _failureLogRepository.LogFailureAsync(log, cancellationToken);

            if (success)
            {
                _logger.LogWarning(
                    "權限驗證失敗已記錄: UserId={UserId}, Resource={Resource}, Reason={Reason}",
                    userId,
                    attemptedResource,
                    failureReason
                );
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄權限驗證失敗失敗: UserId={UserId}", userId);
            throw;
        }
    }
}
