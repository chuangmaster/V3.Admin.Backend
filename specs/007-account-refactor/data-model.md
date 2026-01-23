# Data Model: Account Module Refactoring

**Feature**: Account Module Refactoring  
**Branch**: `007-account-refactor`  
**Date**: 2026-01-20

## Overview

此文件定義 Account Module Refactoring 功能所需的資料模型變更,包含 User entity 的欄位重命名,以及使用既有 version 欄位進行併發控制和 token 驗證。

## Entity Changes

### User Entity (Modified)

**資料庫表名**: `users`  
**變更類型**: 欄位重命名

#### 欄位定義

| 欄位名 (DB) | C# 屬性 | 型別 | 必填 | 說明 | 變更 |
|------------|---------|------|------|------|------|
| id | Id | int | ✓ | 用戶唯一識別碼 | 無變更 |
| **account** | **Account** | string(50) | ✓ | 用戶帳號(原 username) | **重命名** |
| password | Password | string(255) | ✓ | BCrypt 加密後的密碼 | 無變更 |
| email | Email | string(100) | ✓ | 電子郵件 | 無變更 |
| display_name | DisplayName | string(100) | ✓ | 顯示名稱 | 無變更 |
| version | Version | int | ✓ | 併發控制版本號,同時用於 JWT token 驗證 | **說明更新** |
| is_active | IsActive | bool | ✓ | 是否啟用 | 無變更 |
| created_at | CreatedAt | DateTime | ✓ | 建立時間 | 無變更 |
| updated_at | UpdatedAt | DateTime | ✓ | 更新時間 | 無變更 |
| deleted_at | DeletedAt | DateTime? | ✗ | 軟刪除時間 | 無變更 |

#### 索引

| 索引名 | 欄位 | 類型 | 說明 | 變更 |
|--------|------|------|------|------|
| pk_users | id | PRIMARY KEY | 主鍵 | 無變更 |
| **idx_users_account** | **account** | UNIQUE | 帳號唯一索引 | **重命名**(原 idx_users_username) |
| idx_users_email | email | UNIQUE | 電子郵件唯一索引 | 無變更 |

#### C# Entity Class

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V3.Admin.Backend.Models.Entities
{
    /// <summary>
    /// 用戶實體
    /// </summary>
    [Table("users")]
    public class User
    {
        /// <summary>
        /// 用戶 ID
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// 用戶帳號(用於登入)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("account")]
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 密碼(BCrypt 加密)
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 電子郵件
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 顯示名稱
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 併發控制版本號(任何更新操作時遞增)
        /// 同時用於 JWT token 驗證,任何資料修改都會使 token 失效
        /// </summary>
        [Column("version")]
        public int Version { get; set; } = 0;

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 建立時間
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新時間
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 軟刪除時間
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}
```

---

### AuditLog Entity (No Changes, Usage Clarified)

**資料庫表名**: `audit_logs`  
**變更類型**: 無結構變更,明確使用方式

#### 欄位定義

| 欄位名 (DB) | C# 屬性 | 型別 | 必填 | 說明 |
|------------|---------|------|------|------|
| id | Id | int | ✓ | 審計日誌 ID |
| action | Action | string(50) | ✓ | 操作類型 |
| operator_id | OperatorId | int? | ✗ | 操作者用戶 ID |
| target_user_id | TargetUserId | int? | ✗ | 被操作的用戶 ID |
| details | Details | string | ✗ | JSON 格式詳細資訊 |
| ip_address | IpAddress | string(50) | ✗ | 操作者 IP 地址 |
| created_at | CreatedAt | DateTime | ✓ | 操作時間 |

#### 密碼重設審計日誌範例

```csharp
// Service 層記錄審計日誌
await _auditLogRepository.CreateAsync(new AuditLog
{
    Action = "PasswordReset",
    OperatorId = currentUserId,           // 管理員 ID
    TargetUserId = targetUserId,          // 被重設密碼的用戶 ID
    Details = JsonSerializer.Serialize(new
    {
        ResetBy = currentUser.Account,
        Timestamp = DateTime.UtcNow
    }),
    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
    CreatedAt = DateTime.UtcNow
});
```

**重要**: 絕對不可在 Details 或任何欄位中記錄密碼(明文或加密)。

---

## DTOs (Data Transfer Objects)

### UserDto (Modified)

用於 API 回應和 Service 層資料傳遞。

```csharp
namespace V3.Admin.Backend.Models.Dtos
{
    /// <summary>
    /// 用戶資料傳輸物件
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// 用戶 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用戶帳號
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 電子郵件
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 顯示名稱
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 併發控制版本號
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        // 注意:不包含 Password(安全性考量)
    }
}
```

---

## Request Models

### ChangePasswordRequest (New)

用戶自助密碼修改請求。

```csharp
namespace V3.Admin.Backend.Models.Requests
{
    /// <summary>
    /// 用戶修改密碼請求
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// 舊密碼
        /// </summary>
        [Required(ErrorMessage = "舊密碼為必填")]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密碼
        /// </summary>
        [Required(ErrorMessage = "新密碼為必填")]
        [MinLength(8, ErrorMessage = "密碼長度至少 8 個字元")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// 併發控制版本號
        /// </summary>
        [Required(ErrorMessage = "版本號為必填")]
        public int Version { get; set; }
    }
}
```

### ResetPasswordRequest (New)

管理員重設密碼請求。

```csharp
namespace V3.Admin.Backend.Models.Requests
{
    /// <summary>
    /// 管理員重設密碼請求
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// 新密碼
        /// </summary>
        [Required(ErrorMessage = "新密碼為必填")]
        [MinLength(8, ErrorMessage = "密碼長度至少 8 個字元")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// 併發控制版本號
        /// </summary>
        [Required(ErrorMessage = "版本號為必填")]
        public int Version { get; set; }
    }
}
```

---

## Database Migration Scripts

### Migration 014: Rename username to account

**檔案**: `Database/Migrations/014_RenameUsernameToAccount.sql`

```sql
-- 將 username 欄位重命名為 account
-- Migration: 014
-- Date: 2026-01-20
-- Description: Rename username field to account for better semantic clarity
-- Feature: 007-account-refactor

