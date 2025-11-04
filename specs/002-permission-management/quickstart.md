# 快速開始指南：權限管理機制

**Feature**: 002-permission-management  
**Date**: 2025-11-05  
**Purpose**: 提供快速設置和使用權限管理功能的指南

---

## 前置條件

- .NET 9 SDK 已安裝
- PostgreSQL 16+ 資料庫已安裝並運行
- 已完成 001-account-management 功能（用戶管理基礎）
- Git 已安裝（用於版本控制）

---

## 1. 資料庫遷移

### 執行資料庫遷移腳本

依序執行以下 SQL 遷移腳本（位於 `Database/Migrations/` 資料夾）：

```bash
# 連接到 PostgreSQL 資料庫
psql -U your_username -d your_database

# 執行遷移腳本
\i Database/Migrations/002_CreatePermissionsTable.sql
\i Database/Migrations/003_CreateRolesTable.sql
\i Database/Migrations/004_CreateRolePermissionsTable.sql
\i Database/Migrations/005_CreateUserRolesTable.sql
\i Database/Migrations/006_CreateAuditLogsTable.sql
\i Database/Migrations/007_CreatePermissionFailureLogsTable.sql
```

### 執行種子數據腳本（可選）

如果需要初始化一些基礎權限數據：

```bash
\i Database/Scripts/seed_permissions.sql
```

---

## 2. 安裝相關套件

確認 `V3.Admin.Backend.csproj` 包含以下套件（如已存在則跳過）：

```xml
<PackageReference Include="Npgsql" Version="9.0.*" />
<PackageReference Include="Dapper" Version="2.1.*" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
```

如需新增，執行：

```bash
dotnet add package Npgsql --version 9.0.*
dotnet add package Dapper --version 2.1.*
dotnet add package FluentValidation.AspNetCore --version 11.3.*
```

---

## 3. 設定資料庫連線

更新 `appsettings.development.json`（如果尚未設定）：

```json
{
  "DatabaseSettings": {
    "ConnectionString": "Host=localhost;Port=5432;Database=v3_admin_db;Username=your_username;Password=your_password"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long",
    "Issuer": "V3.Admin.Backend",
    "Audience": "V3.Admin.Frontend",
    "ExpirationHours": 1
  }
}
```

---

## 4. 註冊依賴項

在 `Program.cs` 中註冊權限管理相關的服務和存儲庫：

```csharp
// Repositories
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPermissionFailureLogRepository, PermissionFailureLogRepository>();

// Services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IPermissionValidationService, PermissionValidationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreatePermissionRequestValidator>();
```

---

## 5. 啟動應用程式

```bash
dotnet run
```

應用程式預設在 `http://localhost:5000` 啟動。

---

## 6. 使用範例

### 6.1 登入取得 JWT Token

首先需要登入以取得 JWT token（假設已有帳號）：

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin@123"
  }'
```

回應：
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "登入成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": "...",
    "username": "admin",
    "displayName": "系統管理員"
  },
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123"
}
```

將 `token` 儲存，後續請求需要使用。

---

### 6.2 建立權限

```bash
curl -X POST http://localhost:5000/api/permissions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "permissionCode": "inventory.create",
    "name": "新增庫存",
    "description": "允許新增庫存項目",
    "permissionType": "function"
  }'
```

回應：
```json
{
  "success": true,
  "code": "CREATED",
  "message": "權限建立成功",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "permissionCode": "inventory.create",
    "name": "新增庫存",
    "description": "允許新增庫存項目",
    "permissionType": "function",
    "routePath": null,
    "createdAt": "2025-11-05T10:30:00Z",
    "version": 1
  },
  "timestamp": "2025-11-05T10:30:00Z",
  "traceId": "abc123"
}
```

---

### 6.3 建立角色

```bash
curl -X POST http://localhost:5000/api/roles \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "roleName": "庫存管理員",
    "description": "負責庫存管理的角色"
  }'
```

回應：
```json
{
  "success": true,
  "code": "CREATED",
  "message": "角色建立成功",
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "roleName": "庫存管理員",
    "description": "負責庫存管理的角色",
    "createdAt": "2025-11-05T10:31:00Z",
    "version": 1
  },
  "timestamp": "2025-11-05T10:31:00Z",
  "traceId": "def456"
}
```

---

### 6.4 為角色分配權限

