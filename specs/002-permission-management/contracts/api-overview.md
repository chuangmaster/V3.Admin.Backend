# API 契約概要

**Feature**: 002-permission-management  
**Date**: 2025-11-05  
**Purpose**: 權限管理機制 API 端點總覽

---

## API 端點總覽

本功能提供以下 API 端點群組：

### 1. 權限管理 API (Permissions)
- `GET /api/permissions` - 查詢權限列表（分頁 + 篩選）
- `POST /api/permissions` - 建立權限
- `GET /api/permissions/{id}` - 查詢單一權限
- `PUT /api/permissions/{id}` - 更新權限
- `DELETE /api/permissions/{id}` - 刪除權限（軟刪除）

### 2. 角色管理 API (Roles)
- `GET /api/roles` - 查詢角色列表（分頁 + 篩選）
- `POST /api/roles` - 建立角色
- `GET /api/roles/{id}` - 查詢單一角色
- `GET /api/roles/{id}/permissions` - 查詢角色的所有權限
- `PUT /api/roles/{id}` - 更新角色
- `DELETE /api/roles/{id}` - 刪除角色（軟刪除）

### 3. 角色權限分配 API (Role Permissions)
- `POST /api/roles/{roleId}/permissions` - 為角色分配權限（批次）
- `DELETE /api/roles/{roleId}/permissions/{permissionId}` - 從角色移除權限

### 4. 用戶角色指派 API (User Roles)
- `GET /api/users/{userId}/roles` - 查詢用戶的所有角色
- `POST /api/users/{userId}/roles` - 為用戶指派角色
- `DELETE /api/users/{userId}/roles/{roleId}` - 從用戶移除角色
- `GET /api/users/{userId}/permissions` - 查詢用戶的有效權限（多角色合併）

### 5. 權限驗證 API (Permission Validation)
- `POST /api/permissions/validate` - 驗證用戶是否擁有指定權限
- `POST /api/permissions/validate-batch` - 批次驗證多個權限

### 6. 稽核日誌 API (Audit Logs)
- `GET /api/audit-logs` - 查詢稽核日誌（分頁 + 篩選）
- `GET /api/audit-logs/{id}` - 查詢單一稽核日誌詳情

### 7. 權限驗證失敗記錄 API (Permission Failure Logs)
- `GET /api/permission-failure-logs` - 查詢權限驗證失敗記錄（分頁 + 篩選）

---

## 通用回應結構

所有 API 端點使用統一的 `ApiResponseModel<T>` 回應格式：

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "操作成功",
  "data": { ... },
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123-def456-ghi789"
}
```

### 欄位說明
- `success`: 操作是否成功（boolean）
- `code`: 業務邏輯碼（string），如 SUCCESS, VALIDATION_ERROR, PERMISSION_NOT_FOUND
- `message`: 訊息（繁體中文）
- `data`: 回應數據（可為 null）
- `timestamp`: 回應時間戳（ISO 8601 格式）
- `traceId`: 分散式追蹤 ID

---

## 業務邏輯碼對照表

### 成功碼
- `SUCCESS`: 操作成功
- `CREATED`: 資源建立成功
- `UPDATED`: 資源更新成功
- `DELETED`: 資源刪除成功

### 客戶端錯誤碼（4xx）
- `VALIDATION_ERROR`: 輸入驗證失敗（400）
- `UNAUTHORIZED`: 未授權，JWT token 缺失或無效（401）
- `FORBIDDEN`: 無權限執行此操作（403）
- `PERMISSION_NOT_FOUND`: 權限不存在（404）
- `ROLE_NOT_FOUND`: 角色不存在（404）
- `USER_NOT_FOUND`: 用戶不存在（404）
- `AUDIT_LOG_NOT_FOUND`: 稽核日誌不存在（404）
- `CONCURRENT_UPDATE_CONFLICT`: 併發更新衝突，版本號不匹配（409）
- `DUPLICATE_PERMISSION_CODE`: 權限代碼已存在（422）
- `DUPLICATE_ROLE_NAME`: 角色名稱已存在（422）
- `PERMISSION_IN_USE`: 權限正被角色使用，無法刪除（422）
- `ROLE_IN_USE`: 角色正被用戶使用，無法刪除（422）

### 伺服器錯誤碼（5xx）
- `INTERNAL_ERROR`: 系統內部錯誤（500）

---

## 驗證規則

### 權限代碼格式
- 功能權限: `^[a-z]+\.[a-z]+$`（如 `inventory.create`, `users.delete`）
- 路由權限: 自訂字串（如 `dashboard_access`, `inventory_page_access`）

### 角色名稱
- 長度: 1-100 字元
- 不可重複（排除已刪除）

### 路由路徑
- 格式: `/path` 或 `/path/subpath`
- 最大長度: 500 字元

---

## 分頁參數

所有列表 API 支援分頁參數：

- `pageNumber`: 頁碼（從 1 開始），預設 1
- `pageSize`: 每頁筆數（1-100），預設 20

分頁回應格式：
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": {
    "items": [...],
    "totalCount": 100,
    "pageNumber": 1,
    "pageSize": 20
  },
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123"
}
```

---

## 篩選參數

### 權限列表篩選
- `permissionType`: 權限類型（route 或 function）
- `searchKeyword`: 搜尋關鍵字（搜尋權限代碼或名稱）

### 角色列表篩選
- `searchKeyword`: 搜尋關鍵字（搜尋角色名稱）

