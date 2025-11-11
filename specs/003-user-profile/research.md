# Research: 用戶個人資料查詢 API

**Feature**: 003-user-profile  
**Date**: 2025-11-12  
**Purpose**: 解決技術實作細節與最佳實踐

## 研究項目

### 1. JWT Claims 提取用戶身份

**決策**: 使用 BaseApiController 的 `GetUserId()` 方法提取用戶 ID

**理由**:
- BaseApiController 已提供標準化的用戶 ID 提取方法
- 自動處理多個 claim types (`sub` 和 `ClaimTypes.NameIdentifier`)
- 返回 `Guid?` 類型，符合系統設計（User.Id 為 Guid）
- 減少重複程式碼，保持一致性

**實作方式**:
```csharp
// 在 Controller 中（繼承自 BaseApiController）
var userId = GetUserId();
if (userId is null)
{
    return UnauthorizedResponse("未授權，請先登入");
}
```

**BaseApiController.GetUserId() 實作**:
```csharp
protected Guid? GetUserId()
{
    var userIdClaim = User.FindFirst("sub")?.Value 
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return null;
    }

    return userId;
}
```

**替代方案**:
- 在每個 Controller 自行實作：重複程式碼，容易不一致
- 直接存取 User.Claims：增加複雜度，BaseController 已封裝

---

### 2. 停用帳號的 Token 驗證策略

**決策**: 在 JWT 驗證中介層 (Middleware) 加入帳號狀態檢查

**理由**:
- 集中化安全控制，所有端點自動受保護
- 停用帳號應立即失去存取權限
- 符合規格要求「在 token 驗證階段即拒絕存取」

**實作方式**:
```csharp
// 在 JWT 驗證成功後的中介層中
var userId = context.User.FindFirst("sub")?.Value;
var user = await userRepository.GetUserByIdAsync(int.Parse(userId));
if (user == null || user.IsDeleted)
{
    context.Response.StatusCode = 401;
    return;
}
```

**位置**: 
- 選項 A: 擴展現有的 JWT 驗證中介層
- 選項 B: 新增專用的帳號狀態驗證中介層（推薦）

**替代方案**:
- 在每個端點檢查：重複程式碼，容易遺漏
- 依賴 token 過期：停用帳號仍可在 token 有效期內存取

---

### 3. 角色資料查詢最佳實踐

**決策**: 使用 LEFT JOIN 一次性查詢用戶和角色資料

**理由**:
- 減少資料庫往返次數（1 次查詢 vs 2 次查詢）
- 符合效能目標 <200ms
- PostgreSQL 對 JOIN 查詢優化良好

**SQL 範例**:
```sql
SELECT 
    u.id, u.username, u.display_name,
    r.name as role_name
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id AND ur.is_deleted = false
LEFT JOIN roles r ON ur.role_id = r.id AND r.is_deleted = false
WHERE u.id = @UserId AND u.is_deleted = false
```

**資料處理**:
- 使用 Dapper 的 `SplitOn` 參數處理多重結果
- 或使用 `QueryMultiple` 分離查詢後在記憶體中組合

**替代方案**:
- 兩次獨立查詢：簡單但效能較差（2x 資料庫往返）
- 儲存程序：增加維護複雜度，不符合專案慣例

---

### 4. 權限定義策略

**決策**: 定義 `user.profile.read` 權限

**理由**:
- 遵循專案既有權限命名慣例 `{resource}.{action}`
- 將來可擴展為 `user.profile.update` 等相關權限
- 符合 Constitution 要求的權限設計模式

**權限設定**:
```sql
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('user.profile.read', '查詢個人資料', '允許查詢當前用戶的個人資料', 'function')
ON CONFLICT (permission_code) DO NOTHING;
```

**Controller 標註**:
```csharp
[HttpGet("me")]
[RequirePermission("user.profile.read")]
public async Task<IActionResult> GetMyProfile()
```

**替代方案**:
- 不使用權限，僅依賴 JWT 驗證：無法細緻控制功能存取
- 使用現有 `account.read` 權限：語意不夠精確

---

### 5. API 端點路由設計

**決策**: `GET /api/account/me`

**理由**:
- RESTful 慣例，`/me` 代表當前用戶
- 與現有 AccountController 路由一致
- 語意清晰，前端容易理解

**完整路由**:
```
GET /api/account/me
Authorization: Bearer {jwt_token}
```

**替代方案**:
- `/api/account/{id}`: 需要傳遞 ID，增加複雜度
- `/api/user/profile`: 需要新增 Controller，增加檔案數量
- `/api/me`: 太過簡短，不符合現有 API 結構

---

### 6. 空值處理策略

**決策**: 使用 C# nullable types 搭配 JSON 序列化設定

**理由**:
- displayname 可能為 null（規格明確要求）
- roles 應回傳空陣列 `[]` 而非 null（避免前端 null 檢查）
- 符合 C# 13 nullable reference types 最佳實踐

**DTO 定義**:
```csharp
public class UserProfileResponse
{
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }  // nullable
    public List<string> Roles { get; set; } = new();  // 預設空陣列
}
```

**JSON 輸出範例**:
```json
{
  "username": "john_doe",
  "displayName": null,
  "roles": []
}
```

**替代方案**:
- 使用空字串代替 null：不符合規格要求
- roles 使用 null：前端需要額外 null 檢查

---

## 總結

所有技術細節已明確定義，無剩餘 NEEDS CLARIFICATION 項目。實作可以直接進入 Phase 1（資料模型與合約設計）。

**關鍵技術決策**:
1. ✅ 從 JWT `sub` claim 提取用戶 ID
2. ✅ 在中介層驗證帳號狀態
3. ✅ 使用 LEFT JOIN 一次性查詢
4. ✅ 定義 `user.profile.read` 權限
5. ✅ 路由為 `GET /api/account/me`
6. ✅ displayName 可為 null，roles 為空陣列

**效能預估**:
- 資料庫查詢時間：~10-50ms（單一 JOIN 查詢）
- JWT 驗證與反序列化：~5-10ms
- 物件組合與序列化：~5-10ms
- **總計**: ~20-70ms（遠低於 200ms 目標）

**安全性確認**:
- ✅ JWT 驗證必須
- ✅ 權限驗證必須
- ✅ 停用帳號自動拒絕
- ✅ 無敏感資訊洩漏風險
