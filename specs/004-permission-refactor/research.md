````markdown
# Research 研究報告：權限模型重構 - 移除 RoutePath、整合路由決策至 PermissionCode

**Date**: 2025-11-16  
**Branch**: `004-permission-refactor`  
**Status**: Completed (Phase 0)

---

## 摘要

本研究針對功能規格中的關鍵設計決策進行澄清和驗證。基於現有的澄清會議記錄和專案憲法，本報告確認了 PermissionType 擴展機制、PermissionCode 編碼規範、以及資料庫遷移策略的最終方向。

---

## 研究主題

### 1. PermissionType 擴展機制設計

**決策**: 採用 **Enum + Database Table 混合方案**（預留擴展性）

**依據**:
- 現有系統中 PermissionType 目前有 3 種：`function`（操作權限）、`view`（區塊瀏覽權限）、`route`（路由權限，本次重構移除）
- 根據澄清會議：「預留擴展機制（enum 或 database table），便於未來新增類型」
- 考量長期維護：未來可能新增 `report`、`api`、`module` 等類型

**技術方案**:
1. **短期（本次重構）**: 維持 Enum 方式，在 C# 中定義：
   ```csharp
   public enum PermissionType
   {
       /// <summary>功能操作權限（如新增、編輯、刪除）</summary>
       Function = 1,
       
       /// <summary>UI 區塊瀏覽權限（如儀表板小工具）</summary>
       View = 2
   }
   ```

2. **中期（未來擴展準備）**: 設計資料庫表 `permission_types` 以支持動態類型：
   - `id` (int, PK)
   - `type_code` (varchar, unique) - e.g., 'function', 'view', 'report'
   - `type_name` (varchar) - 型別名稱
   - `description` (text) - 描述
   - `is_active` (boolean) - 是否啟用
   - `created_at` (datetime)
   - `updated_at` (datetime)

3. **長期轉換路徑**:
   - 當需要動態新增類型時，先遷移 Enum 值到 `permission_types` 表
   - 修改 Permission 表的 `permission_type` 欄位為外鍵參考 `permission_types.id`
   - 應用程式邏輯改為查詢表而非依賴 Enum（向後相容）

**實施細節**:
- 在 `PermissionRepository` 中實現快取機制以避免頻繁查詢（若後續採用 DB 方案）
- Permission 驗證時使用 `PermissionService.IsValidPermissionType()` 方法檢查
- API DTO 使用字符串表示（如 `"function"`, `"view"`）以支持前後端統一

**相容性**:
- C# Enum 確保型別安全和編譯時檢查
- 資料庫儲存字符串或整數便於未來遷移
- 前端接收字符串，與後端 Enum 名稱對應

**替代方案評估**:
- ❌ 純 Enum：無法在運行時新增類型（被否決）
- ❌ 純資料庫表：初期性能額外開銷（推遲至需要時採用）
- ✅ 混合方案：平衡現實需求和未來擴展性

---

### 2. PermissionCode 編碼規範與驗證

**決策**: **多層級格式 + 靈活驗證 + 無硬格式限制**

**依據**:
- 澄清會議：「PermissionCode 驗證規則是否需要硬限制格式？→ 不需要硬限制格式，保持靈活」
- 現有使用場景包括：
  - 操作層：`permission.create`, `role.update`, `account.delete`
  - UI 區塊層：`dashboard.summary_widget`, `reports.analytics_panel`, `inventory.quick_actions`
  - 多層級資源：`inventory.warehouse.transfer`, `reports.sales.daily_summary`

**編碼規範**:

| 場景 | 格式範例 | 說明 |
|------|--------|------|
| 基礎操作 | `resource.action` | `permission.create`, `role.read` |
| UI 區塊 | `resource.component` | `dashboard.widget`, `header.notifications` |
| 多層級資源 | `resource.subresource.action` | `inventory.warehouse.transfer` |
| 特殊操作 | `resource.operation` | `account.export`, `role.import` |

**驗證規則**:
```csharp
/// <summary>
/// PermissionCode 驗證規則：
/// 1. 不能為空或全空白
/// 2. 長度介於 3-100 字元
/// 3. 允許字母（a-z, A-Z）、數字（0-9）、下劃線（_）、點號（.）
/// 4. 開頭和結尾不能是點號或下劃線
/// 5. 連續的點號或下劃線不允許（如 '..', '__'）
/// 驗證式：^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$
/// </summary>
```

