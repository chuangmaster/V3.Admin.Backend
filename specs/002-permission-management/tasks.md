# Tasks: 權限管理機制

**Input**: Design documents from `/specs/002-permission-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/
**Language**: Traditional Chinese (zh-TW)
**Feature**: 完整的 RBAC 權限管理系統，包含權限定義、角色管理、用戶角色指派、權限驗證和稽核日誌功能

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可並行執行（不同檔案，無依賴關係）
- **[Story]**: 所屬用戶故事（US1, US2, US3, US4, US5, US6）
- 所有檔案路徑為絕對路徑，基於專案根目錄

## Path Conventions

- **Controllers**: `Controllers/` - API 端點層
- **Services**: `Services/`, `Services/Interfaces/` - 業務邏輯層
- **Repositories**: `Repositories/`, `Repositories/Interfaces/` - 資料存取層
- **Models**: `Models/Entities/`, `Models/Dtos/`, `Models/Requests/`, `Models/Responses/` - 資料模型
- **Validators**: `Validators/` - FluentValidation 驗證器
- **Middleware**: `Middleware/` - 中介軟體
- **Database**: `Database/Migrations/`, `Database/Scripts/` - 資料庫遷移與腳本
- **Tests**: `Tests/Unit/`, `Tests/Integration/` - 測試專案

---

## Phase 1: Setup (專案初始化)

**Purpose**: 專案結構初始化與基本設定

- [x] T001 建立專案資料夾結構（Controllers, Services, Repositories, Models, Validators, Middleware, Database, Tests）
- [x] T002 [P] 安裝 NuGet 套件：Npgsql 9.0, Dapper 2.1, BCrypt.Net-Next 4.0, Microsoft.AspNetCore.Authentication.JwtBearer 9.0, FluentValidation.AspNetCore 11.3, Serilog.AspNetCore 8.0
- [x] T003 [P] 設定 Serilog 結構化日誌記錄（appsettings.json, Program.cs），包含 TraceId 上下文
- [x] T004 [P] 設定 PostgreSQL 連線字串與 Dapper 初始化（Configuration/DatabaseSettings.cs, Program.cs）

---

## Phase 2: Foundational (核心基礎設施)

**Purpose**: 核心基礎設施，所有用戶故事的前置需求

**⚠️ CRITICAL**: 所有用戶故事實作前必須完成此階段。所有基礎任務必須遵循 constitution 原則。

- [x] T005 建立資料庫遷移檔案 `Database/Migrations/001_CreateUsersTable.sql`（如尚未存在，否則跳過）
- [x] T006 建立資料庫遷移檔案 `Database/Migrations/002_CreatePermissionsTable.sql`（permissions 表，包含索引與約束）
- [x] T007 建立資料庫遷移檔案 `Database/Migrations/003_CreateRolesTable.sql`（roles 表，包含索引與約束）
- [x] T008 建立資料庫遷移檔案 `Database/Migrations/004_CreateRolePermissionsTable.sql`（role_permissions 表，包含外鍵與唯一約束）
- [x] T009 建立資料庫遷移檔案 `Database/Migrations/005_CreateUserRolesTable.sql`（user_roles 表，包含外鍵與軟刪除支援）
- [x] T010 建立資料庫遷移檔案 `Database/Migrations/006_CreateAuditLogsTable.sql`（audit_logs 表，包含 JSONB 欄位與索引）
- [x] T011 建立資料庫遷移檔案 `Database/Migrations/007_CreatePermissionFailureLogsTable.sql`（permission_failure_logs 表，包含索引）
- [x] T012 [P] 建立初始權限種子資料腳本 `Database/Scripts/seed_permissions.sql`（管理員權限：permissions.*, roles.*, user_roles.*, audit_logs.view）
- [x] T013 [P] 擴充 ResponseCodes 枚舉 `Models/ResponseCodes.cs`（新增 PERMISSION_NOT_FOUND, ROLE_NOT_FOUND, PERMISSION_IN_USE, ROLE_IN_USE, DUPLICATE_PERMISSION_CODE, DUPLICATE_ROLE_NAME, CONCURRENT_UPDATE_CONFLICT）
- [x] T014 [P] 擴充 BaseApiController `Controllers/BaseApiController.cs`（新增 helper methods：Conflict, BusinessError, 優化 TraceId 處理）
- [x] T015 [P] 實作 TraceIdMiddleware `Middleware/TraceIdMiddleware.cs`（如尚未實作）確保所有 HTTP 請求包含 TraceId
- [x] T016 [P] 實作 ExceptionHandlingMiddleware `Middleware/ExceptionHandlingMiddleware.cs`（如尚未實作）統一錯誤處理與 ApiResponseModel 回應
- [x] T017 [P] 設定 XML 文件註解生成與 Swagger 整合 `Program.cs`（啟用 XML 文件註解，設定 Swagger UI）

**Checkpoint**: 基礎設施完成 - 用戶故事實作可以開始並行執行

---

## Phase 3: User Story 1 - 權限建立與管理 (Priority: P1) 🎯 MVP

**Goal**: 系統管理員可以定義、新增、修改、刪除和查詢系統中所有權限（路由權限與功能權限）

**Independent Test**: 管理員可以創建路由權限（如「/inventory」頁面訪問權限）和功能權限（如「inventory.create」新增庫存權限），並能查詢、修改或刪除這些權限，無需其他功能即可驗證

### Implementation for User Story 1

- [x] T018 [P] [US1] 建立 Permission 實體 `Models/Entities/Permission.cs`（遵循 C# 13 best practices, PascalCase, XML 文件註解）
- [x] T019 [P] [US1] 建立 PermissionDto `Models/Dtos/PermissionDto.cs`（用於 API 回應，包含 id, permissionCode, name, description, permissionType, routePath, createdAt, version）
- [x] T020 [P] [US1] 建立 CreatePermissionRequest `Models/Requests/CreatePermissionRequest.cs`（包含 permissionCode, name, description, permissionType, routePath）
- [x] T021 [P] [US1] 建立 UpdatePermissionRequest `Models/Requests/UpdatePermissionRequest.cs`（包含 name, description, routePath, version）
- [x] T022 [P] [US1] 建立 DeletePermissionRequest `Models/Requests/DeletePermissionRequest.cs`（包含 version）
- [x] T023 [P] [US1] 建立 PermissionResponse `Models/Responses/PermissionResponse.cs`（繼承 ApiResponseModel<PermissionDto>）
- [x] T024 [P] [US1] 建立 PermissionListResponse `Models/Responses/PermissionListResponse.cs`（繼承 ApiResponseModel，包含分頁資訊：items, totalCount, pageNumber, pageSize）
- [x] T025 [P] [US1] 建立 CreatePermissionRequestValidator `Validators/CreatePermissionRequestValidator.cs`（FluentValidation，驗證 permissionCode 格式、name 長度、permissionType 值、routePath 必填性）
- [x] T026 [P] [US1] 建立 UpdatePermissionRequestValidator `Validators/UpdatePermissionRequestValidator.cs`（FluentValidation，驗證 name 長度、version >= 1）
- [x] T027 [US1] 建立 IPermissionRepository 介面 `Repositories/Interfaces/IPermissionRepository.cs`（方法：CreateAsync, GetByIdAsync, GetAllAsync, UpdateAsync, DeleteAsync, ExistsAsync, IsInUseAsync）
- [x] T028 [US1] 實作 PermissionRepository `Repositories/PermissionRepository.cs`（使用 Dapper, snake_case 與 PascalCase 映射, 查詢過濾 is_deleted = false, 軟刪除實作, 樂觀並發控制）（依賴 T027）
- [x] T029 [US1] 建立 IPermissionService 介面 `Services/Interfaces/IPermissionService.cs`（方法：CreatePermissionAsync, GetPermissionByIdAsync, GetPermissionsAsync, UpdatePermissionAsync, DeletePermissionAsync）
- [x] T030 [US1] 實作 PermissionService `Services/PermissionService.cs`（業務邏輯：檢查權限代碼唯一性、防止刪除使用中權限、稽核日誌記錄、併發控制、DTO 轉換）（依賴 T028, T029）
- [x] T031 [US1] 實作 PermissionController `Controllers/PermissionController.cs`（API 端點：GET /api/permissions, POST /api/permissions, GET /api/permissions/{id}, PUT /api/permissions/{id}, DELETE /api/permissions/{id}，使用 ApiResponseModel 包裝器，JWT 驗證，權限驗證 [Authorize]）（依賴 T030）
- [x] T032 [P] [US1] 整合測試 `Tests/Integration/PermissionControllerIntegrationTests.cs`（測試：建立權限成功、代碼重複失敗、查詢列表分頁、更新權限、刪除使用中權限失敗、併發更新衝突、軟刪除成功）
- [x] T033 [P] [US1] 單元測試 `Tests/Unit/Services/PermissionServiceTests.cs`（測試：CreatePermission 業務規則、權限代碼唯一性驗證、刪除使用中權限檢查）

**Checkpoint**: 權限管理功能完全可用且可獨立測試，管理員可以完整管理系統權限

---

## Phase 4: User Story 2 - 角色建立與權限配置 (Priority: P1) 🎯 MVP

**Goal**: 系統管理員可以創建自訂角色，並為每個角色配置路由權限和功能權限

**Independent Test**: 管理員可以創建「庫存管理員」角色，為其分配「庫存管理頁面」路由權限以及「新增庫存」、「修改庫存」功能權限，並能查詢、修改或刪除角色，無需實際指派給用戶即可驗證

### Implementation for User Story 2

- [ ] T034 [P] [US2] 建立 Role 實體 `Models/Entities/Role.cs`（遵循 C# 13 best practices, PascalCase, XML 文件註解）
- [ ] T035 [P] [US2] 建立 RolePermission 實體 `Models/Entities/RolePermission.cs`（關聯表實體，包含 roleId, permissionId, assignedBy, assignedAt）
- [ ] T036 [P] [US2] 建立 RoleDto `Models/Dtos/RoleDto.cs`（用於 API 回應，包含 id, roleName, description, createdAt, version）
- [ ] T037 [P] [US2] 建立 RoleDetailDto `Models/Dtos/RoleDetailDto.cs`（包含 RoleDto 欄位 + permissions 列表）
- [ ] T038 [P] [US2] 建立 CreateRoleRequest `Models/Requests/CreateRoleRequest.cs`（包含 roleName, description）
- [ ] T039 [P] [US2] 建立 UpdateRoleRequest `Models/Requests/UpdateRoleRequest.cs`（包含 roleName, description, version）
- [ ] T040 [P] [US2] 建立 DeleteRoleRequest `Models/Requests/DeleteRoleRequest.cs`（包含 version）
- [ ] T041 [P] [US2] 建立 AssignRolePermissionsRequest `Models/Requests/AssignRolePermissionsRequest.cs`（包含 permissionIds 陣列）
- [ ] T042 [P] [US2] 建立 RoleResponse `Models/Responses/RoleResponse.cs`（繼承 ApiResponseModel<RoleDto>）
- [ ] T043 [P] [US2] 建立 RoleListResponse `Models/Responses/RoleListResponse.cs`（繼承 ApiResponseModel，包含分頁資訊）
- [ ] T044 [P] [US2] 建立 RoleDetailResponse `Models/Responses/RoleDetailResponse.cs`（繼承 ApiResponseModel<RoleDetailDto>）
- [ ] T045 [P] [US2] 建立 CreateRoleRequestValidator `Validators/CreateRoleRequestValidator.cs`（FluentValidation，驗證 roleName 長度 1-100 字元）
- [ ] T046 [P] [US2] 建立 UpdateRoleRequestValidator `Validators/UpdateRoleRequestValidator.cs`（FluentValidation，驗證 roleName 長度、version >= 1）
- [ ] T047 [P] [US2] 建立 AssignRolePermissionsRequestValidator `Validators/AssignRolePermissionsRequestValidator.cs`（FluentValidation，驗證 permissionIds 非空且每個 ID 格式正確）
- [ ] T048 [US2] 建立 IRoleRepository 介面 `Repositories/Interfaces/IRoleRepository.cs`（方法：CreateAsync, GetByIdAsync, GetAllAsync, UpdateAsync, DeleteAsync, ExistsAsync, IsInUseAsync）
- [ ] T049 [US2] 實作 RoleRepository `Repositories/RoleRepository.cs`（使用 Dapper, snake_case 映射, 軟刪除, 樂觀並發控制）（依賴 T048）
- [ ] T050 [US2] 建立 IRolePermissionRepository 介面 `Repositories/Interfaces/IRolePermissionRepository.cs`（方法：AssignPermissionsAsync, RemovePermissionAsync, GetRolePermissionsAsync, ClearRolePermissionsAsync）
- [ ] T051 [US2] 實作 RolePermissionRepository `Repositories/RolePermissionRepository.cs`（使用 Dapper, 批次新增支援, 防止重複分配）（依賴 T050）
- [ ] T052 [US2] 建立 IRoleService 介面 `Services/Interfaces/IRoleService.cs`（方法：CreateRoleAsync, GetRoleByIdAsync, GetRolesAsync, GetRoleDetailAsync, UpdateRoleAsync, DeleteRoleAsync, AssignPermissionsAsync, RemovePermissionAsync）
- [ ] T053 [US2] 實作 RoleService `Services/RoleService.cs`（業務邏輯：檢查角色名稱唯一性、防止刪除使用中角色、權限存在性驗證、稽核日誌記錄、併發控制）（依賴 T049, T051, T052）
- [ ] T054 [US2] 實作 RoleController `Controllers/RoleController.cs`（API 端點：GET /api/roles, POST /api/roles, GET /api/roles/{id}, GET /api/roles/{id}/permissions, PUT /api/roles/{id}, DELETE /api/roles/{id}, POST /api/roles/{roleId}/permissions, DELETE /api/roles/{roleId}/permissions/{permissionId}，使用 ApiResponseModel 包裝器）（依賴 T053）
- [ ] T055 [P] [US2] 整合測試 `Tests/Integration/RoleControllerIntegrationTests.cs`（測試：建立角色成功、名稱重複失敗、分配權限成功、移除權限成功、查詢角色詳情含權限列表、刪除使用中角色失敗）
- [ ] T056 [P] [US2] 單元測試 `Tests/Unit/Services/RoleServiceTests.cs`（測試：CreateRole 業務規則、角色名稱唯一性驗證、刪除使用中角色檢查、分配不存在權限失敗）

**Checkpoint**: 角色管理功能完全可用且可獨立測試，管理員可以創建角色並配置權限

---

## Phase 5: User Story 3 - 用戶角色指派 (Priority: P2)

**Goal**: 系統管理員可以為用戶指派一個或多個角色，使用戶繼承角色所擁有的所有權限

**Independent Test**: 管理員可以選擇一個已存在的用戶，為其指派「庫存管理員」角色，系統記錄該指派關係，管理員可以查詢該用戶當前擁有的角色，並可以移除角色分配

### Implementation for User Story 3

- [ ] T057 [P] [US3] 建立 UserRole 實體 `Models/Entities/UserRole.cs`（關聯表實體，包含 userId, roleId, assignedBy, assignedAt, 軟刪除欄位）
- [ ] T058 [P] [US3] 建立 UserRoleDto `Models/Dtos/UserRoleDto.cs`（用於 API 回應，包含 id, userId, roleId, roleName, assignedAt）
- [ ] T059 [P] [US3] 建立 AssignUserRoleRequest `Models/Requests/AssignUserRoleRequest.cs`（包含 roleIds 陣列）
- [ ] T060 [P] [US3] 建立 RemoveUserRoleRequest `Models/Requests/RemoveUserRoleRequest.cs`（包含 roleId）
- [ ] T061 [P] [US3] 建立 UserRoleResponse `Models/Responses/UserRoleResponse.cs`（繼承 ApiResponseModel<List<UserRoleDto>>）
- [ ] T062 [P] [US3] 建立 AssignUserRoleRequestValidator `Validators/AssignUserRoleRequestValidator.cs`（FluentValidation，驗證 roleIds 非空且每個 ID 格式正確）
- [ ] T063 [US3] 建立 IUserRoleRepository 介面 `Repositories/Interfaces/IUserRoleRepository.cs`（方法：AssignRoleAsync, RemoveRoleAsync, GetUserRolesAsync, HasRoleAsync）
- [ ] T064 [US3] 實作 UserRoleRepository `Repositories/UserRoleRepository.cs`（使用 Dapper, 軟刪除實作, 防止重複指派）（依賴 T063）
- [ ] T065 [US3] 建立 IUserRoleService 介面 `Services/Interfaces/IUserRoleService.cs`（方法：AssignRoleAsync, RemoveRoleAsync, GetUserRolesAsync）
- [ ] T066 [US3] 實作 UserRoleService `Services/UserRoleService.cs`（業務邏輯：檢查用戶與角色存在性、防止重複指派、稽核日誌記錄）（依賴 T064, T065）
- [ ] T067 [US3] 實作 UserRoleController `Controllers/UserRoleController.cs`（API 端點：GET /api/users/{userId}/roles, POST /api/users/{userId}/roles, DELETE /api/users/{userId}/roles/{roleId}，使用 ApiResponseModel 包裝器）（依賴 T066）
- [ ] T068 [P] [US3] 整合測試 `Tests/Integration/UserRoleControllerIntegrationTests.cs`（測試：指派角色成功、重複指派失敗、查詢用戶角色列表、移除角色成功、軟刪除驗證）
- [ ] T069 [P] [US3] 單元測試 `Tests/Unit/Services/UserRoleServiceTests.cs`（測試：AssignRole 業務規則、重複指派檢查、用戶/角色存在性驗證）

**Checkpoint**: 用戶角色指派功能完全可用且可獨立測試，管理員可以為用戶分配和移除角色

---

## Phase 6: User Story 4 - 權限驗證與訪問控制 (Priority: P1) 🎯 MVP

**Goal**: 系統必須在用戶訪問頁面或執行操作時，即時驗證用戶是否擁有相應的路由權限或功能權限

**Independent Test**: 創建測試用戶並指派特定權限，測試該用戶訪問不同頁面和執行不同操作時，系統是否正確允許或拒絕

### Implementation for User Story 4

- [ ] T070 [P] [US4] 建立 PermissionFailureLog 實體 `Models/Entities/PermissionFailureLog.cs`（包含 userId, username, attemptedResource, failureReason, attemptedAt, ipAddress, userAgent, traceId）
- [ ] T071 [P] [US4] 建立 UserEffectivePermissionsDto `Models/Dtos/UserEffectivePermissionsDto.cs`（包含 userId, permissions 列表：合併後的所有權限）
- [ ] T072 [P] [US4] 建立 ValidatePermissionRequest `Models/Requests/ValidatePermissionRequest.cs`（包含 permissionCode）
- [ ] T073 [P] [US4] 建立 PermissionValidationResponse `Models/Responses/PermissionValidationResponse.cs`（繼承 ApiResponseModel<bool>，包含 hasPermission 欄位）
- [ ] T074 [P] [US4] 建立 UserEffectivePermissionsResponse `Models/Responses/UserEffectivePermissionsResponse.cs`（繼承 ApiResponseModel<UserEffectivePermissionsDto>）
- [ ] T075 [US4] 建立 IPermissionFailureLogRepository 介面 `Repositories/Interfaces/IPermissionFailureLogRepository.cs`（方法：LogFailureAsync, GetFailureLogsAsync）
- [ ] T076 [US4] 實作 PermissionFailureLogRepository `Repositories/PermissionFailureLogRepository.cs`（使用 Dapper, 僅新增和查詢）（依賴 T075）
- [ ] T077 [US4] 建立 IPermissionValidationService 介面 `Services/Interfaces/IPermissionValidationService.cs`（方法：ValidatePermissionAsync, GetUserEffectivePermissionsAsync, LogPermissionFailureAsync）
- [ ] T078 [US4] 實作 PermissionValidationService `Services/PermissionValidationService.cs`（業務邏輯：即時查詢最新權限配置、多角色權限合併（聯集）、權限驗證失敗記錄、性能優化 <100ms）（依賴 T077, T076）
- [ ] T079 [US4] 實作 PermissionAuthorizationMiddleware `Middleware/PermissionAuthorizationMiddleware.cs`（自動權限驗證中介軟體，讀取 [RequirePermission] attribute，驗證失敗返回 403 Forbidden 並記錄失敗日誌）（依賴 T078）
- [ ] T080 [US4] 擴充 BaseApiController `Controllers/BaseApiController.cs`（新增 [RequirePermission] attribute 支援，如 [RequirePermission("permissions.create")]）
- [ ] T081 [US4] 實作 API 端點 POST /api/permissions/validate 於 PermissionController（驗證單一權限）（依賴 T078）
- [ ] T082 [US4] 實作 API 端點 GET /api/users/{userId}/permissions 於 UserRoleController（查詢用戶有效權限）（依賴 T078）
- [ ] T083 [P] [US4] 整合測試 `Tests/Integration/PermissionValidationIntegrationTests.cs`（測試：多角色權限合併正確、權限驗證失敗記錄、即時生效驗證、中介軟體驗證成功/失敗、查詢用戶有效權限）
- [ ] T084 [P] [US4] 單元測試 `Tests/Unit/Services/PermissionValidationServiceTests.cs`（測試：權限合併邏輯、驗證失敗記錄邏輯、性能基準測試 <100ms）
- [ ] T085 [US4] 為現有 PermissionController, RoleController, UserRoleController 端點新增 [RequirePermission] attributes（依賴 T079, T080）

**Checkpoint**: 權限驗證功能完全可用且可獨立測試，系統可以即時驗證用戶權限並記錄失敗嘗試

---

## Phase 7: User Story 5 - 稽核日誌記錄 (Priority: P2)

**Goal**: 系統必須記錄所有與權限管理相關的操作，包括權限的新增、修改、刪除，角色的創建、權限分配，用戶角色的指派等

**Independent Test**: 執行一系列權限管理操作（如創建權限、分配角色），然後查詢稽核日誌，驗證所有操作是否被正確記錄，包含所有必要的欄位資訊

### Implementation for User Story 5

- [ ] T086 [P] [US5] 建立 AuditLog 實體 `Models/Entities/AuditLog.cs`（包含 operatorId, operatorName, operationTime, operationType, targetType, targetId, beforeState, afterState, ipAddress, userAgent, traceId）
- [ ] T087 [P] [US5] 建立 AuditLogDto `Models/Dtos/AuditLogDto.cs`（用於 API 回應，包含所有 AuditLog 欄位）
- [ ] T088 [P] [US5] 建立 QueryAuditLogRequest `Models/Requests/QueryAuditLogRequest.cs`（包含 startTime, endTime, operatorId, operationType, targetType, pageNumber, pageSize）
- [ ] T089 [P] [US5] 建立 AuditLogListResponse `Models/Responses/AuditLogListResponse.cs`（繼承 ApiResponseModel，包含分頁資訊）
- [ ] T090 [P] [US5] 建立 QueryAuditLogRequestValidator `Validators/QueryAuditLogRequestValidator.cs`（FluentValidation，驗證時間範圍、分頁參數）
- [ ] T091 [US5] 建立 IAuditLogRepository 介面 `Repositories/Interfaces/IAuditLogRepository.cs`（方法：LogAsync, GetByIdAsync, GetLogsAsync）
- [ ] T092 [US5] 實作 AuditLogRepository `Repositories/AuditLogRepository.cs`（使用 Dapper, 僅新增和查詢, 複雜篩選查詢, 索引優化）（依賴 T091）
- [ ] T093 [US5] 建立 IAuditLogService 介面 `Services/Interfaces/IAuditLogService.cs`（方法：LogOperationAsync, GetAuditLogByIdAsync, GetAuditLogsAsync）
- [ ] T094 [US5] 實作 AuditLogService `Services/AuditLogService.cs`（業務邏輯：稽核日誌記錄、JSON 序列化 beforeState/afterState、分頁查詢、篩選支援）（依賴 T092, T093）
- [ ] T095 [US5] 整合稽核日誌記錄到 PermissionService（所有 CRUD 操作在同一 Transaction 中記錄稽核日誌，記錄失敗時回滾業務操作）（依賴 T094）
- [ ] T096 [US5] 整合稽核日誌記錄到 RoleService（所有 CRUD 和權限分配操作記錄稽核日誌）（依賴 T094）
- [ ] T097 [US5] 整合稽核日誌記錄到 UserRoleService（所有角色指派操作記錄稽核日誌）（依賴 T094）
- [ ] T098 [US5] 實作 AuditLogController `Controllers/AuditLogController.cs`（API 端點：GET /api/audit-logs, GET /api/audit-logs/{id}，使用 ApiResponseModel 包裝器）（依賴 T094）
- [ ] T099 [P] [US5] 整合測試 `Tests/Integration/AuditLogControllerIntegrationTests.cs`（測試：查詢稽核日誌分頁、多條件篩選、稽核日誌記錄完整性、Transaction 回滾驗證）
- [ ] T100 [P] [US5] 單元測試 `Tests/Unit/Services/AuditLogServiceTests.cs`（測試：JSON 序列化正確性、分頁查詢邏輯、篩選條件正確性）

**Checkpoint**: 稽核日誌功能完全可用且可獨立測試，所有權限管理操作均被完整記錄

---

## Phase 8: User Story 6 - 權限繼承與合併 (Priority: P3)

**Goal**: 當用戶被指派多個角色時，系統必須合併所有角色的權限，用戶擁有所有角色的聯集權限，並提供介面讓管理員查看用戶的有效權限

**Independent Test**: 創建兩個角色（如「庫存查詢員」和「庫存管理員」），為用戶同時指派這兩個角色，然後查詢該用戶的有效權限，驗證系統是否正確顯示兩個角色的聯集權限

### Implementation for User Story 6

- [ ] T101 [P] [US6] 建立 PermissionFailureLogDto `Models/Dtos/PermissionFailureLogDto.cs`（用於 API 回應，包含所有 PermissionFailureLog 欄位）
- [ ] T102 [P] [US6] 建立 PermissionFailureLogListResponse `Models/Responses/PermissionFailureLogListResponse.cs`（繼承 ApiResponseModel，包含分頁資訊）
- [ ] T103 [US6] 實作 API 端點 GET /api/permission-failure-logs 於 AuditLogController 或新建 PermissionFailureLogController（查詢權限驗證失敗記錄，支援分頁與篩選）（依賴 T076）
- [ ] T104 [P] [US6] 整合測試 `Tests/Integration/PermissionInheritanceIntegrationTests.cs`（測試：多角色聯集權限正確性、去重驗證、角色移除後權限更新、查詢用戶有效權限 API）
- [ ] T105 [P] [US6] 單元測試 `Tests/Unit/Services/PermissionValidationServiceTests.cs`（補充測試：權限合併去重邏輯、空角色處理、大量角色性能測試）

**Checkpoint**: 權限繼承與合併功能完全可用，管理員可以查看用戶的完整有效權限列表

---

## Phase 9: Polish & Cross-Cutting Concerns (優化與交叉關注點)

**Purpose**: 改進影響多個用戶故事的功能與品質

- [ ] T106 [P] 更新 XML 文件註解與 README.md（包含 API 端點列表、快速開始指南、資料庫遷移步驟）
- [ ] T107 程式碼清理與重構（遵循 C# 13 best practices、移除重複程式碼、優化 LINQ 查詢）
- [ ] T108 性能優化（async/await 模式驗證、資料庫查詢優化防止 N+1、索引優化、權限驗證 <100ms 基準測試）
- [ ] T109 [P] 補充單元測試（Controllers, Services, Repositories 覆蓋率 >= 80%）
- [ ] T110 安全加固（JWT token 驗證強化、輸入消毒、SQL 注入防護驗證、敏感資訊脫敏）
- [ ] T111 Constitution 合規性驗證（檢查所有實體遵循命名規範、三層式架構完整性、ApiResponseModel 使用一致性、錯誤訊息繁體中文化）
- [ ] T112 執行 quickstart.md 驗證與更新（驗證所有範例程式碼可執行、更新 Swagger 文件、補充故障排除指南）
- [ ] T113 [P] 建立資料庫遷移執行腳本 `Database/Scripts/run_migrations.sql`（按順序執行 001-007 遷移檔案）
- [ ] T114 [P] 建立端到端整合測試 `Tests/Integration/E2EPermissionManagementTests.cs`（完整流程測試：建立權限 → 建立角色 → 分配權限 → 指派角色 → 驗證權限 → 查詢稽核日誌）

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無依賴 - 可立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - **阻塞所有用戶故事**
- **User Stories (Phase 3-8)**: 全部依賴 Foundational 完成
  - 用戶故事可以並行執行（如有團隊資源）
  - 或按優先順序依序執行（P1 → P2 → P3）
- **Polish (Phase 9)**: 依賴所有期望的用戶故事完成

### User Story Dependencies

- **User Story 1 (P1 - 權限管理)**: Foundational 完成後可開始 - **無其他用戶故事依賴** - MVP 必需
- **User Story 2 (P1 - 角色管理)**: Foundational 完成後可開始 - **依賴 US1 的 Permission 實體和 Repository** - MVP 必需
- **User Story 3 (P2 - 用戶角色指派)**: Foundational 完成後可開始 - **依賴 US2 的 Role 實體和 Repository**
- **User Story 4 (P1 - 權限驗證)**: Foundational 完成後可開始 - **依賴 US1, US2, US3 的實體和 Repository** - MVP 必需
- **User Story 5 (P2 - 稽核日誌)**: Foundational 完成後可開始 - **無其他用戶故事依賴（獨立記錄機制）**，但需整合到 US1, US2, US3 的 Service 層
- **User Story 6 (P3 - 權限繼承)**: Foundational 完成後可開始 - **依賴 US4 的 PermissionValidationService**

### Within Each User Story

- Models/Entities before Repositories
- Repositories before Services
- Services before Controllers
- Tests 可與實作並行（TDD 先寫測試）
- 核心實作完成後再整合到其他用戶故事
- 用戶故事完成後再移至下一優先級

### Parallel Opportunities

- Phase 1 所有標記 [P] 的任務可並行
- Phase 2 所有標記 [P] 的任務可並行（在 Phase 2 內部）
- **Foundational 完成後**：
  - US1, US2, US3, US5 可同時啟動（不同團隊成員）
  - US4 需等待 US1, US2, US3 的 Repositories 完成後啟動
  - US6 需等待 US4 完成後啟動
- 每個用戶故事內部標記 [P] 的任務可並行
- 不同用戶故事可由不同團隊成員並行開發

---

## Parallel Example: Foundational Phase

```bash
# Foundational 階段內部可並行執行（資料庫遷移檔案除外需按順序）:
Task T012: "建立初始權限種子資料腳本"
Task T013: "擴充 ResponseCodes 枚舉"
Task T014: "擴充 BaseApiController"
Task T015: "實作 TraceIdMiddleware"
Task T016: "實作 ExceptionHandlingMiddleware"
Task T017: "設定 XML 文件註解生成與 Swagger 整合"
```

---

## Parallel Example: User Story 1

```bash
# US1 內部可並行執行的任務（Models, DTOs, Validators）:
Task T018: "建立 Permission 實體"
Task T019: "建立 PermissionDto"
Task T020: "建立 CreatePermissionRequest"
Task T021: "建立 UpdatePermissionRequest"
Task T022: "建立 DeletePermissionRequest"
Task T023: "建立 PermissionResponse"
Task T024: "建立 PermissionListResponse"
Task T025: "建立 CreatePermissionRequestValidator"
Task T026: "建立 UpdatePermissionRequestValidator"

