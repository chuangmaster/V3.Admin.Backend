using System.Security.Claims;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Middleware;

/// <summary>
/// 權限授權中介軟體
/// 自動驗證 [RequirePermission] 屬性並檢查用戶權限
/// </summary>
public class PermissionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionAuthorizationMiddleware> _logger;

    /// <summary>
    /// 初始化中介軟體
    /// </summary>
    public PermissionAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<PermissionAuthorizationMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 中介軟體呼叫方法
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        IPermissionValidationService permissionValidationService
    )
    {
        try
        {
            // 檢查端點是否需要權限驗證
            var endpoint = context.GetEndpoint();
            var requirePermission = endpoint
                ?.Metadata.GetOrderedMetadata<RequirePermissionAttribute>()
                .FirstOrDefault();

            if (requirePermission != null)
            {
                // 取得當前用戶
                var userIdClaim = context.User.FindFirst("sub")?.Value;
                var usernameClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (
                    string.IsNullOrEmpty(userIdClaim)
                    || !Guid.TryParse(userIdClaim, out Guid userId)
                )
                {
                    _logger.LogWarning(
                        "未授權的訪問嘗試（無效的用戶 ID）| TraceId: {TraceId}",
                        context.TraceIdentifier
                    );
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                // 驗證權限
                bool hasPermission = await permissionValidationService.ValidatePermissionAsync(
                    userId,
                    requirePermission.PermissionCode,
                    context.RequestAborted
                );

                if (!hasPermission)
                {
                    // 記錄失敗嘗試
                    string? ipAddress = context.Connection.RemoteIpAddress?.ToString();
                    string? userAgent = context.Request.Headers.UserAgent.ToString();
                    string resource = $"{context.Request.Method} {context.Request.Path}";

                    _ = permissionValidationService.LogPermissionFailureAsync(
                        userId,
                        usernameClaim ?? "Unknown",
                        resource,
                        $"缺少所需權限: {requirePermission.PermissionCode}",
                        ipAddress,
                        userAgent,
                        context.TraceIdentifier
                    );

                    _logger.LogWarning(
                        "權限驗證失敗: UserId={UserId}, Permission={Permission}, Resource={Resource} | TraceId: {TraceId}",
                        userId,
                        requirePermission.PermissionCode,
                        resource,
                        context.TraceIdentifier
                    );

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                _logger.LogInformation(
                    "權限驗證成功: UserId={UserId}, Permission={Permission} | TraceId: {TraceId}",
                    userId,
                    requirePermission.PermissionCode,
                    context.TraceIdentifier
                );
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "權限驗證中介軟體異常 | TraceId: {TraceId}",
                context.TraceIdentifier
            );
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}

/// <summary>
/// 權限要求屬性
/// 標記控制器或方法需要的權限
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute : Attribute
{
    /// <summary>
    /// 所需的權限代碼
    /// </summary>
    public string PermissionCode { get; }

    /// <summary>
    /// 初始化屬性
    /// </summary>
    public RequirePermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}