**實施位置**:
- `CreatePermissionRequestValidator.cs`: 新增/編輯時驗證
- `PermissionService.ValidatePermissionCode()`: 商業邏輯層驗證
- 資料庫層級：建議建立 CHECK constraint （可選）

**向後相容**:
- 舊系統中含 `RoutePath` 的權限已移除
- 當前系統無 route 類型權限，無遷移需求
- 新建立權限遵循此規範

**替代方案評估**:
- ❌ 嚴格正則限制（如 `^[a-z]+\.[a-z]+$`）：過於死板，不符合未來多層級需求
- ❌ 無驗證：容易導致命名混亂和系統混亂
- ✅ 靈活驗證：允許多樣性同時保持基本的格式約束

---

### 3. 資料庫遷移策略

**決策**: **立即刪除 RoutePath，無過渡期**

**依據**:
- 澄告會議：「RoutePath 欄位的最終處理方式？→ 立即刪除，從資料庫 schema 和所有程式碼中完全移除」
- 現狀：當前系統沒有 route 類型權限，無遷移成本
- 需求：FR-001 明確指出「完全刪除，不經過廢棄期」

**遷移步驟**:

1. **建立遷移檔案** `Migrations/[timestamp]_RemoveRoutePath.cs`：
   ```csharp
   public override void Up(MigrationBuilder migrationBuilder)
   {
       // 移除 RoutePath 欄位
       migrationBuilder.DropColumn(
           name: "route_path",
           table: "permissions");
   }

   public override void Down(MigrationBuilder migrationBuilder)
   {
       // 回滾時重新建立欄位（用於開發/測試）
       migrationBuilder.AddColumn<string>(
           name: "route_path",
           table: "permissions",
           type: "character varying(255)",
           nullable: true);
   }
   ```

2. **移除 Permission 實體中的 RoutePath 屬性**:
   ```csharp
   public class Permission
   {
       public int Id { get; set; }
       public string PermissionCode { get; set; }
       public string Name { get; set; }
       public string Description { get; set; }
       public PermissionType PermissionType { get; set; }
       // RoutePath 已移除 ❌
       // ... other properties
   }
   ```

3. **更新所有涉及 Permission 的 DTO**:
   - `CreatePermissionRequest`: 移除 `RoutePath` 欄位
   - `UpdatePermissionRequest`: 移除 `RoutePath` 欄位
   - `PermissionResponse`: 移除 `RoutePath` 屬性

4. **更新驗證器**:
   - `CreatePermissionRequestValidator.cs`: 移除 RoutePath 驗證
   - `UpdatePermissionRequestValidator.cs`: 移除 RoutePath 驗證

5. **清理程式碼**:
   - `PermissionService`: 移除所有 RoutePath 相關邏輯
   - `PermissionRepository`: 移除 RoutePath 查詢邏輯
   - `PermissionAuthorizationMiddleware`: 確認 RoutePath 不被使用
   - 所有控制器回應中移除 RoutePath

**資料整合**:
- 無需資料遷移（無舊資料包含 RoutePath）
- 若開發環境存在舊資料，遷移後自動忽略

**風險評估**:
- ✅ 低風險：當前系統無 RoutePath 資料
- ✅ 無級聯依賴：Permission 表的其他表已正確設定外鍵級聯刪除
- ✅ 測試覆蓋：需確保所有權限驗證邏輯測試通過

---

### 4. Permission 權限驗證邏輯更新

**決策**: **PermissionCode 直接決定路由和 UI 元件權限**

**依據**:
- FR-006：「系統 MUST 更新所有權限驗證邏輯以適配新模型，PermissionCode 直接決定路由和 UI 元件權限」
- 移除 RoutePath 後，授權檢查完全依賴 PermissionCode 和 PermissionType

**實施更新**:

