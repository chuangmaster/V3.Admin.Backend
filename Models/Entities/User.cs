namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 使用者實體
/// </summary>
/// <remarks>
/// 對應資料庫 users 資料表,包含帳號基本資訊、軟刪除與樂觀並發控制欄位
/// </remarks>
public class User
{
    /// <summary>
    /// 使用者唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 帳號名稱 (唯一,用於登入)
    /// </summary>
    /// <remarks>
    /// 長度限制: 3-20 字元,僅允許英數字與底線
    /// </remarks>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 密碼雜湊 (BCrypt)
    /// </summary>
    /// <remarks>
    /// 使用 BCrypt.Net-Next 雜湊,work factor 為 12,長度固定 60 字元
    /// </remarks>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    /// <remarks>
    /// 最大長度 100 字元,支援繁體中文與所有 Unicode 字元
    /// </remarks>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 是否已刪除 (軟刪除標記)
    /// </summary>
    /// <remarks>
    /// true: 帳號已刪除但資料保留供審計
    /// false: 帳號有效
    /// </remarks>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 刪除時間 (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 刪除操作者 ID
    /// </summary>
    /// <remarks>
    /// 記錄執行刪除操作的使用者 ID,可關聯至 users.id
    /// </remarks>
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// 版本號 (樂觀並發控制)
    /// </summary>
    /// <remarks>
    /// 初始值為 1,每次更新遞增
    /// 用於偵測並發更新衝突
    /// </remarks>
    public int Version { get; set; }
}
