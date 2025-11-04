# 資料模型設計：權限管理機制

**Feature**: 002-permission-management  
**Date**: 2025-11-05  
**Purpose**: 定義權限管理機制所需的所有資料實體、關係、驗證規則和狀態轉換

---

## 實體概覽

本功能引入以下核心實體：

1. **Permission（權限）**: 系統中定義的訪問或操作授權
2. **Role（角色）**: 權限的集合，用於簡化用戶權限管理
3. **RolePermission（角色權限關聯）**: 連接角色與權限的多對多關係
4. **UserRole（用戶角色關聯）**: 連接用戶與角色的多對多關係
5. **AuditLog（稽核日誌）**: 記錄權限管理相關操作的歷史記錄
6. **PermissionFailureLog（權限驗證失敗記錄）**: 記錄所有權限驗證失敗的嘗試

---

## 1. Permission（權限）

### 描述
權限是系統中定義的訪問或操作授權。支援兩種類型：路由權限（控制頁面訪問）和功能權限（控制操作權限如新增、修改、刪除）。

### 資料表結構（PostgreSQL）

```sql
CREATE TABLE permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_code VARCHAR(100) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    permission_type VARCHAR(20) NOT NULL, -- 'route' 或 'function'
    route_path VARCHAR(500),              -- 僅路由權限使用（如 '/inventory', '/users/profile'）
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,                      -- 建立者 ID（可關聯至 users.id）
    updated_by UUID,                      -- 最後更新者 ID
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1,
    
    -- 約束
    CONSTRAINT chk_permission_type CHECK (permission_type IN ('route', 'function')),
    CONSTRAINT chk_permission_code_format CHECK (
        (permission_type = 'function' AND permission_code ~ '^[a-z]+\.[a-z]+$') OR
        (permission_type = 'route')
    ),
    CONSTRAINT chk_route_path_required CHECK (
        (permission_type = 'route' AND route_path IS NOT NULL) OR
        (permission_type = 'function' AND route_path IS NULL)
    ),
    CONSTRAINT chk_version_positive CHECK (version >= 1)
);

-- 索引
CREATE UNIQUE INDEX idx_permissions_code ON permissions(permission_code) WHERE is_deleted = false;
CREATE INDEX idx_permissions_type ON permissions(permission_type);
CREATE INDEX idx_permissions_isdeleted ON permissions(is_deleted);
CREATE INDEX idx_permissions_createdat ON permissions(created_at DESC);

-- 註解
COMMENT ON TABLE permissions IS '權限資料表';
COMMENT ON COLUMN permissions.permission_code IS '權限代碼（唯一，功能權限格式為 resource.action，如 inventory.create）';
COMMENT ON COLUMN permissions.permission_type IS '權限類型（route: 路由權限, function: 功能權限）';
COMMENT ON COLUMN permissions.route_path IS '路由路徑（僅路由權限使用，如 /inventory）';
```

### C# 實體

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 權限實體
/// </summary>
/// <remarks>
/// 對應資料庫 permissions 資料表，定義系統中的訪問或操作授權
/// </remarks>
public class Permission
{
    /// <summary>
    /// 權限唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 權限代碼（唯一）
    /// </summary>
    /// <remarks>
    /// 功能權限格式: resource.action（如 inventory.create, users.delete）
    /// 路由權限格式: 自訂字串（如 dashboard_access, inventory_page_access）
    /// </remarks>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 權限名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 權限描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 權限類型（route: 路由權限, function: 功能權限）
    /// </summary>
    public string PermissionType { get; set; } = string.Empty;