```bash
curl -X POST http://localhost:5000/api/roles/660e8400-e29b-41d4-a716-446655440001/permissions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "permissionIds": [
      "550e8400-e29b-41d4-a716-446655440000"
    ]
  }'
```

回應：
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "角色權限分配成功",
  "data": null,
  "timestamp": "2025-11-05T10:32:00Z",
  "traceId": "ghi789"
}
```

---

### 6.5 為用戶指派角色

```bash
curl -X POST http://localhost:5000/api/users/USER_ID/roles \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "roleId": "660e8400-e29b-41d4-a716-446655440001"
  }'
```

回應：
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "用戶角色指派成功",
  "data": null,
  "timestamp": "2025-11-05T10:33:00Z",
  "traceId": "jkl012"
}
```

---

### 6.6 查詢用戶的有效權限

```bash
curl -X GET http://localhost:5000/api/users/USER_ID/permissions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

回應：
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": {
    "userId": "USER_ID",
    "username": "john_doe",
    "permissions": [
      {
        "permissionCode": "inventory.create",
        "name": "新增庫存",
        "permissionType": "function",
        "routePath": null
      }
    ]
  },
  "timestamp": "2025-11-05T10:34:00Z",
  "traceId": "mno345"
}
```

---

### 6.7 驗證用戶權限

```bash
curl -X POST http://localhost:5000/api/permissions/validate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "permissionCode": "inventory.create"
  }'
```

回應（有權限）：
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "權限驗證通過",
  "data": {
    "hasPermission": true,
    "permissionCode": "inventory.create"
  },
  "timestamp": "2025-11-05T10:35:00Z",
  "traceId": "pqr678"
}
```

回應（無權限）：
```json
{
  "success": false,
  "code": "PERMISSION_DENIED",
  "message": "權限不足，無法執行此操作",
  "data": {
    "hasPermission": false,
    "permissionCode": "inventory.delete"
  },
  "timestamp": "2025-11-05T10:36:00Z",
  "traceId": "stu901"
}
```

---

### 6.8 查詢稽核日誌

```bash
curl -X GET "http://localhost:5000/api/audit-logs?pageNumber=1&pageSize=20&operationType=新增權限" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

回應：
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": {
    "items": [
      {
        "id": "770e8400-e29b-41d4-a716-446655440002",
        "operatorId": "...",
        "operatorName": "系統管理員",
        "operationTime": "2025-11-05T10:30:00Z",
        "operationType": "新增權限",
        "targetType": "Permission",
        "targetId": "550e8400-e29b-41d4-a716-446655440000",
        "beforeState": null,
        "afterState": "{\"permissionCode\":\"inventory.create\",\"name\":\"新增庫存\",...}",
        "ipAddress": "127.0.0.1",
        "userAgent": "curl/7.68.0"
      }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 20
  },
  "timestamp": "2025-11-05T10:37:00Z",
  "traceId": "vwx234"
}
```

---

## 7. 常見場景

### 7.1 設定完整的權限體系

1. **建立路由權限**（頁面訪問控制）
```bash
# 庫存管理頁面
POST /api/permissions
{
  "permissionCode": "inventory_page_access",
  "name": "庫存管理頁面訪問權限",
  "permissionType": "route",
  "routePath": "/inventory"
}

# 用戶管理頁面
POST /api/permissions
{
  "permissionCode": "users_page_access",
  "name": "用戶管理頁面訪問權限",
  "permissionType": "route",
  "routePath": "/users"
}
```

2. **建立功能權限**（操作控制）
```bash
# 庫存相關功能
POST /api/permissions
{
  "permissionCode": "inventory.create",
  "name": "新增庫存",
  "permissionType": "function"
}

POST /api/permissions
{
  "permissionCode": "inventory.update",
  "name": "修改庫存",
  "permissionType": "function"
}

POST /api/permissions
{
  "permissionCode": "inventory.delete",
  "name": "刪除庫存",
  "permissionType": "function"
}

POST /api/permissions
{
  "permissionCode": "inventory.view",
  "name": "查詢庫存",
  "permissionType": "function"
}
```

3. **建立角色並分配權限**
```bash
# 建立「庫存管理員」角色
POST /api/roles
{
  "roleName": "庫存管理員",
  "description": "負責庫存管理的角色，擁有完整的庫存操作權限"
}