### 稽核日誌篩選
- `startTime`: 開始時間（ISO 8601 格式）
- `endTime`: 結束時間（ISO 8601 格式）
- `operatorId`: 操作者 ID（UUID）
- `operationType`: 操作類型（如 "新增權限", "修改角色"）
- `targetType`: 目標對象類型（如 "Permission", "Role"）

### 權限驗證失敗記錄篩選
- `startTime`: 開始時間（ISO 8601 格式）
- `endTime`: 結束時間（ISO 8601 格式）
- `userId`: 用戶 ID（UUID）
- `attemptedResource`: 嘗試訪問的資源

---

## 驗證與授權

### JWT 驗證
所有 API 端點（除了登入）都需要 JWT Bearer token：

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 權限要求
不同 API 端點需要不同的權限：

| API 端點 | 所需權限 |
|---------|---------|
| 權限管理 CRUD | `permissions.create`, `permissions.update`, `permissions.delete`, `permissions.view` |
| 角色管理 CRUD | `roles.create`, `roles.update`, `roles.delete`, `roles.view` |
| 用戶角色指派 | `user_roles.assign`, `user_roles.revoke`, `user_roles.view` |
| 稽核日誌查詢 | `audit_logs.view` |
| 權限驗證（自身） | 無需額外權限（用戶可驗證自己的權限） |

---

## 錯誤處理範例

### 驗證失敗
```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "權限代碼格式錯誤，必須為 resource.action 格式（如 inventory.create）",
  "data": null,
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123"
}
```

### 資源不存在
```json
{
  "success": false,
  "code": "PERMISSION_NOT_FOUND",
  "message": "權限不存在",
  "data": null,
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123"
}
```

### 業務規則違反
```json
{
  "success": false,
  "code": "PERMISSION_IN_USE",
  "message": "該權限正被角色使用，無法刪除",
  "data": null,
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123"
}
```

### 併發衝突
```json
{
  "success": false,
  "code": "CONCURRENT_UPDATE_CONFLICT",
  "message": "資料已被其他使用者更新，請重新載入後再試",
  "data": null,
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123"
}
```

---

## 稽核日誌記錄

以下操作會自動記錄稽核日誌：

### 權限管理
- 新增權限 → `operation_type: "新增權限"`
- 更新權限 → `operation_type: "修改權限"`
- 刪除權限 → `operation_type: "刪除權限"`

### 角色管理
- 新增角色 → `operation_type: "新增角色"`
- 更新角色 → `operation_type: "修改角色"`
- 刪除角色 → `operation_type: "刪除角色"`
- 分配角色權限 → `operation_type: "分配角色權限"`
- 移除角色權限 → `operation_type: "移除角色權限"`

### 用戶角色指派
- 指派用戶角色 → `operation_type: "指派用戶角色"`
- 移除用戶角色 → `operation_type: "移除用戶角色"`

稽核日誌包含以下欄位：
- `operator_id`: 操作者 ID
- `operator_name`: 操作者名稱
- `operation_time`: 操作時間
- `operation_type`: 操作類型
- `target_type`: 目標對象類型（Permission, Role, UserRole）
- `target_id`: 目標對象 ID
- `before_state`: 變更前狀態（JSON）
- `after_state`: 變更後狀態（JSON）
- `ip_address`: 操作者 IP
- `user_agent`: 操作者 UserAgent
- `trace_id`: TraceId

---

## 權限驗證失敗記錄

當用戶權限驗證失敗時，系統會自動記錄以下資訊：
- `user_id`: 用戶 ID
- `username`: 用戶名稱
- `attempted_resource`: 嘗試訪問的資源（權限代碼或路由）
- `failure_reason`: 失敗原因（如 "權限不足", "用戶無任何角色"）
- `attempted_at`: 嘗試時間
- `ip_address`: 用戶 IP
- `user_agent`: 用戶 UserAgent
- `trace_id`: TraceId

---

## 性能要求

| API 類型 | 響應時間目標 |
|---------|-------------|
| 權限驗證（單次） | <100ms |
| 權限/角色 CRUD | <200ms |
| 分頁查詢（列表） | <500ms |
| 稽核日誌查詢 | <2000ms |

---

## 併發控制

權限和角色實體使用樂觀並發控制（Optimistic Locking）：

1. 每次查詢時返回 `version` 欄位
2. 更新時必須提供當前的 `version`
3. 如果 `version` 不匹配，返回 `CONCURRENT_UPDATE_CONFLICT` 錯誤
4. 前端需要重新載入資料並重試

範例：
```json
// 查詢權限
GET /api/permissions/{id}
{
  "data": {
    "id": "...",
    "permissionCode": "inventory.create",
    "version": 1
  }
}

// 更新權限
PUT /api/permissions/{id}
{
  "name": "新增庫存（更新）",
  "version": 1  // 必須提供當前版本號
}

// 如果版本號不匹配
Response: 409 Conflict
{
  "code": "CONCURRENT_UPDATE_CONFLICT",
  "message": "資料已被其他使用者更新，請重新載入後再試"
}
```

---

## 完整 OpenAPI 規格

詳細的 OpenAPI 3.0 規格文件請參閱：
- `permissions-api.yaml` - 權限管理 API
- 其他 API 端點的詳細規格將在實作階段完善

---

## 總結

本 API 契約定義了權限管理機制的所有端點，包括：
- 統一的回應格式（ApiResponseModel）
- 雙層錯誤碼設計（HTTP status + business code）
- 完整的驗證規則和業務規則
- 稽核日誌自動記錄機制
- 樂觀並發控制
- 性能要求和安全考量

所有端點均採用 RESTful 設計原則，支援分頁、篩選和排序，並提供完整的錯誤處理和追蹤機制。