    /// <summary>
    /// 路由路徑（僅路由權限使用）
    /// </summary>
    /// <remarks>
    /// 如 /inventory, /users/profile
    /// </remarks>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 建立者 ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 最後更新者 ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// 是否已刪除（軟刪除標記）
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
    /// 版本號（樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
```

### 驗證規則

- `permission_code`: 必填，最大 100 字元，不可重複（排除已刪除），功能權限必須符合 `^[a-z]+\.[a-z]+$` 格式
- `name`: 必填，最大 200 字元
- `description`: 選填，最大 TEXT
- `permission_type`: 必填，僅允許 'route' 或 'function'
- `route_path`: 當 permission_type = 'route' 時必填，最大 500 字元
- `version`: 必須 >= 1

### 業務規則

- 建立權限時，`permission_code` 必須唯一（不考慮已刪除的權限）
- 刪除權限時，必須檢查是否被任何角色使用（role_permissions 表），若使用中則拒絕刪除（FR-017）
- 更新權限時，使用樂觀並發控制（version 欄位）防止衝突
- 軟刪除：設定 `is_deleted = true`, `deleted_at = CURRENT_TIMESTAMP`, `deleted_by = 操作者ID`

---

## 2. Role（角色）

### 描述
角色是權限的集合，用於簡化用戶權限管理。一個角色可以擁有多個權限（路由權限 + 功能權限）。

### 資料表結構（PostgreSQL）

```sql
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1,
    
    -- 約束
    CONSTRAINT chk_role_name_length CHECK (LENGTH(role_name) BETWEEN 1 AND 100),
    CONSTRAINT chk_version_positive CHECK (version >= 1)
);

-- 索引
CREATE UNIQUE INDEX idx_roles_name ON roles(role_name) WHERE is_deleted = false;
CREATE INDEX idx_roles_isdeleted ON roles(is_deleted);
CREATE INDEX idx_roles_createdat ON roles(created_at DESC);

-- 註解
COMMENT ON TABLE roles IS '角色資料表';
COMMENT ON COLUMN roles.role_name IS '角色名稱（唯一，1-100 字元）';
```

### C# 實體

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 角色實體
/// </summary>
/// <remarks>
/// 對應資料庫 roles 資料表，代表權限的集合
/// </remarks>
public class Role
{
    /// <summary>
    /// 角色唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色名稱（唯一）
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 建立者 ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 最後更新者 ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// 是否已刪除（軟刪除標記）
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
    /// 版本號（樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
```

### 驗證規則

- `role_name`: 必填，1-100 字元，不可重複（排除已刪除）
- `description`: 選填，最大 TEXT
- `version`: 必須 >= 1

### 業務規則

- 建立角色時，`role_name` 必須唯一（不考慮已刪除的角色）
- 刪除角色時，必須檢查是否被任何用戶使用（user_roles 表），若使用中則拒絕刪除（FR-018）
- 更新角色時，使用樂觀並發控制（version 欄位）防止衝突
- 軟刪除：設定 `is_deleted = true`, `deleted_at = CURRENT_TIMESTAMP`, `deleted_by = 操作者ID`

---

## 3. RolePermission（角色權限關聯）

### 描述
連接角色與權限的多對多關係。一個角色可以擁有多個權限，一個權限可以被多個角色使用。

### 資料表結構（PostgreSQL）

```sql
CREATE TABLE role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL,
    permission_id UUID NOT NULL,
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID,                     -- 分配者 ID
    
    -- 外鍵約束
    CONSTRAINT fk_role_permissions_role_id FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permissions_permission_id FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE,
    
    -- 唯一約束（避免重複分配）
    CONSTRAINT uq_role_permission UNIQUE (role_id, permission_id)
);

-- 索引
CREATE INDEX idx_role_permissions_role_id ON role_permissions(role_id);
CREATE INDEX idx_role_permissions_permission_id ON role_permissions(permission_id);
CREATE INDEX idx_role_permissions_assigned_at ON role_permissions(assigned_at DESC);

-- 註解
COMMENT ON TABLE role_permissions IS '角色權限關聯資料表';
COMMENT ON COLUMN role_permissions.role_id IS '角色 ID（外鍵關聯 roles.id）';
COMMENT ON COLUMN role_permissions.permission_id IS '權限 ID（外鍵關聯 permissions.id）';
COMMENT ON COLUMN role_permissions.assigned_by IS '分配者 ID（關聯 users.id）';
```

