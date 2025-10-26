# 資料模型設計 - 帳號管理系統

**Feature**: 001-account-management  
**Date**: 2025-10-26  
**Status**: Phase 1 Complete

## 概述

本文件定義帳號管理系統的資料模型,包含資料庫實體、DTO (Data Transfer Objects)、驗證規則與狀態轉換。所有設計符合三層架構原則與專案憲法要求。

## 命名規則

本專案遵循以下命名規則:

- **Entity (實體)**: 單一資料表映射,直接對應資料庫表格結構 (例如: `User`, `Role`)
- **Request (請求 DTO)**: API 層的輸入模型,用於接收客戶端請求 (例如: `LoginRequest`, `CreateAccountRequest`)
- **Response (回應 DTO)**: API 層的輸出模型,用於回傳給客戶端 (例如: `LoginResponse`, `AccountResponse`)
- **View (視圖 DTO)**: Join 查詢結果的複合模型,包含多表關聯資料 (例如: `UserRoleView`, `AccountDetailView`)

**分層對應**:
- **Repository 層**: 使用 `Entity` (資料庫實體)
- **Service 層**: `Entity` ↔ `Request`/`Response`/`View` 轉換
- **Controller 層**: 使用 `Request`/`Response` DTO

---

## 1. 資料庫實體 (Entity)

### 1.1 User 實體

**目的**: 代表系統中的使用者帳號,儲存於 PostgreSQL `users` 資料表。

```csharp
/// <summary>
/// 使用者實體
/// </summary>
public class User
{
    /// <summary>
    /// 使用者唯一識別碼
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// 帳號名稱 (唯一,用於登入)
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密碼雜湊 (BCrypt)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// 是否已刪除 (軟刪除標記)
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// 刪除時間 (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// 刪除操作者 ID
    /// </summary>
    public Guid? DeletedBy { get; set; }
    
    /// <summary>
    /// 版本號 (樂觀並發控制)
    /// </summary>
    public int Version { get; set; }
}
```

**欄位約束**:
- `Id`: PRIMARY KEY, NOT NULL
- `Username`: UNIQUE (WHERE IsDeleted = false), NOT NULL, 3-20 字元, 正規表示式: `^[a-zA-Z0-9_]+$`
- `PasswordHash`: NOT NULL, BCrypt 雜湊字串 (60 字元)
- `DisplayName`: NOT NULL, 最大 100 字元
- `CreatedAt`: NOT NULL, DEFAULT CURRENT_TIMESTAMP
- `UpdatedAt`: NULL
- `IsDeleted`: NOT NULL, DEFAULT false
- `DeletedAt`: NULL
- `DeletedBy`: NULL, FOREIGN KEY (未來可關聯至 users.Id)
- `Version`: NOT NULL, DEFAULT 1

**索引**:
```sql
-- 主鍵索引
PRIMARY KEY (Id)

-- 帳號名稱唯一索引 (排除已刪除)
CREATE UNIQUE INDEX idx_users_username ON users(Username) WHERE IsDeleted = false;

-- 軟刪除查詢索引
CREATE INDEX idx_users_isdeleted ON users(IsDeleted);

-- 建立時間索引 (用於排序)
CREATE INDEX idx_users_createdat ON users(CreatedAt DESC);
```

---

## 2. Request DTOs (API 請求物件)

### 2.1 LoginRequest

**用途**: 使用者登入請求

```csharp
/// <summary>
/// 登入請求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 帳號名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密碼
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
```

**驗證規則**:
- `Username`: 必填,3-20 字元
- `Password`: 必填,最少 8 字元

---

### 2.2 CreateAccountRequest

**用途**: 新增帳號請求

```csharp
/// <summary>
/// 新增帳號請求
/// </summary>
public class CreateAccountRequest
{
    /// <summary>
    /// 帳號名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密碼
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
```

**驗證規則**:
- `Username`: 必填,3-20 字元,僅英數字與底線 `^[a-zA-Z0-9_]+$`
- `Password`: 必填,最少 8 字元,支援所有 Unicode 字元
- `DisplayName`: 必填,最大 100 字元

---

### 2.3 UpdateAccountRequest

**用途**: 更新帳號資訊請求

```csharp
/// <summary>
/// 更新帳號請求
/// </summary>
public class UpdateAccountRequest
{
    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
```

