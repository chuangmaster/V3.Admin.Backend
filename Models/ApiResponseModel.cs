using System.Text.Json.Serialization;

namespace V3.Admin.Backend.Models;

/// <summary>
/// 標準 API 響應模型，結合 HTTP 狀態碼與業務邏輯回應碼
/// </summary>
/// <typeparam name="T">響應資料類型</typeparam>
public class ApiResponseModel<T>
{
    /// <summary>
    /// 請求是否成功（通常與 HTTP 狀態碼對應）
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 業務邏輯回應碼，用於細分不同的業務場景
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 響應訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 響應資料
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>
    /// 時間戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 追蹤 ID，用於問題排查
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 建立成功響應
    /// </summary>
    public static ApiResponseModel<T> CreateSuccess(T? data = default, string message = "操作成功", string code = "SUCCESS")
    {
        return new ApiResponseModel<T>
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 建立失敗響應
    /// </summary>
    public static ApiResponseModel<T> CreateFailure(string message, string code = "FAILURE", T? data = default)
    {
        return new ApiResponseModel<T>
        {
            Success = false,
            Code = code,
            Message = message,
            Data = data
        };
    }
}

/// <summary>
/// 無資料的 API 響應模型
/// </summary>
public class ApiResponseModel : ApiResponseModel<object>
{
    /// <summary>
    /// 建立成功響應（無資料）
    /// </summary>
    public static ApiResponseModel CreateSuccess(string message = "操作成功", string code = "SUCCESS")
    {
        return new ApiResponseModel
        {
            Success = true,
            Code = code,
            Message = message
        };
    }

    /// <summary>
    /// 建立失敗響應（無資料）
    /// </summary>
    public static ApiResponseModel CreateFailure(string message, string code = "FAILURE")
    {
        return new ApiResponseModel
        {
            Success = false,
            Code = code,
            Message = message
        };
    }
}

/// <summary>
/// 業務邏輯回應碼常數
/// </summary>
public static class ResponseCodes
{
    // 成功相關
    public const string Success = "SUCCESS";
    public const string Created = "CREATED";
    public const string Updated = "UPDATED";
    public const string Deleted = "DELETED";

    // 驗證錯誤
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";

    // 資源相關
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Gone = "GONE";

    // 業務邏輯錯誤
    public const string BusinessError = "BUSINESS_ERROR";
    public const string InsufficientBalance = "INSUFFICIENT_BALANCE";
    public const string ExceededLimit = "EXCEEDED_LIMIT";

    // 系統錯誤
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string Timeout = "TIMEOUT";

    // 外部服務錯誤
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
    public const string DatabaseError = "DATABASE_ERROR";
}