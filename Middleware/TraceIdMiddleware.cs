namespace V3.Admin.Backend.Middleware;

/// <summary>
/// TraceId 中介軟體
/// </summary>
/// <remarks>
/// 自動產生分散式追蹤 ID 並注入到 HttpContext.Items 與回應標頭
/// 用於關聯前後端日誌,協助問題排查
/// </remarks>
public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string TraceIdKey = "TraceId";
    private const string TraceIdHeader = "X-Trace-Id";

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 產生或取得 TraceId
        string traceId =
            context.Request.Headers[TraceIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // 注入到 HttpContext.Items 供後續存取
        context.Items[TraceIdKey] = traceId;

        // 注入到回應標頭
        context.Response.Headers.Append(TraceIdHeader, traceId);

        await _next(context);
    }
}