### C# 實體

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 角色權限關聯實體
/// </summary>
/// <remarks>
/// 對應資料庫 role_permissions 資料表，連接角色與權限的多對多關係
/// </remarks>
public class RolePermission
{
    /// <summary>
    /// 關聯唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 權限 ID
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// 分配時間 (UTC)
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// 分配者 ID
    /// </summary>
    public Guid? AssignedBy { get; set; }
}
```

### 驗證規則

- `role_id`: 必填，必須存在於 roles 表中
- `permission_id`: 必填，必須存在於 permissions 表中
- `(role_id, permission_id)`: 組合必須唯一（避免重複分配）

### 業務規則

- 分配權限時，檢查 role 和 permission 是否存在且未被軟刪除
- 移除權限時，直接 DELETE 記錄（非軟刪除）
- 查詢角色權限時，JOIN permissions 表並過濾 `is_deleted = false`

---

## 4. UserRole（用戶角色關聯）

### 描述
連接用戶與角色的多對多關係。一個用戶可以擁有多個角色，一個角色可以被多個用戶使用。

### 資料表結構（PostgreSQL）

```sql
CREATE TABLE user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID,                     -- 指派者 ID
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    
    -- 外鍵約束
    CONSTRAINT fk_user_roles_user_id FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role_id FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    
    -- 唯一約束（避免重複指派，排除已刪除）
    CONSTRAINT uq_user_role UNIQUE (user_id, role_id)
);

-- 索引
CREATE INDEX idx_user_roles_user_id_isdeleted ON user_roles(user_id, is_deleted);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);
CREATE INDEX idx_user_roles_assigned_at ON user_roles(assigned_at DESC);

-- 註解
COMMENT ON TABLE user_roles IS '用戶角色關聯資料表';
COMMENT ON COLUMN user_roles.user_id IS '用戶 ID（外鍵關聯 users.id）';
COMMENT ON COLUMN user_roles.role_id IS '角色 ID（外鍵關聯 roles.id）';
COMMENT ON COLUMN user_roles.assigned_by IS '指派者 ID（關聯 users.id）';
```

### C# 實體

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 用戶角色關聯實體
/// </summary>
/// <remarks>
/// 對應資料庫 user_roles 資料表，連接用戶與角色的多對多關係
/// </remarks>
public class UserRole
{
    /// <summary>
    /// 關聯唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用戶 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 指派時間 (UTC)
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// 指派者 ID
    /// </summary>
    public Guid? AssignedBy { get; set; }

    /// <summary>
    /// 是否已刪除（軟刪除標記）
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
}
```

### 驗證規則

- `user_id`: 必填，必須存在於 users 表中
- `role_id`: 必填，必須存在於 roles 表中
- `(user_id, role_id)`: 組合必須唯一（避免重複指派）

### 業務規則

- 指派角色時，檢查 user 和 role 是否存在且未被軟刪除
- 移除角色時，使用軟刪除（設定 `is_deleted = true`, `deleted_at`, `deleted_by`）
- 查詢用戶角色時，過濾 `is_deleted = false`

---

## 5. AuditLog（稽核日誌）

### 描述
記錄權限管理相關操作的歷史記錄，包括權限、角色、用戶角色的新增、修改、刪除。稽核日誌僅可新增和查詢，不可修改或刪除，永久保留。

