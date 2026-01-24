using System.Net;
using System.Text.Json;
using FluentValidation;
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

        ApiResponseModel response;
        HttpStatusCode statusCode;

        // 根據異常類型處理
        switch (exception)
        {
            // FluentValidation 驗證錯誤
            case ValidationException validationEx:
                _logger.LogWarning(
                    "驗證失敗 | TraceId: {TraceId} | Path: {Path} | Errors: {Errors}",
                    traceId,
                    context.Request.Path,
                    string.Join("; ", validationEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))
                );

                response = ApiResponseModel.CreateFailure(
                    "輸入資料驗證失敗",
                    ResponseCodes.VALIDATION_ERROR
                );
                response.TraceId = traceId;
                response.Errors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToList() as object
                    );

                statusCode = HttpStatusCode.BadRequest;
                break;

            // JSON 反序列化錯誤 (包含 UTC0 時間格式錯誤)
            case JsonException jsonEx:
                _logger.LogWarning(
                    jsonEx,
                    "JSON 解析失敗 | TraceId: {TraceId} | Path: {Path}",
                    traceId,
                    context.Request.Path
                );

                response = ApiResponseModel.CreateFailure(
                    "請求資料格式錯誤",
                    ResponseCodes.VALIDATION_ERROR
                );
                response.TraceId = traceId;
                response.Errors = new Dictionary<string, object>
                {
                    ["detail"] = jsonEx.Message
                };

                statusCode = HttpStatusCode.BadRequest;
                break;

            // 其他未處理的例外
            default:
                _logger.LogError(
                    exception,
                    "未處理的例外發生 | TraceId: {TraceId} | Path: {Path} | Method: {Method}",
                    traceId,
                    context.Request.Path,
                    context.Request.Method
                );

                response = ApiResponseModel.CreateFailure(
                    "系統內部錯誤,請稍後再試",
                    ResponseCodes.INTERNAL_ERROR
                );
                response.TraceId = traceId;

                statusCode = HttpStatusCode.InternalServerError;
                break;
        }

        // 設定回應
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // 序列化並寫入回應
        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        await context.Response.WriteAsync(json);
    }
}