# 為角色分配權限（路由 + 功能權限）
POST /api/roles/{roleId}/permissions
{
  "permissionIds": [
    "inventory_page_access_id",
    "inventory.create_id",
    "inventory.update_id",
    "inventory.delete_id",
    "inventory.view_id"
  ]
}
```

4. **為用戶指派角色**
```bash
POST /api/users/{userId}/roles
{
  "roleId": "inventory_manager_role_id"
}
```

---

### 7.2 多角色用戶權限合併

假設用戶同時擁有「庫存管理員」和「庫存查詢員」兩個角色：

- **庫存管理員**: inventory.create, inventory.update, inventory.delete, inventory.view
- **庫存查詢員**: inventory.view

系統會自動合併權限（聯集），用戶最終擁有：
- inventory.create
- inventory.update
- inventory.delete
- inventory.view

查詢用戶有效權限：
```bash
GET /api/users/{userId}/permissions
```

---

### 7.3 權限驗證失敗排查

如果用戶反映無法執行某操作：

1. **查詢用戶的角色**
```bash
GET /api/users/{userId}/roles
```

2. **查詢用戶的有效權限**
```bash
GET /api/users/{userId}/permissions
```

3. **查詢權限驗證失敗記錄**
```bash
GET /api/permission-failure-logs?userId={userId}&startTime=2025-11-05T00:00:00Z
```

4. **檢查角色的權限配置**
```bash
GET /api/roles/{roleId}/permissions
```

---

## 8. 測試

### 執行單元測試
```bash
dotnet test Tests/Unit/
```

### 執行整合測試
```bash
dotnet test Tests/Integration/
```

### 測試涵蓋範圍
- 權限 CRUD 操作
- 角色 CRUD 操作
- 角色權限分配
- 用戶角色指派
- 權限驗證邏輯（多角色合併）
- 稽核日誌記錄
- 樂觀並發控制
- 業務規則驗證（如刪除使用中的權限/角色）

---

## 9. 疑難排解

### 問題 1: 權限驗證始終失敗
**可能原因**:
- 用戶未被指派任何角色
- 角色未分配所需權限
- 權限代碼拼寫錯誤

**解決方案**:
1. 查詢用戶角色: `GET /api/users/{userId}/roles`
2. 查詢有效權限: `GET /api/users/{userId}/permissions`
3. 檢查權限代碼是否正確

---

### 問題 2: 無法刪除權限
**錯誤訊息**: "該權限正被角色使用，無法刪除"

**解決方案**:
1. 查詢使用該權限的角色: `GET /api/roles?permissionId={permissionId}`
2. 先從所有角色移除該權限: `DELETE /api/roles/{roleId}/permissions/{permissionId}`
3. 再刪除權限: `DELETE /api/permissions/{permissionId}`

---

### 問題 3: 併發更新衝突
**錯誤訊息**: "資料已被其他使用者更新，請重新載入後再試"

**解決方案**:
1. 重新查詢最新的權限/角色資料
2. 使用最新的 `version` 欄位值
3. 重新提交更新請求

---

### 問題 4: 權限變更未生效
**可能原因**: 權限驗證使用即時查詢，應該立即生效

**檢查步驟**:
1. 確認角色權限已正確分配: `GET /api/roles/{roleId}/permissions`
2. 確認用戶角色關聯存在: `GET /api/users/{userId}/roles`
3. 檢查稽核日誌確認操作已執行: `GET /api/audit-logs`

---

## 10. 後續步驟

完成快速開始後，建議：

1. **閱讀 API 契約文件** (`contracts/api-overview.md`) 瞭解完整的 API 規格
2. **閱讀資料模型文件** (`data-model.md`) 瞭解資料結構設計
3. **查看研究報告** (`research.md`) 瞭解技術決策背景
4. **執行完整的測試套件** 確保功能正常運作
5. **設定前端整合** 根據 API 契約對接前端應用

---

## 11. 相關文件

- [Feature Specification](./spec.md) - 完整功能規格
- [Implementation Plan](./plan.md) - 實作計劃
- [Data Model](./data-model.md) - 資料模型設計
- [Research Report](./research.md) - 技術研究報告
- [API Contracts](./contracts/api-overview.md) - API 契約總覽

---

## 12. 支援與回饋

如有問題或建議，請：
1. 查閱專案文件和規格
2. 檢查稽核日誌和錯誤日誌
3. 聯繫開發團隊

---

**祝您使用愉快！**
