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
    /// 支援萬用字元模式：例如 "serviceOrder.*.read" 可匹配 "serviceOrder.buyback.read" 或 "serviceOrder.consignment.read"
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

            // 檢查是否包含該權限（支援萬用字元匹配）
            bool hasPermission = effectivePermissions.Permissions.Any(p =>
                MatchesPermissionPattern(p.PermissionCode, permissionCode)
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
    /// 檢查用戶權限是否匹配所需的權限模式
    /// </summary>
    /// <param name="userPermission">用戶擁有的權限代碼</param>
    /// <param name="requiredPattern">所需的權限模式（可包含萬用字元 *）</param>
    /// <returns>是否匹配</returns>
    /// <remarks>
    /// 萬用字元規則：
    /// - "*" 匹配任意單一區段（以 . 分隔）
    /// - 精確匹配優先：若無萬用字元則進行精確比對
    /// - 例如：用戶擁有 "serviceOrder.buyback.read"，可匹配模式 "serviceOrder.*.read"
    /// </remarks>
    private static bool MatchesPermissionPattern(string userPermission, string requiredPattern)
    {
        // 精確匹配
        if (userPermission == requiredPattern)
        {
            return true;
        }

        // 若模式不含萬用字元，則必須精確匹配
        if (!requiredPattern.Contains('*'))
        {
            return false;
        }

        // 分割權限代碼為區段
        var userSegments = userPermission.Split('.');
        var patternSegments = requiredPattern.Split('.');

        // 區段數量必須相同
        if (userSegments.Length != patternSegments.Length)
        {
            return false;
        }

        // 逐一比對每個區段
        for (int i = 0; i < patternSegments.Length; i++)
        {
            var patternSegment = patternSegments[i];
            var userSegment = userSegments[i];

            // 萬用字元匹配任意值
            if (patternSegment == "*")
            {
                continue;
            }

            // 非萬用字元需精確匹配
            if (!string.Equals(patternSegment, userSegment, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
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
    /// 檢查用戶是否擁有指定型別的權限
    /// </summary>
    public async Task<bool> HasPermissionTypeAsync(
        Guid userId,
        string permissionCode,
        string permissionType,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // 首先檢查用戶是否擁有該權限
            bool hasPermission = await ValidatePermissionAsync(userId, permissionCode, cancellationToken);
            if (!hasPermission)
            {
                return false;
            }

            // 檢查權限類型是否匹配
            var permission = await _permissionRepository.GetByCodeAsync(permissionCode);
            if (permission == null || permission.IsDeleted)
            {
                return false;
            }

            // 驗證權限類型
            bool typeMatches = string.Equals(permission.PermissionType, permissionType, StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation(
                "權限類型檢查: UserId={UserId}, PermissionCode={PermissionCode}, ExpectedType={ExpectedType}, ActualType={ActualType}, Matches={Matches}",
                userId,
                permissionCode,
                permissionType,
                permission.PermissionType,
                typeMatches
            );

            return typeMatches;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "權限類型檢查失敗: UserId={UserId}, PermissionCode={PermissionCode}, PermissionType={PermissionType}",
                userId,
                permissionCode,
                permissionType
            );
            throw;
        }
    }

    /// <summary>
    /// 驗證 PermissionCode 格式是否正確
    /// </summary>
    /// <param name="permissionCode">權限代碼</param>
    /// <returns>格式是否正確</returns>
    public bool ValidatePermissionCode(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        // 長度檢查
        if (permissionCode.Length < 3 || permissionCode.Length > 100)
        {
            return false;
        }

        // 格式驗證：允許字母、數字、點號、下劃線
        // 開頭和結尾不能是點號或下劃線
        // 支援單字元或 3-100 字元的格式
        var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$");
        
        return regex.IsMatch(permissionCode);
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