### 資料表結構（PostgreSQL）

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id UUID NOT NULL,            -- 操作者 ID
    operator_name VARCHAR(100) NOT NULL,  -- 操作者名稱（快照，避免 JOIN）
    operation_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    operation_type VARCHAR(50) NOT NULL,  -- 如 '新增權限', '修改角色', '指派角色'
    target_type VARCHAR(50) NOT NULL,     -- 如 'Permission', 'Role', 'UserRole'
    target_id UUID NOT NULL,              -- 目標對象 ID
    before_state JSONB,                   -- 變更前狀態（JSON 格式）
    after_state JSONB,                    -- 變更後狀態（JSON 格式）
    ip_address VARCHAR(45),               -- 操作者 IP 位址（支援 IPv6）
    user_agent TEXT,                      -- 操作者 UserAgent
    trace_id VARCHAR(50)                  -- TraceId（關聯分散式追蹤）
);

-- 索引
CREATE INDEX idx_audit_logs_operation_time_desc ON audit_logs(operation_time DESC);
CREATE INDEX idx_audit_logs_operator_id ON audit_logs(operator_id);
CREATE INDEX idx_audit_logs_operation_type ON audit_logs(operation_type);
CREATE INDEX idx_audit_logs_target_type ON audit_logs(target_type);
CREATE INDEX idx_audit_logs_operator_time ON audit_logs(operator_id, operation_time DESC);

-- 註解
COMMENT ON TABLE audit_logs IS '稽核日誌資料表（僅可新增和查詢，不可修改或刪除）';
COMMENT ON COLUMN audit_logs.before_state IS '變更前狀態（JSON 格式，新增操作為 null）';
COMMENT ON COLUMN audit_logs.after_state IS '變更後狀態（JSON 格式，刪除操作後為 null）';
```

### C# 實體

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 稽核日誌實體
/// </summary>
/// <remarks>
/// 對應資料庫 audit_logs 資料表，記錄權限管理相關操作的歷史記錄
/// </remarks>
public class AuditLog
{
    /// <summary>
    /// 日誌唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 操作者 ID
    /// </summary>
    public Guid OperatorId { get; set; }

    /// <summary>
    /// 操作者名稱（快照）
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 操作時間 (UTC)
    /// </summary>
    public DateTime OperationTime { get; set; }

    /// <summary>
    /// 操作類型（如 '新增權限', '修改角色', '指派角色'）
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 目標對象類型（如 'Permission', 'Role', 'UserRole'）
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 目標對象 ID
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// 變更前狀態（JSON 字串）
    /// </summary>
    public string? BeforeState { get; set; }

    /// <summary>
    /// 變更後狀態（JSON 字串）
    /// </summary>
    public string? AfterState { get; set; }

    /// <summary>
    /// 操作者 IP 位址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 操作者 UserAgent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// TraceId（分散式追蹤）
    /// </summary>
    public string? TraceId { get; set; }
}
```

### 驗證規則

- `operator_id`: 必填
- `operator_name`: 必填，最大 100 字元
- `operation_type`: 必填，最大 50 字元
- `target_type`: 必填，最大 50 字元
- `target_id`: 必填
- `ip_address`: 選填，最大 45 字元（支援 IPv6）
- `user_agent`: 選填，最大 TEXT

### 業務規則

- 稽核日誌僅可 INSERT，不允許 UPDATE 或 DELETE（FR-024）
- 每次權限管理操作必須在同一 Transaction 中寫入稽核日誌（FR-029）
- 若稽核日誌寫入失敗，必須回滾整個業務操作
- 查詢時支援分頁和多條件篩選（操作者、時間範圍、操作類型）

---

## 6. PermissionFailureLog（權限驗證失敗記錄）

### 描述
記錄所有權限驗證失敗的嘗試，用於安全監控和問題排查。

### 資料表結構（PostgreSQL）

