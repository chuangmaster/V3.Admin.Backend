namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 權限類型列舉
/// </summary>
/// <remarks>
/// 注意：架構設計預留了擴展機制，可在將來升級為資料庫表 (permission_types)
/// 以支持動態新增類型（如 Report, Api 等）
/// </remarks>
public enum PermissionType
{
    /// <summary>
    /// 功能操作權限，代表用戶可以執行的動作
    /// </summary>
    /// <remarks>
    /// 範例：permission.create, role.update, account.delete, inventory.export
    /// 用於控制操作按鈕的顯示和功能呼叫的授權
    /// </remarks>
    Function = 1,

    /// <summary>
    /// UI 區塊瀏覽權限，代表用戶可以查看的 UI 元件或頁面區塊
    /// </summary>
    /// <remarks>
    /// 範例：dashboard.summary_widget, reports.analytics_panel, settings.advanced_options
    /// 用於控制前端 UI 元件的顯示/隱藏
    /// </remarks>
    View = 2

    // 未來擴展點（遷移到資料庫表後）：
    // Report = 3,     // 報表存取權限
    // Api = 4,        // API 存取權限
    // Module = 5      // 模組功能權限
}