**驗證規則**:
- `DisplayName`: 必填,最大 100 字元

---

### 2.4 ChangePasswordRequest

**用途**: 變更密碼請求

```csharp
/// <summary>
/// 變更密碼請求
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 舊密碼
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// 新密碼
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
```

**驗證規則**:
- `OldPassword`: 必填,最少 8 字元
- `NewPassword`: 必填,最少 8 字元,支援所有 Unicode 字元
- 業務規則: `NewPassword` 不可與 `OldPassword` 相同

---

### 2.5 DeleteAccountRequest

**用途**: 刪除帳號請求

```csharp
/// <summary>
/// 刪除帳號請求
/// </summary>
public class DeleteAccountRequest
{
    /// <summary>
    /// 確認訊息 (必須為 "CONFIRM")
    /// </summary>
    public string Confirmation { get; set; } = string.Empty;
}
```

**驗證規則**:
- `Confirmation`: 必填,必須等於 "CONFIRM" (防止誤刪)

---

## 3. Response DTOs (API 回應物件)

**說明**: Response DTO 用於 API 層回傳資料給客戶端,不包含敏感資訊 (如密碼雜湊、內部欄位)。

### 3.1 LoginResponse

**用途**: 登入成功回應

```csharp
/// <summary>
/// 登入回應
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT Access Token
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Token 過期時間 (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// 使用者資訊
    /// </summary>
    public AccountResponse User { get; set; } = new();
}
```

---

### 3.2 AccountResponse

**用途**: 帳號資訊回應

```csharp
/// <summary>
/// 帳號資訊回應
/// </summary>
public class AccountResponse
{
    /// <summary>
    /// 使用者 ID
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// 帳號名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
```

**注意**: 不包含 `PasswordHash`、`IsDeleted`、`DeletedAt`、`DeletedBy` 等敏感或內部欄位。

---

### 3.3 AccountListResponse

**用途**: 帳號列表回應 (分頁)

```csharp
/// <summary>
/// 帳號列表回應
/// </summary>
public class AccountListResponse
{
    /// <summary>
    /// 帳號清單
    /// </summary>
    public List<AccountResponse> Items { get; set; } = new();
    
    /// <summary>
    /// 總數量
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 當前頁碼 (從 1 開始)
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// 每頁數量
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages { get; set; }
}
```

---

## 3.4 View DTOs (視圖物件)

**說明**: View DTO 用於表示 Join 查詢結果的複合模型。本功能目前無 Join 查詢需求,未來擴充角色權限管理時可能需要以下 View:

**未來範例** (角色權限功能):
```csharp
/// <summary>
/// 使用者角色視圖 (User JOIN UserRole JOIN Role)
/// </summary>
public class UserRoleView
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}
```

**注意**: 目前帳號管理功能僅操作單一 `users` 表格,因此僅使用 `User` Entity 與 `AccountResponse` DTO,無需 View DTO。

---

## 4. 驗證器 (FluentValidation)

### 4.1 LoginRequestValidator

```csharp
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("帳號不可為空")
            .Length(3, 20).WithMessage("帳號長度必須為 3-20 字元");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼不可為空")
            .MinimumLength(8).WithMessage("密碼長度至少 8 字元");
    }
}
```

---

### 4.2 CreateAccountRequestValidator

```csharp
public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("帳號不可為空")
            .Length(3, 20).WithMessage("帳號長度必須為 3-20 字元")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("帳號僅允許英數字與底線");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼不可為空")
            .MinimumLength(8).WithMessage("密碼長度至少 8 字元");
        
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("姓名不可為空")
            .MaximumLength(100).WithMessage("姓名長度不可超過 100 字元");
    }
}
```

---

### 4.3 UpdateAccountRequestValidator

```csharp
public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("姓名不可為空")
            .MaximumLength(100).WithMessage("姓名長度不可超過 100 字元");
    }
}
```

---

### 4.4 ChangePasswordRequestValidator

```csharp
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("舊密碼不可為空")
            .MinimumLength(8).WithMessage("舊密碼長度至少 8 字元");
        
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密碼不可為空")
            .MinimumLength(8).WithMessage("新密碼長度至少 8 字元");
        
        RuleFor(x => x)
            .Must(x => x.NewPassword != x.OldPassword)
            .WithMessage("新密碼不可與舊密碼相同")
            .WithName("NewPassword");
    }
}
```