1. **PermissionValidationService** 更新:
   ```csharp
   public class PermissionValidationService
   {
       /// <summary>檢查用戶是否擁有指定權限</summary>
       public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
       {
           // 查詢用戶透過角色所擁有的權限
           var userPermissions = await _userRepository.GetUserPermissionsAsync(userId);
           return userPermissions.Any(p => p.PermissionCode == permissionCode);
       }

       /// <summary>檢查用戶是否擁有指定型別的權限</summary>
       public async Task<bool> HasPermissionTypeAsync(int userId, string permissionCode, PermissionType type)
       {
           var permission = await _permissionRepository.GetByCodeAsync(permissionCode);
           if (permission == null) return false;
           
           if (permission.PermissionType != type) return false;
           
           return await HasPermissionAsync(userId, permissionCode);
       }
   }
   ```

2. **PermissionAuthorizationMiddleware** 更新:
   ```csharp
   public class PermissionAuthorizationMiddleware
   {
       public async Task InvokeAsync(HttpContext context, IPermissionValidationService permissionValidationService)
       {
           // 提取 [RequirePermission] 屬性
           var endpoint = context.GetEndpoint();
           var requirePermissionAttr = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();
           
           if (requirePermissionAttr != null)
           {
               var userId = context.User.FindFirst("sub")?.Value;
               if (!int.TryParse(userId, out var userIdInt))
               {
                   context.Response.StatusCode = 401;
                   return;
               }

               var hasPermission = await permissionValidationService.HasPermissionAsync(
                   userIdInt, 
                   requirePermissionAttr.PermissionCode);
               
               if (!hasPermission)
               {
                   // 記錄權限失敗
                   await _permissionFailureLogRepository.LogAsync(
                       userId: userIdInt,
                       permissionCode: requirePermissionAttr.PermissionCode,
                       reason: "User does not have required permission");
                   
                   context.Response.StatusCode = 403;
                   return;
               }
           }

           await _next(context);
       }
   }
   ```

3. **前端查詢權限** (API endpoint):
   ```csharp
   /// <summary>檢查當前用戶是否擁有指定權限</summary>
   [HttpGet("check/{permissionCode}")]
   [Authorize]
   public async Task<IActionResult> CheckPermission(string permissionCode)
   {
       var userId = User.FindFirst("sub").Value;
       var hasPermission = await _permissionValidationService.HasPermissionAsync(
           int.Parse(userId), 
           permissionCode);
       
       return Success(new { hasPermission });
   }
   ```

**相容性**:
- 所有權限檢查改為基於 PermissionCode（不再涉及 RoutePath）
- UI 區塊渲染時查詢 `view` 型別權限
- 操作按鈕渲染時查詢 `function` 型別權限

---

### 5. API 合約與稽核日誌

**決策**: **PermissionFailureLog 調整，Permission 操作記錄稽核**

**依據**:
- FR-008：「系統 MUST 在所有 Permission 異動操作（CREATE、UPDATE、DELETE）時記錄稽核日誌」
- PermissionFailureLog 需要調整以適應新模型（移除 RoutePath 欄位）

**稽核日誌設計**:

1. **PermissionFailureLog 欄位調整**:
   - 移除：`route_path` 欄位（已移除）
   - 保留：`user_id`, `permission_code`, `created_at`
   - 新增：`request_path` (記錄實際請求路徑), `reason` (失敗原因)

2. **AuditLog 擴展** (現有機制):
   - 所有 Permission 表的 CREATE/UPDATE/DELETE 自動記錄
   - 記錄內容：變更前後的完整資料、操作者、時間戳

**實施細節**:
```csharp
public class PermissionFailureLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PermissionCode { get; set; }
    public string RequestPath { get; set; }
    public string Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 結論與後續步驟

| 項目 | 決策 | 狀態 |
|------|------|------|
| PermissionType 擴展 | Enum + DB 混合方案 | ✅ 已決定 |
| PermissionCode 驗證 | 靈活格式 + 規範驗證 | ✅ 已決定 |
| RoutePath 移除 | 立即刪除，無過渡期 | ✅ 已決定 |
| 權限驗證邏輯 | PermissionCode 直接決策 | ✅ 已決定 |
| 稽核和日誌 | 擴展 PermissionFailureLog | ✅ 已決定 |

**Phase 0 完成。所有 NEEDS CLARIFICATION 已解決。**

**下一步**：進入 Phase 1 - 設計合約與資料模型

````
