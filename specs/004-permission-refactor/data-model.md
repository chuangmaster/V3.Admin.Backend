````markdown
# Data Model 資料模型設計：權限模型重構

**Date**: 2025-11-16  
**Branch**: `004-permission-refactor`  
**Status**: Phase 1 - Design

---

## 概述

本文件定義了權限模型重構後的資料實體設計，包括資料庫結構、C# 實體類、DTO 模型及其驗證規則。重點是完全移除 `RoutePath` 欄位，並擴展 `PermissionType` 以支持 `function` 和 `view` 兩種類型。

---

## Core Entities

### 1. Permission 實體

**資料庫表結構**：`permissions`

```sql
CREATE TABLE permissions (
    id SERIAL PRIMARY KEY,
    permission_code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    permission_type INTEGER NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_by INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by INTEGER,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_by INTEGER,
    deleted_at TIMESTAMP,
    version INTEGER DEFAULT 0,
    
    -- Constraints
    CHECK (char_length(permission_code) >= 3 AND char_length(permission_code) <= 100),
    CHECK (char_length(name) >= 1 AND char_length(name) <= 255),
    
    -- Foreign Keys (per Constitution Principle III)
    CONSTRAINT fk_permissions_createdby FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_permissions_updatedby FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_permissions_deletedby FOREIGN KEY (deleted_by) REFERENCES users(id) ON DELETE SET NULL,
    
    -- Indexes
    CREATE INDEX idx_permissions_code ON permissions(permission_code);
    CREATE INDEX idx_permissions_type ON permissions(permission_type);
    CREATE INDEX idx_permissions_deleted ON permissions(is_deleted);
);
```

**C# 實體類**：

```csharp
/// <summary>
/// 權限實體，代表系統中的一個權限定義
/// </summary>
public class Permission
{
    /// <summary>主鍵</summary>
    public int Id { get; set; }

    /// <summary>權限代碼，遵循 resource.action 或 resource.subresource.action 格式</summary>
    public string PermissionCode { get; set; }

    /// <summary>權限名稱，用於 UI 顯示</summary>
    public string Name { get; set; }

    /// <summary>權限描述</summary>
    public string Description { get; set; }

    /// <summary>權限類型：Function (操作) 或 View (區塊瀏覽)</summary>
    public PermissionType PermissionType { get; set; }

    /// <summary>軟刪除標記</summary>
    public bool IsDeleted { get; set; }

    /// <summary>建立者 ID</summary>
    public int? CreatedBy { get; set; }

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最後修改者 ID</summary>
    public int? UpdatedBy { get; set; }

    /// <summary>最後修改時間</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>刪除者 ID</summary>
    public int? DeletedBy { get; set; }

    /// <summary>刪除時間</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>樂觀並發控制版本號</summary>
    public int Version { get; set; }

    /// <summary>建立者用戶（導航屬性）</summary>
    public virtual User CreatedByUser { get; set; }

    /// <summary>更新者用戶（導航屬性）</summary>
    public virtual User UpdatedByUser { get; set; }

    /// <summary>刪除者用戶（導航屬性）</summary>
    public virtual User DeletedByUser { get; set; }

    /// <summary>角色-權限關聯（導航屬性）</summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
```

---

### 2. PermissionType 列舉

```csharp
/// <summary>
/// 權限類型列舉
/// 
/// 注意：架構設計預留了擴展機制，可在將來升級為資料庫表 (permission_types)
/// 以支持動態新增類型（如 Report, Api 等）
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// 功能操作權限，代表用戶可以執行的動作
    /// 範例：permission.create, role.update, account.delete, inventory.export
    /// 用於控制操作按鈕的顯示和功能呼叫的授權
    /// </summary>
    Function = 1,

    /// <summary>
    /// UI 區塊瀏覽權限，代表用戶可以查看的 UI 元件或頁面區塊
    /// 範例：dashboard.summary_widget, reports.analytics_panel, settings.advanced_options
    /// 用於控制前端 UI 元件的顯示/隱藏
    /// </summary>
    View = 2

    // 未來擴展點（遷移到資料庫表後）：
    // Report = 3,     // 報表存取權限
    // Api = 4,        // API 存取權限
    // Module = 5      // 模組功能權限
}
```

---

### 3. Role 實體（無變動）

```csharp
/// <summary>
/// 角色實體，代表權限的集合
/// 本次重構不涉及 Role 實體的修改
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string RoleCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsDeleted { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int Version { get; set; }

    public virtual User CreatedByUser { get; set; }
    public virtual User UpdatedByUser { get; set; }
    public virtual User DeletedByUser { get; set; }
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```

---

### 4. RolePermission 實體（無直接變動）

```csharp
/// <summary>
/// 角色-權限關聯實體
/// 本次重構不涉及此實體的結構修改，但查詢邏輯需適配新的 Permission 模型
/// </summary>
public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsDeleted { get; set; }

    public virtual Role Role { get; set; }
    public virtual Permission Permission { get; set; }
    public virtual User AssignedByUser { get; set; }
}
```