---

### 4.5 DeleteAccountRequestValidator

```csharp
public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Confirmation)
            .NotEmpty().WithMessage("確認訊息不可為空")
            .Equal("CONFIRM").WithMessage("確認訊息必須為 CONFIRM");
    }
}
```

---

## 5. 業務規則與狀態轉換

### 5.1 帳號生命週期

```
[建立] → [活躍] → [軟刪除]
           ↓
        [更新資訊]
           ↓
        [變更密碼]
```

**狀態定義**:
- **建立**: `IsDeleted = false`, `CreatedAt` 已設定
- **活躍**: `IsDeleted = false`
- **軟刪除**: `IsDeleted = true`, `DeletedAt` 與 `DeletedBy` 已設定

---

### 5.2 業務規則

#### 登入規則
1. 帳號必須存在且 `IsDeleted = false`
2. 密碼必須通過 BCrypt 驗證
3. 登入失敗記錄失敗嘗試 (帳號、時間、IP)
4. 登入成功產生 JWT Token (1 小時有效期)

#### 新增帳號規則
1. 帳號名稱必須唯一 (不含已刪除帳號)
2. 密碼使用 BCrypt 雜湊 (work factor 12)
3. `CreatedAt` 自動設定為當前時間 (UTC)
4. `Id` 使用 GUID v4
5. `Version` 初始值為 1

#### 更新資訊規則
1. 使用者只能更新自己的資訊
2. 僅允許更新 `DisplayName`
3. `UpdatedAt` 自動更新為當前時間 (UTC)
4. `Version` 遞增 (樂觀並發控制)

#### 變更密碼規則
1. 必須驗證舊密碼正確
2. 新密碼不可與舊密碼相同
3. 新密碼使用 BCrypt 重新雜湊
4. `UpdatedAt` 自動更新
5. `Version` 遞增
6. 變更密碼後應撤銷所有現有 Token (未來實作)

#### 刪除帳號規則
1. 使用軟刪除 (設定 `IsDeleted = true`)
2. 設定 `DeletedAt` 為當前時間 (UTC)
3. 設定 `DeletedBy` 為操作者 ID
4. 不可刪除當前登入的帳號
5. 不可刪除最後一個有效帳號 (檢查 `COUNT(*) WHERE IsDeleted = false`)
6. 必須提供確認訊息 "CONFIRM"
7. 已刪除帳號無法登入

---

### 5.3 並發控制

使用樂觀並發控制 (Optimistic Concurrency):

```csharp
// 更新時檢查版本號
public async Task<bool> UpdateAsync(User user, int expectedVersion)
{
    const string sql = @"
        UPDATE users 
        SET DisplayName = @DisplayName, 
            UpdatedAt = @UpdatedAt, 
            Version = Version + 1
        WHERE Id = @Id 
          AND Version = @ExpectedVersion
          AND IsDeleted = false
    ";
    
    var affected = await _connection.ExecuteAsync(sql, new {
        user.DisplayName,
        UpdatedAt = DateTime.UtcNow,
        user.Id,
        ExpectedVersion = expectedVersion
    });
    
    return affected > 0; // false 表示並發衝突
}
```

如偵測到並發衝突:
- 回傳 `409 Conflict`
- 業務代碼: `CONCURRENT_UPDATE_CONFLICT`
- 訊息: "資料已被其他使用者更新,請重新載入後再試"

---

## 6. 資料映射 (Mapping)

**說明**: 資料映射在 Service 層執行,負責 Entity ↔ Request/Response/View DTO 之間的轉換。

### 6.1 Entity → Response DTO

**用途**: Repository 返回 Entity 後,Service 層轉換為 Response DTO 回傳給 Controller。

```csharp
/// <summary>
/// User Entity 擴充方法
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// 將 User Entity 轉換為 AccountResponse DTO
    /// </summary>
    public static AccountResponse ToResponse(this User user)
    {
        return new AccountResponse
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
```

---

### 6.2 Request DTO → Entity

**用途**: Controller 接收 Request DTO 後,Service 層轉換為 Entity 傳給 Repository。

