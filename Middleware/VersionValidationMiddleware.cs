using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Middleware;

/// <summary>
/// JWT Token 版本驗證中介軟體
/// </summary>
/// <remarks>
/// 驗證 JWT 中的 version 與資料庫當前 version 是否一致。
/// 使用分散式快取減少資料庫查詢，提升效能。
/// 任何資料修改都會遞增 version 並清除快取，使舊 token 失效。
/// </remarks>
public class VersionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<VersionValidationMiddleware> _logger;
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5); // 快取 5 分鐘

    /// <summary>
    /// 初始化版本驗證中介軟體
    /// </summary>
    /// <param name="next">下一個中介軟體</param>
    /// <param name="logger">日誌記錄器</param>
    public VersionValidationMiddleware(
        RequestDelegate next,
        ILogger<VersionValidationMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 執行版本驗證邏輯
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="accountService">帳號服務(透過 DI 注入)</param>
    /// <param name="cache">分散式快取(透過 DI 注入)</param>
    public async Task InvokeAsync(
        HttpContext context,
        IAccountService accountService,
        IDistributedCache cache
    )
    {
        // 僅對已驗證的請求進行版本檢查
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("user_id")?.Value;
            var versionClaim = context.User.FindFirst("version")?.Value;

            // 若 JWT 中包含 user_id 和 version claim，則進行版本驗證
            if (
                Guid.TryParse(userIdClaim, out var userId)
                && int.TryParse(versionClaim, out var tokenVersion)
            )
            {
                var currentVersion = await GetUserVersionAsync(userId, accountService, cache);

                // 檢查版本是否一致
                if (currentVersion is null)
                {
                    _logger.LogWarning("版本驗證失敗：使用者不存在 | UserId: {UserId}", userId);

                    await WriteUnauthorizedResponse(context, "使用者不存在，請重新登入");
                    return;
                }

                if (currentVersion.Value != tokenVersion)
                {
                    _logger.LogWarning(
                        "版本驗證失敗：Token 版本不匹配 | UserId: {UserId} | TokenVersion: {TokenVersion} | CurrentVersion: {CurrentVersion}",
                        userId,
                        tokenVersion,
                        currentVersion.Value
                    );

                    await WriteUnauthorizedResponse(context, "Token 已失效，請重新登入");
                    return;
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// 取得使用者版本號(優先從快取讀取)
    /// </summary>
    private async Task<int?> GetUserVersionAsync(
        Guid userId,
        IAccountService accountService,
        IDistributedCache cache
    )
    {
        var cacheKey = $"user_version:{userId}";

        // 1. 嘗試從快取讀取
        var cachedVersion = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedVersion) && int.TryParse(cachedVersion, out var version))
        {
            return version;
        }

        // 2. 快取未命中，從 Service 層讀取
        try
        {
            var account = await accountService.GetAccountByIdAsync(userId);
            var userVersion = account.Version;

            // 3. 寫入快取
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
            };
            await cache.SetStringAsync(cacheKey, userVersion.ToString(), cacheOptions);

            return userVersion;
        }
        catch (KeyNotFoundException)
        {
            // 帳號不存在
            return null;
        }
    }

    /// <summary>
    /// 寫入未授權回應
    /// </summary>
    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new ApiResponseModel<object>
        {
            Code = ResponseCodes.UNAUTHORIZED,
            Message = message,
            Data = null,
            TraceId = context.TraceIdentifier,
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