---

### 5. UserRole 實體（無變動）

```csharp
/// <summary>
/// 用戶-角色關聯實體
/// 本次重構不涉及修改
/// </summary>
public class UserRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }

    public virtual User User { get; set; }
    public virtual Role Role { get; set; }
    public virtual User AssignedByUser { get; set; }
    public virtual User DeletedByUser { get; set; }
}
```

---

### 6. PermissionFailureLog 實體（欄位調整）

```csharp
/// <summary>
/// 權限檢查失敗日誌
/// 記錄用戶嘗試訪問無權限資源的事件
/// 本次重構調整：移除 route_path 欄位，新增 request_path
/// </summary>
public class PermissionFailureLog
{
    /// <summary>主鍵</summary>
    public int Id { get; set; }

    /// <summary>用戶 ID</summary>
    public int UserId { get; set; }

    /// <summary>被拒絕的權限代碼</summary>
    public string PermissionCode { get; set; }

    /// <summary>實際請求路徑（如 /api/permissions/create）</summary>
    public string RequestPath { get; set; }

    /// <summary>失敗原因描述</summary>
    public string Reason { get; set; }

    /// <summary>記錄時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>用戶（導航屬性）</summary>
    public virtual User User { get; set; }
}
```

**資料庫表結構**：

```sql
CREATE TABLE permission_failure_logs (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    permission_code VARCHAR(100) NOT NULL,
    request_path VARCHAR(500),
    reason TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Key
    CONSTRAINT fk_permlogs_userid FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    
    -- Indexes
    CREATE INDEX idx_permlogs_userid ON permission_failure_logs(user_id);
    CREATE INDEX idx_permlogs_code ON permission_failure_logs(permission_code);
    CREATE INDEX idx_permlogs_createdat ON permission_failure_logs(created_at);
);
```

---

## DTO 模型

### 1. 建立權限請求 DTO

```csharp
/// <summary>
/// 建立權限的請求 DTO
/// 注意：已移除 RoutePath 欄位
/// </summary>
public class CreatePermissionRequest
{
    /// <summary>權限代碼（必填），遵循 resource.action 格式</summary>
    public string PermissionCode { get; set; }

    /// <summary>權限名稱（必填）</summary>
    public string Name { get; set; }

    /// <summary>權限描述</summary>
    public string Description { get; set; }

    /// <summary>
    /// 權限類型（必填）
    /// 允許值："function" (操作權限) 或 "view" (區塊瀏覽權限)
    /// </summary>
    public string PermissionType { get; set; }
}
```

---

### 2. 編輯權限請求 DTO

```csharp
/// <summary>
/// 編輯權限的請求 DTO
/// 注意：已移除 RoutePath 欄位
/// </summary>
public class UpdatePermissionRequest
{
    /// <summary>權限 ID（路由參數）</summary>
    public int Id { get; set; }

    /// <summary>權限名稱</summary>
    public string Name { get; set; }

    /// <summary>權限描述</summary>
    public string Description { get; set; }

    /// <summary>權限類型</summary>
    public string PermissionType { get; set; }

    /// <summary>樂觀並發控制版本號</summary>
    public int Version { get; set; }
}
```

---

### 3. 權限回應 DTO

```csharp
/// <summary>
/// 權限回應 DTO，用於查詢和返回權限資訊
/// </summary>
public class PermissionResponse
{
    /// <summary>權限 ID</summary>
    public int Id { get; set; }

    /// <summary>權限代碼</summary>
    public string PermissionCode { get; set; }

    /// <summary>權限名稱</summary>
    public string Name { get; set; }

    /// <summary>權限描述</summary>
    public string Description { get; set; }

    /// <summary>權限類型（"function" 或 "view"）</summary>
    public string PermissionType { get; set; }

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最後修改時間</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>版本號</summary>
    public int Version { get; set; }
}
```

---

### 4. 批量分配權限請求 DTO

```csharp
/// <summary>
/// 為角色分配一組權限的請求 DTO
/// </summary>
public class AssignPermissionsRequest
{
    /// <summary>角色 ID</summary>
    public int RoleId { get; set; }

    /// <summary>權限 ID 列表</summary>
    public List<int> PermissionIds { get; set; }
}
```

---

### 5. 檢查權限回應 DTO

```csharp
/// <summary>
/// 檢查用戶權限的回應 DTO
/// 供前端查詢用戶是否擁有特定權限
/// </summary>
public class CheckPermissionResponse
{
    /// <summary>權限代碼</summary>
    public string PermissionCode { get; set; }

    /// <summary>權限類型</summary>
    public string PermissionType { get; set; }

    /// <summary>用戶是否擁有此權限</summary>
    public bool HasPermission { get; set; }
}
```

---

## 驗證規則

### 1. CreatePermissionRequest 驗證