# US1 測試可並行執行:
Task T032: "整合測試 PermissionControllerIntegrationTests"
Task T033: "單元測試 PermissionServiceTests"
```

---

## Implementation Strategy

### MVP First (User Story 1, 2, 4 Only - P1 優先)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational (**CRITICAL - 阻塞所有用戶故事**)
3. 完成 Phase 3: User Story 1（權限管理）
4. **STOP and VALIDATE**: 獨立測試 User Story 1
5. 完成 Phase 4: User Story 2（角色管理）
6. **STOP and VALIDATE**: 獨立測試 User Story 2
7. 完成 Phase 6: User Story 4（權限驗證）
8. **STOP and VALIDATE**: 獨立測試 User Story 4
9. **MVP Complete**: 部署/演示基本 RBAC 功能

### Incremental Delivery (Full Feature Set)

1. 完成 Setup + Foundational → 基礎完備
2. 新增 User Story 1 → 獨立測試 → 部署/演示（權限管理）
3. 新增 User Story 2 → 獨立測試 → 部署/演示（角色管理）
4. 新增 User Story 4 → 獨立測試 → 部署/演示（權限驗證 - **MVP 完成**）
5. 新增 User Story 3 → 獨立測試 → 部署/演示（用戶角色指派）
6. 新增 User Story 5 → 獨立測試 → 部署/演示（稽核日誌）
7. 新增 User Story 6 → 獨立測試 → 部署/演示（權限繼承查詢）
8. 每個用戶故事新增價值且不破壞前面的功能

### Parallel Team Strategy (多開發者)

1. 團隊一起完成 Setup + Foundational
2. **Foundational 完成後**：
   - **Developer A**: User Story 1（權限管理）
   - **Developer B**: User Story 2（角色管理，依賴 US1 的 PermissionRepository）
   - **Developer C**: User Story 5（稽核日誌，獨立開發）
3. US1, US2 完成後：
   - **Developer A**: User Story 3（用戶角色指派）
   - **Developer B**: User Story 4（權限驗證）
4. US4 完成後：
   - **Developer C**: User Story 6（權限繼承）
5. 用戶故事獨立完成並整合

---

## Task Count Summary

- **Setup (Phase 1)**: 4 tasks
- **Foundational (Phase 2)**: 13 tasks
- **User Story 1 (P1)**: 16 tasks (T018-T033)
- **User Story 2 (P1)**: 23 tasks (T034-T056)
- **User Story 3 (P2)**: 13 tasks (T057-T069)
- **User Story 4 (P1)**: 16 tasks (T070-T085)
- **User Story 5 (P2)**: 15 tasks (T086-T100)
- **User Story 6 (P3)**: 5 tasks (T101-T105)
- **Polish (Phase 9)**: 9 tasks (T106-T114)

**Total**: 114 tasks

**MVP Scope (US1 + US2 + US4)**: Setup (4) + Foundational (13) + US1 (16) + US2 (23) + US4 (16) = **72 tasks**

---

## Notes

- **[P]** 標記 = 不同檔案，無依賴關係，可並行執行
- **[Story]** 標籤將任務映射到特定用戶故事以便追溯
- 每個用戶故事應可獨立完成和測試
- 測試可先寫（TDD），確保測試失敗後再實作
- 每個任務或邏輯組完成後提交 commit
- 在任何 checkpoint 停止以獨立驗證用戶故事
- 避免：模糊任務、相同檔案衝突、破壞獨立性的跨用戶故事依賴

---

## Recommendations

### For Solo Developer

1. 按優先級順序依次完成用戶故事：Setup → Foundational → US1 → US2 → US4（MVP）→ US3 → US5 → US6
2. 每完成一個用戶故事立即驗證其獨立功能
3. MVP 完成後（US1, US2, US4）可先部署基本功能，後續漸進增強

### For Team

1. 一起完成 Setup + Foundational（約 1-2 天）
2. Foundational 完成後分工：
   - 開發者 A: US1（權限管理）
   - 開發者 B: US2（角色管理，注意依賴 US1 的 PermissionRepository）
   - 開發者 C: US5（稽核日誌，獨立開發）
3. US1, US2 完成後：
   - 開發者 A: US3（用戶角色指派）
   - 開發者 B: US4（權限驗證）
4. 最後完成 US6（權限繼承查詢）
5. 全員一起完成 Polish 階段

### For Quick Validation

1. 完成 Setup + Foundational
2. 僅實作 US1（權限管理）+ US2（角色管理）
3. 手動測試權限與角色的 CRUD 操作
4. 驗證通過後再繼續其他用戶故事

---

**End of Tasks Document**
