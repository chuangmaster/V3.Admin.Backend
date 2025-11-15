# Data Model: 用戶個人資料查詢 API

**Feature**: 003-user-profile  
**Date**: 2025-11-12  
**Purpose**: 定義資料結構與實體關聯

## 概述

此功能使用現有資料表，不需要建立新的資料庫結構。主要涉及讀取 `users`、`roles` 和 `user_roles` 表的資料。

## 資料庫實體

### Users (現有表)

**表名**: `users`

**相關欄位**:
| 欄位名 | 類型 | 說明 | 約束 |
|--------|------|------|------|
| id | uuid | 用戶唯一識別碼 | PRIMARY KEY |
| username | varchar(20) | 用戶名稱 | NOT NULL, UNIQUE |
| display_name | varchar(100) | 顯示名稱 | NULL（可為空） |
| is_deleted | boolean | 軟刪除標記 | NOT NULL, DEFAULT false |

**用途**: 提供用戶基本資訊（username, display_name）

---

### Roles (現有表)

**表名**: `roles`

**相關欄位**:
| 欄位名 | 類型 | 說明 | 約束 |
|--------|------|------|------|
| id | uuid | 角色唯一識別碼 | PRIMARY KEY |
| name | varchar(50) | 角色名稱 | NOT NULL, UNIQUE |
| is_deleted | boolean | 軟刪除標記 | NOT NULL, DEFAULT false |

**用途**: 提供角色名稱

---

### UserRoles (現有表)

**表名**: `user_roles`

**相關欄位**:
| 欄位名 | 類型 | 說明 | 約束 |
|--------|------|------|------|
| user_id | uuid | 用戶 ID | FOREIGN KEY → users.id |
| role_id | uuid | 角色 ID | FOREIGN KEY → roles.id |
| is_deleted | boolean | 軟刪除標記 | NOT NULL, DEFAULT false |

**用途**: 關聯用戶與角色的多對多關係

---

## C# 實體模型

### User Entity (現有)

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 用戶實體
/// </summary>
public class User
{
    /// <summary>
    /// 用戶 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用戶名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 是否已刪除
    /// </summary>
    public bool IsDeleted { get; set; }

    // ... 其他欄位
}
```

---

### Role Entity (現有)

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 角色實體
/// </summary>
public class Role
{
    /// <summary>
    /// 角色 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否已刪除
    /// </summary>
    public bool IsDeleted { get; set; }

    // ... 其他欄位
}
```

---

## DTO 模型

### UserProfileResponse (新增)

```csharp
namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 用戶個人資料回應 DTO
/// </summary>
public class UserProfileResponse
{
    /// <summary>
    /// 用戶名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱（可為 null）
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 角色名稱清單
    /// </summary>
    public List<string> Roles { get; set; } = new();
}
```

**JSON 範例** (有角色):
```json
{
  "username": "john_doe",
  "displayName": "John Doe",
  "roles": ["Admin", "User"]
}
```

**JSON 範例** (無顯示名稱、無角色):
```json
{
  "username": "jane_doe",
  "displayName": null,
  "roles": []
}
```

---

## 資料流程

```
[Client Request]
    ↓
[JWT Middleware] → 驗證 token，提取 user ID from 'sub' claim
    ↓
[Permission Middleware] → 檢查 'user.profile.read' 權限
    ↓
[AccountController.GetMyProfile]
    ↓
[AccountService.GetUserProfileAsync(userId)]
    ↓
[UserRepository.GetUserByIdAsync(userId)] → 查詢 users 表
    ↓
[UserRoleRepository.GetRoleNamesByUserIdAsync(userId)] → 
    查詢 user_roles + roles 表（LEFT JOIN）
    ↓
[組合成 UserProfileResponse]
    ↓
[ApiResponseModel<UserProfileResponse>]
    ↓
[Client Response]
```

---

## 資料驗證規則

### 輸入驗證
- **無輸入參數**：用戶 ID 來自 JWT token

### 業務規則驗證
1. 用戶必須存在於資料庫
2. 用戶的 `is_deleted` 必須為 `false`
3. 角色的 `is_deleted` 必須為 `false`
4. 用戶角色關聯的 `is_deleted` 必須為 `false`

### 錯誤情況
| 情況 | HTTP 狀態碼 | 業務代碼 | 訊息 |
|------|-------------|----------|------|
| Token 無效/過期 | 401 | UNAUTHORIZED | 未授權，請先登入 |
| 用戶不存在 | 404 | NOT_FOUND | 用戶不存在 |
| 用戶已停用 | 401 | UNAUTHORIZED | 帳號已停用 |
| 無權限 | 403 | FORBIDDEN | 無權限執行此操作 |

---

## 資料庫查詢

### 查詢用戶與角色 (推薦方式)

```sql
-- 一次性查詢用戶和所有角色
SELECT 
    u.id,
    u.username,
    u.display_name,
    r.name as role_name
FROM users u
LEFT JOIN user_roles ur 
    ON u.id = ur.user_id 
    AND ur.is_deleted = false
LEFT JOIN roles r 
    ON ur.role_id = r.id 
    AND r.is_deleted = false
WHERE u.id = @UserId 
    AND u.is_deleted = false;
```

**說明**:
- 使用 LEFT JOIN 確保沒有角色的用戶也能查詢
- 過濾已刪除的角色和用戶角色關聯
- 一次查詢完成，減少資料庫往返

**Dapper 實作範例**:
```csharp
var sql = @"
    SELECT 
        u.id, u.username, u.display_name,
        r.name as role_name
    FROM users u
    LEFT JOIN user_roles ur ON u.id = ur.user_id AND ur.is_deleted = false
    LEFT JOIN roles r ON ur.role_id = r.id AND r.is_deleted = false
    WHERE u.id = @UserId AND u.is_deleted = false";

var result = await connection.QueryAsync<UserProfileDto>(sql, new { UserId = userId });
```

---

## 效能考量

### 索引建議 (現有)
- `users.id`: PRIMARY KEY（已存在）
- `user_roles.user_id`: 外鍵索引（應已存在）
- `user_roles.role_id`: 外鍵索引（應已存在）
- `roles.id`: PRIMARY KEY（已存在）

### 查詢複雜度
- **時間複雜度**: O(1) 主鍵查詢 + O(n) 角色數量
- **預期角色數**: 通常 1-5 個，最多不超過 20 個
- **預估執行時間**: 10-50ms

### 快取策略 (可選)
- 短期快取用戶角色資訊（5-10 分鐘）
- 使用記憶體快取減少資料庫查詢
- 當前 MVP 不實作快取

---

## 資料對應

### Entity → DTO 對應

| Entity 欄位 | DTO 欄位 | 轉換邏輯 |
|-------------|----------|----------|
| User.Username | UserProfileResponse.Username | 直接對應 |
| User.DisplayName | UserProfileResponse.DisplayName | 直接對應（可為 null） |
| Role.Name (多筆) | UserProfileResponse.Roles | 聚合為 List&lt;string&gt; |

### 特殊處理
- **無角色**: `Roles = []` (空陣列，非 null)
- **DisplayName 為空**: 保持 `null`，不轉換為空字串
- **已刪除的角色**: 不包含在結果中

---

## 總結

- ✅ 使用現有資料表，無需 migration
- ✅ 資料模型清晰，遵循現有慣例
- ✅ DTO 設計符合規格要求
- ✅ 查詢效能符合目標 (<200ms)
- ✅ 錯誤處理完整定義