```sql
CREATE TABLE permission_failure_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    username VARCHAR(100) NOT NULL,       -- 用戶名稱（快照）
    attempted_resource VARCHAR(500) NOT NULL, -- 嘗試訪問的資源（權限代碼或路由）
    failure_reason VARCHAR(200) NOT NULL, -- 失敗原因
    attempted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address VARCHAR(45),
    user_agent TEXT,
    trace_id VARCHAR(50)
);

-- 索引
CREATE INDEX idx_permission_failure_logs_user_id ON permission_failure_logs(user_id);
CREATE INDEX idx_permission_failure_logs_attempted_at_desc ON permission_failure_logs(attempted_at DESC);
CREATE INDEX idx_permission_failure_logs_user_time ON permission_failure_logs(user_id, attempted_at DESC);

-- 註解
COMMENT ON TABLE permission_failure_logs IS '權限驗證失敗記錄資料表';
COMMENT ON COLUMN permission_failure_logs.attempted_resource IS '嘗試訪問的資源（權限代碼或路由路徑）';
COMMENT ON COLUMN permission_failure_logs.failure_reason IS '失敗原因（如 "權限不足", "用戶無任何角色"）';
```

### C# 實體

```csharp
namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 權限驗證失敗記錄實體
/// </summary>
/// <remarks>
/// 對應資料庫 permission_failure_logs 資料表，記錄所有權限驗證失敗的嘗試
/// </remarks>
public class PermissionFailureLog
{
    /// <summary>
    /// 記錄唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用戶 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用戶名稱（快照）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 嘗試訪問的資源（權限代碼或路由路徑）
    /// </summary>
    public string AttemptedResource { get; set; } = string.Empty;

    /// <summary>
    /// 失敗原因
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// 嘗試時間 (UTC)
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// 操作者 IP 位址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 操作者 UserAgent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// TraceId（分散式追蹤）
    /// </summary>
    public string? TraceId { get; set; }
}
```

### 驗證規則

- `user_id`: 必填
- `username`: 必填，最大 100 字元
- `attempted_resource`: 必填，最大 500 字元
- `failure_reason`: 必填，最大 200 字元

### 業務規則

- 權限驗證失敗時記錄（FR-031）
- 僅可 INSERT，不允許 UPDATE 或 DELETE
- 查詢時支援分頁和多條件篩選（用戶、時間範圍、資源類型）

---

## 實體關係圖（ER Diagram）

```
┌─────────────┐         ┌──────────────────┐         ┌────────────┐
│   Users     │         │   UserRoles      │         │   Roles    │
│  (已存在)    │◄────────┤  (多對多關聯)     ├────────►│  (新增)    │
│             │         │                  │         │            │
│ - id        │ 1     * │ - user_id (FK)   │ *     1 │ - id       │
│ - username  │         │ - role_id (FK)   │         │ - role_name│
│ - ...       │         │ - is_deleted     │         │ - version  │
└─────────────┘         │ - assigned_by    │         └────────────┘
                        └──────────────────┘                │
                                                             │ 1
                                                             │
                                                             │ *
                        ┌──────────────────┐         ┌────────────────┐
                        │ RolePermissions  │         │  Permissions   │
                        │  (多對多關聯)     ├────────►│   (新增)       │
                        │                  │ *     1 │                │
                        │ - role_id (FK)   │         │ - id           │
                        │ - permission_id  │         │ - permission_  │
                        │   (FK)           │         │   code         │
                        │ - assigned_by    │         │ - permission_  │
                        └──────────────────┘         │   type         │
                                                     │ - route_path   │
                                                     │ - version      │
                                                     └────────────────┘

┌──────────────────┐                        ┌───────────────────────┐
│   AuditLogs      │                        │ PermissionFailureLogs │
│   (新增)         │                        │      (新增)           │
│                  │                        │                       │
│ - operator_id    │                        │ - user_id             │
│ - operation_type │                        │ - attempted_resource  │
│ - target_type    │                        │ - failure_reason      │
│ - target_id      │                        │ - attempted_at        │
│ - before_state   │                        │ - ip_address          │
│ - after_state    │                        │ - user_agent          │
│ - ip_address     │                        └───────────────────────┘
│ - user_agent     │
└──────────────────┘
```

