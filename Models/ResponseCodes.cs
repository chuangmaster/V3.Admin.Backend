namespace V3.Admin.Backend.Models;

/// <summary>
/// 業務邏輯回應代碼常數
/// </summary>
/// <remarks>
/// 定義所有 API 回應的業務邏輯代碼,用於前端精確處理不同的業務情況
/// 與 HTTP 狀態碼配合使用,提供雙層回應設計
/// </remarks>
public static class ResponseCodes
{
    // ===== 成功狀態 (2xx) =====

    /// <summary>
    /// 操作成功
    /// </summary>
    public const string SUCCESS = "SUCCESS";

    /// <summary>
    /// 資源建立成功
    /// </summary>
    public const string CREATED = "CREATED";

    // ===== 客戶端錯誤 (4xx) =====

    /// <summary>
    /// 輸入驗證錯誤
    /// </summary>
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";

    /// <summary>
    /// 登入憑證錯誤 (帳號或密碼錯誤)
    /// </summary>
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";

    /// <summary>
    /// 未授權 (缺少或無效的 JWT Token)
    /// </summary>
    public const string UNAUTHORIZED = "UNAUTHORIZED";

    /// <summary>
    /// 禁止操作 (權限不足)
    /// </summary>
    public const string FORBIDDEN = "FORBIDDEN";

    /// <summary>
    /// 資源不存在
    /// </summary>
    public const string NOT_FOUND = "NOT_FOUND";

    // ===== 業務邏輯錯誤 (422) =====

    /// <summary>
    /// 帳號已存在 (新增帳號時重複)
    /// </summary>
    public const string USERNAME_EXISTS = "USERNAME_EXISTS";

    /// <summary>
    /// 新密碼與舊密碼相同
    /// </summary>
    public const string PASSWORD_SAME_AS_OLD = "PASSWORD_SAME_AS_OLD";

    /// <summary>
    /// 無法刪除當前登入的帳號
    /// </summary>
    public const string CANNOT_DELETE_SELF = "CANNOT_DELETE_SELF";

    /// <summary>
    /// 無法刪除最後一個有效帳號
    /// </summary>
    public const string LAST_ACCOUNT_CANNOT_DELETE = "LAST_ACCOUNT_CANNOT_DELETE";

    // ===== 權限管理相關錯誤 (404/422) =====

    /// <summary>
    /// 權限不存在
    /// </summary>
    public const string PERMISSION_NOT_FOUND = "PERMISSION_NOT_FOUND";

    /// <summary>
    /// 角色不存在
    /// </summary>
    public const string ROLE_NOT_FOUND = "ROLE_NOT_FOUND";

    /// <summary>
    /// 用戶不存在
    /// </summary>
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";

    /// <summary>
    /// 稽核日誌不存在
    /// </summary>
    public const string AUDIT_LOG_NOT_FOUND = "AUDIT_LOG_NOT_FOUND";

    /// <summary>
    /// 權限正被角色使用，無法刪除
    /// </summary>
    public const string PERMISSION_IN_USE = "PERMISSION_IN_USE";

    /// <summary>
    /// 角色正被用戶使用，無法刪除
    /// </summary>
    public const string ROLE_IN_USE = "ROLE_IN_USE";

    /// <summary>
    /// 權限代碼已存在
    /// </summary>
    public const string DUPLICATE_PERMISSION_CODE = "DUPLICATE_PERMISSION_CODE";

    /// <summary>
    /// 角色名稱已存在
    /// </summary>
    public const string DUPLICATE_ROLE_NAME = "DUPLICATE_ROLE_NAME";

    // ===== 並發控制 (409) =====

    /// <summary>
    /// 並發更新衝突 (資料已被其他使用者修改)
    /// </summary>
    public const string CONCURRENT_UPDATE_CONFLICT = "CONCURRENT_UPDATE_CONFLICT";

    // ===== 伺服器錯誤 (5xx) =====

    /// <summary>
    /// 系統內部錯誤
    /// </summary>
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
}
