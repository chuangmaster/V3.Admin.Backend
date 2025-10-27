using System.Net;
using System.Text.Json;
using V3.Admin.Backend.Models;

namespace V3.Admin.Backend.Middleware;

/// <summary>
/// 全域異常處理中介軟體
/// </summary>
/// <remarks>
/// 捕捉所有未處理的例外並包裝為 ApiResponseModel 回應
/// 確保所有錯誤回應格式一致,並記錄詳細的錯誤日誌
/// </remarks>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 取得 TraceId
        string? traceId = context.Items["TraceId"]?.ToString();

        // 記錄錯誤日誌
        _logger.LogError(
            exception,
            "未處理的例外發生 | TraceId: {TraceId} | Path: {Path} | Method: {Method}",
            traceId,
            context.Request.Path,
            context.Request.Method
        );

        // 建立錯誤回應
        var response = ApiResponseModel.CreateFailure(
            "系統內部錯誤,請稍後再試",
            ResponseCodes.INTERNAL_ERROR
        );
        response.TraceId = traceId;

        // 設定回應
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // 序列化並寫入回應
        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        await context.Response.WriteAsync(json);
    }
}