---

## 狀態轉換

### Permission 狀態轉換
```
[建立] → [有效] → [軟刪除]
         ↕ (更新 name, description)
```

### Role 狀態轉換
```
[建立] → [有效] → [分配權限] → [軟刪除]
         ↕ (更新 name, description)
         ↕ (新增/移除權限)
```

### UserRole 狀態轉換
```
[指派] → [有效] → [軟刪除]
```

### AuditLog & PermissionFailureLog 狀態
```
[建立] → [永久保留]（不可修改或刪除）
```

---

## 查詢範例

### 查詢用戶的所有有效權限（多角色合併）

```sql
SELECT DISTINCT 
    p.id, 
    p.permission_code, 
    p.permission_type, 
    p.name, 
    p.description,
    p.route_path
FROM permissions p
INNER JOIN role_permissions rp ON p.id = rp.permission_id
INNER JOIN user_roles ur ON rp.role_id = ur.role_id
WHERE ur.user_id = @userId 
  AND ur.is_deleted = false 
  AND p.is_deleted = false
ORDER BY p.permission_type, p.permission_code;
```

### 驗證用戶是否擁有特定權限

```sql
SELECT EXISTS (
    SELECT 1
    FROM permissions p
    INNER JOIN role_permissions rp ON p.id = rp.permission_id
    INNER JOIN user_roles ur ON rp.role_id = ur.role_id
    WHERE ur.user_id = @userId 
      AND ur.is_deleted = false 
      AND p.is_deleted = false
      AND p.permission_code = @permissionCode
) AS has_permission;
```

### 查詢角色的所有權限

```sql
SELECT 
    p.id, 
    p.permission_code, 
    p.permission_type, 
    p.name
FROM permissions p
INNER JOIN role_permissions rp ON p.id = rp.permission_id
WHERE rp.role_id = @roleId 
  AND p.is_deleted = false
ORDER BY p.permission_type, p.permission_code;
```

### 檢查權限是否被任何角色使用

```sql
SELECT EXISTS (
    SELECT 1
    FROM role_permissions rp
    INNER JOIN roles r ON rp.role_id = r.id
    WHERE rp.permission_id = @permissionId 
      AND r.is_deleted = false
) AS is_in_use;
```

### 檢查角色是否被任何用戶使用

```sql
SELECT EXISTS (
    SELECT 1
    FROM user_roles ur
    WHERE ur.role_id = @roleId 
      AND ur.is_deleted = false
) AS is_in_use;
```

### 查詢稽核日誌（分頁 + 篩選）

```sql
SELECT 
    id,
    operator_id,
    operator_name,
    operation_time,
    operation_type,
    target_type,
    target_id,
    before_state,
    after_state,
    ip_address
FROM audit_logs
WHERE operation_time BETWEEN @startTime AND @endTime
  AND (@operatorId IS NULL OR operator_id = @operatorId)
  AND (@operationType IS NULL OR operation_type = @operationType)
ORDER BY operation_time DESC
LIMIT @pageSize OFFSET @offset;
```

---

## 總結

本資料模型設計涵蓋了權限管理機制所需的所有核心實體，包括：

1. **Permission**: 定義系統中的訪問或操作授權（路由權限 + 功能權限）
2. **Role**: 權限的集合，簡化用戶權限管理
3. **RolePermission**: 角色與權限的多對多關係
4. **UserRole**: 用戶與角色的多對多關係
5. **AuditLog**: 稽核日誌（永久保留，不可修改）
6. **PermissionFailureLog**: 權限驗證失敗記錄（安全監控）

所有實體均採用 PostgreSQL snake_case 命名規範（資料庫層）和 C# PascalCase 命名規範（應用層），並提供完整的驗證規則、業務規則和查詢範例。設計符合 constitution 要求的三層式架構、軟刪除機制和樂觀並發控制。