```csharp
/// <summary>
/// Account Request DTO 擴充方法
/// </summary>
public static class AccountRequestExtensions
{
    /// <summary>
    /// 將 CreateAccountRequest 轉換為 User Entity
    /// </summary>
    public static User ToEntity(this CreateAccountRequest request, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.ToLowerInvariant(), // 不區分大小寫
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            Version = 1
        };
    }
}
```

**注意**: 
- 密碼雜湊在 Service 層完成,不在 DTO 轉換時處理
- 系統欄位 (Id, CreatedAt, Version) 由系統自動設定

---

### 6.3 View DTO 映射 (未來擴充)

**用途**: 當有 Join 查詢需求時,Repository 直接返回 View DTO。

**範例** (未來角色權限功能):
```csharp
// Repository 層: 執行 Join 查詢並返回 View DTO
public async Task<IEnumerable<UserRoleView>> GetUserRolesAsync(Guid userId)
{
    const string sql = @"
        SELECT 
            u.id AS UserId,
            u.username AS Username,
            u.display_name AS DisplayName,
            r.id AS RoleId,
            r.name AS RoleName,
            ur.assigned_at AS AssignedAt
        FROM users u
        INNER JOIN user_roles ur ON u.id = ur.user_id
        INNER JOIN roles r ON ur.role_id = r.id
        WHERE u.id = @UserId AND u.is_deleted = false
    ";
    
    return await _connection.QueryAsync<UserRoleView>(sql, new { UserId = userId });
}

// Service 層: 直接使用 View DTO,無需額外轉換
public async Task<IEnumerable<UserRoleView>> GetUserRolesAsync(Guid userId)
{
    return await _userRepository.GetUserRolesAsync(userId);
}
```

**命名規則總結**:
- **Entity**: 單一表格映射,用於 Repository ↔ Database
- **Request**: API 輸入,用於 Controller → Service
- **Response**: API 輸出,用於 Service → Controller
- **View**: Join 查詢結果,用於 Repository → Service (多表關聯時)

---

## 7. 資料庫遷移腳本

### 7.1 001_CreateUsersTable.sql

```sql
-- 建立使用者資料表
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(20) NOT NULL,
    password_hash VARCHAR(60) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1
);

-- 建立索引
CREATE UNIQUE INDEX idx_users_username ON users(username) WHERE is_deleted = false;
CREATE INDEX idx_users_isdeleted ON users(is_deleted);
CREATE INDEX idx_users_createdat ON users(created_at DESC);

-- 建立註解
COMMENT ON TABLE users IS '使用者資料表';
COMMENT ON COLUMN users.id IS '使用者唯一識別碼';
COMMENT ON COLUMN users.username IS '帳號名稱 (唯一,用於登入)';
COMMENT ON COLUMN users.password_hash IS '密碼雜湊 (BCrypt)';
COMMENT ON COLUMN users.display_name IS '顯示名稱';
COMMENT ON COLUMN users.created_at IS '建立時間 (UTC)';
COMMENT ON COLUMN users.updated_at IS '最後更新時間 (UTC)';
COMMENT ON COLUMN users.is_deleted IS '是否已刪除 (軟刪除標記)';
COMMENT ON COLUMN users.deleted_at IS '刪除時間 (UTC)';
COMMENT ON COLUMN users.deleted_by IS '刪除操作者 ID';
COMMENT ON COLUMN users.version IS '版本號 (樂觀並發控制)';
```

---

### 7.2 seed.sql

```sql
-- 插入預設管理員帳號 (密碼: Admin@123)
INSERT INTO users (id, username, password_hash, display_name, created_at, is_deleted, version)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'admin',
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIpYBFeXiu', -- Admin@123
    '系統管理員',
    CURRENT_TIMESTAMP,
    false,
    1
);

-- 插入測試帳號 (密碼: Test@123)
INSERT INTO users (id, username, password_hash, display_name, created_at, is_deleted, version)
VALUES (
    '00000000-0000-0000-0000-000000000002',
    'testuser',
    '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', -- Test@123
    '測試使用者',
    CURRENT_TIMESTAMP,
    false,
    1
);
```

---

## 8. 資料完整性約束

### 8.1 約束清單