BEGIN;

-- Step 1: 重命名欄位
ALTER TABLE users RENAME COLUMN username TO account;

-- Step 2: 更新索引名稱
ALTER INDEX IF EXISTS idx_users_username RENAME TO idx_users_account;

-- Step 3: 更新約束名稱
ALTER TABLE users DROP CONSTRAINT IF EXISTS chk_username_length;
ALTER TABLE users ADD CONSTRAINT chk_account_length 
    CHECK (length(account) >= 3 AND length(account) <= 50);

ALTER TABLE users DROP CONSTRAINT IF EXISTS uq_users_username;
ALTER TABLE users ADD CONSTRAINT uq_users_account UNIQUE (account);

-- Step 4: 資料完整性檢查
DO $$
DECLARE
    total_count INT;
    null_account_count INT;
BEGIN
    SELECT COUNT(*) INTO total_count FROM users;
    SELECT COUNT(*) INTO null_account_count 
    FROM users 
    WHERE account IS NULL OR account = '';
    
    IF null_account_count > 0 THEN
        RAISE EXCEPTION '資料完整性檢查失敗: % 個用戶的 account 為 NULL 或空字串', null_account_count;
    END IF;
    
    RAISE NOTICE '✓ 遷移成功: % 個用戶已遷移', total_count;
END $$;

COMMIT;
```

### Migration 015: Add account module permissions

**檔案**: `Database/Migrations/015_AddAccountModulePermissions.sql`

此遷移新增帳號模組所需的功能權限。

```sql
-- 新增帳號模組相關權限
-- Migration: 015
-- Date: 2026-01-24
-- Description: Add account module permissions for account management and password operations
-- Feature: 007-account-refactor
```

**新增的權限**:

| 權限代碼 | 名稱 | 說明 | 對應端點 |
|---------|------|------|---------|
| user.profile.update | 修改個人資料 | 允許用戶修改自己的個人資料，包括密碼變更 | PUT /api/account/me/password |
| account.read | 查詢帳號 | 允許查詢帳號列表和單一帳號詳情 | GET /api/account, GET /api/account/{id} |
| account.create | 新增帳號 | 允許建立新的使用者帳號 | POST /api/account |
| account.update | 更新帳號 | 允許更新帳號資訊和重設使用者密碼 | PUT /api/account/{id}, PUT /api/account/{id}/reset-password |
| account.delete | 刪除帳號 | 允許刪除使用者帳號（軟刪除） | DELETE /api/account/{id} |

**自動行為**:
- 所有權限自動指派給 Admin 角色
- 使用 `ON CONFLICT (permission_code) DO NOTHING` 確保冪等性
- 包含驗證邏輯，確認權限建立成功並正確指派

---

## Relationships

### User ↔ AuditLog

- **Relationship**: One-to-Many
- **Foreign Keys**:
  - `audit_logs.operator_id` → `users.id` (操作者)
  - `audit_logs.target_user_id` → `users.id` (被操作者)
- **說明**: 一個用戶可以執行多個操作(operator),也可以是多個操作的目標(target)

---

## Migration Execution Order

執行 migration scripts 的順序:

1. `XXX_RenameUsernameToAccount.sql` - 重命名欄位

**注意**: 此 migration 必須在部署新程式碼之前執行完成。

---

## Mapping Strategy

### Entity ↔ DTO Mapping

```csharp
// Entity → DTO (使用 extension method 或 mapping library)
public static UserDto ToDto(this User user)
{
    return new UserDto
    {
        Id = user.Id,
        Account = user.Account,          // 使用新欄位名稱
        Email = user.Email,
        DisplayName = user.DisplayName,
        Version = user.Version,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
        // 不包含 Password(安全性考量)
    };
}
```

---

## Validation Rules

### Account Field

- **長度**: 1-50 字元
- **格式**: 允許英數字、底線、連字號
- **唯一性**: 必須唯一(database unique constraint)
- **必填**: 不可為 NULL 或空字串

### Password Field

- **長度**: 8-100 字元(原始密碼,加密後會更長)
- **強度**: 至少包含一個大寫字母、一個小寫字母、一個數字(由 validator 驗證)
- **加密**: 使用 BCrypt,work factor 12

### Version Field

- **型別**: int
- **初始值**: 0
- **遞增**: 每次更新操作 +1
- **驗證**: UPDATE 時必須匹配資料庫當前值
- **額外用途**: 用於 JWT token 驗證,任何資料修改都會使 token 失效

---

## Next Steps

✅ Phase 1.1 完成:資料模型已定義  
➡️ Phase 1.2:建立 contracts/api-spec.yaml  
➡️ Phase 1.3:建立 quickstart.md
