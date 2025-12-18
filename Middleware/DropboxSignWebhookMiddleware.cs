using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using V3.Admin.Backend.Configuration;

namespace V3.Admin.Backend.Middleware;

/// <summary>
/// Dropbox Sign Webhook 驗證中介軟體
/// </summary>
/// <remarks>
/// - 針對特定路徑啟用 Request Body Buffering,讓後續 Controller 可再次讀取 body
/// - 若有設定 WebhookSecret,將嘗試驗證簽章(依 Dropbox Sign 常見模式: HMACSHA256)
/// - 若未設定 WebhookSecret,則僅記錄警告並放行(方便開發環境先串起流程)
/// </remarks>
public class DropboxSignWebhookMiddleware
{
    private const string _defaultPath = "/webhooks/dropbox-sign";

    private readonly RequestDelegate _next;
    private readonly DropboxSignSettings _settings;
    private readonly ILogger<DropboxSignWebhookMiddleware> _logger;
    private readonly IMemoryCache _memoryCache;

    public DropboxSignWebhookMiddleware(
        RequestDelegate next,
        IOptions<DropboxSignSettings> options,
        IMemoryCache memoryCache,
        ILogger<DropboxSignWebhookMiddleware> logger
    )
    {
        _next = next;
        _settings = options.Value;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.Equals(_defaultPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        string body;
        using (var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true
        ))
        {
            body = await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            _logger.LogWarning(
                "Dropbox Sign WebhookSecret 未設定，略過簽章驗證 | TraceId: {TraceId}",
                context.TraceIdentifier
            );
            context.Items["DropboxSignWebhookBody"] = body;
            await _next(context);
            return;
        }

        // 常見 header 名稱(依供應商實際文件調整)
        // 這裡先採用最常見的變體，後續可在正式串接時依文件確認
        string signatureHeader =
            context.Request.Headers["X-Dropbox-Signature"].ToString();

        string timestampHeader =
            context.Request.Headers["X-Dropbox-Request-Timestamp"].ToString();

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing webhook signature", context.RequestAborted);
            return;
        }

        if (string.IsNullOrWhiteSpace(timestampHeader) || !long.TryParse(timestampHeader, out long unixSeconds))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid webhook timestamp", context.RequestAborted);
            return;
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var now = DateTimeOffset.UtcNow;
        if ((now - requestTime).Duration() > TimeSpan.FromMinutes(5))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Webhook timestamp out of range", context.RequestAborted);
            return;
        }

        // 以 HMACSHA256(secret, body) 比對 hex
        string expectedSignature = ComputeHmacSha256Hex(_settings.WebhookSecret, body);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signatureHeader)
        ))
        {
            _logger.LogWarning(
                "Dropbox Sign Webhook 簽章驗證失敗 | TraceId: {TraceId}",
                context.TraceIdentifier
            );
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid webhook signature", context.RequestAborted);
            return;
        }

        // Event Hash 防重複(以 body 的 SHA256 當作事件指紋)
        string eventHash = ComputeSha256Hex(body);
        string dedupeKey = $"dropboxsign:webhook:{eventHash}";

        if (_memoryCache.TryGetValue(dedupeKey, out _))
        {
            _logger.LogInformation(
                "Dropbox Sign Webhook 重複事件，略過處理 | EventHash: {EventHash} | TraceId: {TraceId}",
                eventHash,
                context.TraceIdentifier
            );

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("Duplicate event", context.RequestAborted);
            return;
        }

        _memoryCache.Set(dedupeKey, true, TimeSpan.FromHours(1));

        context.Items["DropboxSignWebhookBody"] = body;
        await _next(context);
    }

    private static string ComputeHmacSha256Hex(string secret, string payload)
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        byte[] hash = hmac.ComputeHash(payloadBytes);

        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    private static string ComputeSha256Hex(string payload)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        byte[] hash = SHA256.HashData(bytes);

        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