```csharp
public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        // PermissionCode 驗證
        RuleFor(x => x.PermissionCode)
            .NotEmpty().WithMessage("權限代碼不可為空")
            .Length(3, 100).WithMessage("權限代碼長度須為 3-100 字元")
            .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$")
            .WithMessage("權限代碼格式不正確，只允許字母、數字、點號、下劃線");

        // Name 驗證
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("權限名稱不可為空")
            .Length(1, 255).WithMessage("權限名稱長度須為 1-255 字元");

        // Description 驗證
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("權限描述最多 500 字元");

        // PermissionType 驗證
        RuleFor(x => x.PermissionType)
            .NotEmpty().WithMessage("權限類型不可為空")
            .Must(x => x == "function" || x == "view")
            .WithMessage("權限類型只能是 'function' 或 'view'");
    }
}
```

### 2. UpdatePermissionRequest 驗證

```csharp
public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        // Id 驗證
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("權限 ID 無效");

        // Name 驗證
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("權限名稱不可為空")
            .Length(1, 255).WithMessage("權限名稱長度須為 1-255 字元");

        // Description 驗證
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("權限描述最多 500 字元");

        // PermissionType 驗證
        RuleFor(x => x.PermissionType)
            .NotEmpty().WithMessage("權限類型不可為空")
            .Must(x => x == "function" || x == "view")
            .WithMessage("權限類型只能是 'function' 或 'view'");

        // Version 驗證
        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(0).WithMessage("版本號無效");
    }
}
```

---

## 欄位對應關係

| C# 屬性 | 資料庫欄位 | 型別 | 備註 |
|--------|----------|------|------|
| Id | id | INT | 主鍵 |
| PermissionCode | permission_code | VARCHAR(100) | 唯一約束 |
| Name | name | VARCHAR(255) | |
| Description | description | TEXT | |
| PermissionType | permission_type | INT | 1=Function, 2=View |
| IsDeleted | is_deleted | BOOLEAN | 軟刪除 |
| CreatedBy | created_by | INT | 外鍵：users.id |
| CreatedAt | created_at | TIMESTAMP | |
| UpdatedBy | updated_by | INT | 外鍵：users.id |
| UpdatedAt | updated_at | TIMESTAMP | |
| DeletedBy | deleted_by | INT | 外鍵：users.id |
| DeletedAt | deleted_at | TIMESTAMP | |
| Version | version | INT | 樂觀鎖 |

---

## 遷移檢查清單

### 資料庫層面
- [ ] 新建遷移文件移除 `route_path` 欄位
- [ ] 確認新的 CHECK 約束正確應用
- [ ] 確認外鍵約束遵循 Constitution Principle III
- [ ] 新增/更新索引以優化查詢性能

### C# 應用層面
- [ ] 從 Permission 實體移除 RoutePath 屬性
- [ ] 更新 PermissionRepository 查詢邏輯
- [ ] 更新 PermissionService 商業邏輯
- [ ] 移除所有 DTO 中的 RoutePath 欄位
- [ ] 更新驗證器，移除 RoutePath 驗證
- [ ] 更新 PermissionAuthorizationMiddleware
- [ ] 更新 PermissionValidationService

### 測試層面
- [ ] 單元測試：Permission 實體和驗證
- [ ] 單元測試：PermissionService 權限檢查
- [ ] 整合測試：Permission API 端點（建立、編輯、刪除）
- [ ] 整合測試：權限驗證中介軟體
- [ ] 端對端測試：前端查詢權限並渲染 UI

---

## 後續擴展

### 可能的 PermissionType 未來型別

當系統成熟並需要更多權限控制粒度時，考慮擴展 PermissionType：

| 型別 | 用途 | 範例 |
|------|------|------|
| Function | ✅ 現有 | permission.create |
| View | ✅ 現有 | dashboard.widget |
| Report | 🔄 規劃中 | sales_report.view |
| Api | 🔄 規劃中 | api.export_data |
| Module | 🔄 規劃中 | admin_panel.access |

### 資料庫表遷移準備

當需要動態類型時，建立 `permission_types` 表並進行遷移：

```sql
CREATE TABLE permission_types (
    id SERIAL PRIMARY KEY,
    type_code VARCHAR(50) UNIQUE NOT NULL,
    type_name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 初始資料
INSERT INTO permission_types (type_code, type_name, description) VALUES
('function', '功能操作權限', '代表用戶可以執行的動作'),
('view', 'UI 區塊瀏覽權限', '代表用戶可以查看的 UI 元件或頁面區塊');
```

修改 Permission 表：

```sql
ALTER TABLE permissions
    ALTER COLUMN permission_type DROP DEFAULT,
    ADD COLUMN permission_type_id INTEGER REFERENCES permission_types(id),
    ADD CONSTRAINT fk_permissions_type FOREIGN KEY (permission_type_id) REFERENCES permission_types(id);
```

````