| 約束類型 | 欄位 | 規則 |
|---------|------|------|
| PRIMARY KEY | id | 主鍵唯一識別 |
| UNIQUE | username | 帳號名稱唯一 (WHERE is_deleted = false) |
| NOT NULL | id, username, password_hash, display_name, created_at, is_deleted, version | 必填欄位 |
| CHECK | username | LENGTH(username) BETWEEN 3 AND 20 |
| CHECK | display_name | LENGTH(display_name) <= 100 |
| CHECK | version | version >= 1 |
| FOREIGN KEY | deleted_by | 未來可關聯至 users(id) |

---

### 8.2 約束實作

```sql
-- 新增 CHECK 約束 (可選,應用層已驗證)
ALTER TABLE users ADD CONSTRAINT chk_username_length 
    CHECK (LENGTH(username) BETWEEN 3 AND 20);

ALTER TABLE users ADD CONSTRAINT chk_displayname_length 
    CHECK (LENGTH(display_name) <= 100);

ALTER TABLE users ADD CONSTRAINT chk_version_positive 
    CHECK (version >= 1);

-- 新增 FOREIGN KEY 約束 (未來權限管理擴充時)
-- ALTER TABLE users ADD CONSTRAINT fk_deleted_by 
--     FOREIGN KEY (deleted_by) REFERENCES users(id);
```

---

## 9. 效能考量

### 9.1 索引策略

- **idx_users_username**: 用於登入查詢與帳號唯一性檢查 (高頻查詢)
- **idx_users_isdeleted**: 用於過濾已刪除帳號 (所有查詢)
- **idx_users_createdat**: 用於帳號列表排序 (分頁查詢)

### 9.2 查詢優化

```sql
-- 登入查詢 (使用 idx_users_username)
SELECT id, username, password_hash, display_name, created_at, updated_at, version
FROM users
WHERE username = @username AND is_deleted = false
LIMIT 1;

-- 帳號列表查詢 (使用 idx_users_isdeleted + idx_users_createdat)
SELECT id, username, display_name, created_at, updated_at
FROM users
WHERE is_deleted = false
ORDER BY created_at DESC
LIMIT @pageSize OFFSET @offset;

-- 檢查最後一個帳號 (使用 idx_users_isdeleted)
SELECT COUNT(*) FROM users WHERE is_deleted = false;
```

### 9.3 連接池設定

```csharp
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=v3admin;Username=admin;Password=***;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=300;"
}
```

---

## 10. 安全性考量

### 10.1 敏感資訊保護

- **PasswordHash**: 絕不在 API 回應中回傳
- **DeletedBy**: 內部欄位,不對外暴露
- **Version**: 內部欄位,僅用於並發控制

### 10.2 SQL Injection 防護

- 所有查詢使用參數化查詢 (Dapper 預設支援)
- 不拼接 SQL 字串

### 10.3 資料驗證

- 應用層 (FluentValidation) 與資料庫層 (CHECK 約束) 雙重驗證
- 輸入長度限制防止緩衝區溢位

---

## 總結

本資料模型涵蓋:
- ✅ **1 個核心實體 (Entity)**: User (單一表格映射)
- ✅ **5 個 Request DTOs**: Login, CreateAccount, UpdateAccount, ChangePassword, DeleteAccount (API 輸入)
- ✅ **3 個 Response DTOs**: Login, Account, AccountList (API 輸出)
- ✅ **0 個 View DTOs**: 目前無 Join 查詢需求 (未來角色權限功能時新增)
- ✅ **5 個驗證器**: FluentValidation (輸入驗證)
- ✅ **完整的業務規則**: 生命週期、狀態轉換、並發控制
- ✅ **資料庫遷移腳本**: 建表、索引、初始資料
- ✅ **效能與安全性考量**: 索引策略、參數化查詢、敏感資訊保護

**命名規則遵循**:
- ✅ Entity: 單一表格映射 (User)
- ✅ Request: API 輸入模型 (LoginRequest, CreateAccountRequest 等)
- ✅ Response: API 輸出模型 (LoginResponse, AccountResponse 等)
- ✅ View: Join 查詢結果 (未來擴充時使用)

所有設計符合專案憲法與三層架構原則,可進入實作階段。

**下一步**: 執行 `/speckit.tasks` 指令建立詳細的實作任務清單。
