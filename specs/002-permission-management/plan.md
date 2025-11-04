# Implementation Plan: 權限管理機制

**Branch**: `002-permission-management` | **Date**: 2025-11-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-permission-management/spec.md`
**Language**: This plan MUST be written in Traditional Chinese (zh-TW) per constitution requirements

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

實作完整的權限管理機制（RBAC - Role-Based Access Control），包含權限定義、角色管理、用戶角色指派、權限驗證和稽核日誌功能。系統將支援兩種權限類型：路由權限（控制頁面訪問）和功能權限（控制操作權限如新增、修改、刪除）。採用 .NET 9 + PostgreSQL + Dapper 實作三層式架構，確保權限變更即時生效，並提供完整的稽核追蹤。

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: 
- ASP.NET Core 9 (Web API 框架)
- Npgsql 9.0 (PostgreSQL 連接驅動)
- Dapper 2.1 (微型 ORM，用於數據訪問)
- BCrypt.Net-Next 4.0 (密碼哈希)
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0 (JWT 驗證)
- FluentValidation.AspNetCore 11.3 (輸入驗證)
- Serilog.AspNetCore 8.0 (結構化日誌)

**Storage**: PostgreSQL 16+ (生產環境), 採用 snake_case 命名規範
**Testing**: xUnit 2.9, Moq 4.20 (mocking), Microsoft.AspNetCore.Mvc.Testing 9.0 (整合測試)
**Target Platform**: Cross-platform (Windows/Linux/macOS)
**Project Type**: Web API - ASP.NET Core backend service for v3-admin-frontend

**Performance Goals**: 
- 權限驗證 <100ms (單次請求，包含多角色權限合併)
- 權限/角色 CRUD 操作 <200ms
- 分頁查詢 (帳號/角色/權限列表) <500ms
- 稽核日誌查詢 <2000ms (支援百萬級記錄篩選)
- 併發支援 1000 TPS 權限驗證請求

**Constraints**: 
- JWT 驗證必須整合到所有受保護端點
- 所有 API 回應必須使用 ApiResponseModel 包裝器 (HTTP status + business code 雙層設計)
- 錯誤訊息必須使用繁體中文
- 權限變更必須即時生效 (下次請求時驗證最新配置，不使用登入時快照)
- TraceId 必須包含在所有回應中用於分散式追蹤
- 稽核日誌必須完整記錄且不可修改/刪除（僅新增和查詢）
- 必須防止刪除正在使用的權限和角色（cascade 檢查）
- 必須實作樂觀並發控制防止數據衝突

**Scale/Scope**: 
- 支援數千個權限定義
- 支援數百個自訂角色
- 支援數萬用戶的權限驗證
- 稽核日誌永久保留，預期累積百萬級以上記錄

**Known Challenges**:
- 權限驗證性能優化（多角色權限合併）→ NEEDS CLARIFICATION: 是否需要權限快取機制？
- 稽核日誌查詢效能（大量記錄）→ NEEDS CLARIFICATION: 索引策略和分割表方案？
- 權限繼承與合併邏輯複雜度 → NEEDS CLARIFICATION: 權限衝突解決策略？
- 併發寫入稽核日誌的性能影響 → NEEDS CLARIFICATION: 非同步寫入或批次處理？

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Code Quality Excellence**: ✅ **PASS** - 所有程式碼將遵循 C# 13 最佳實踐，包含 XML 文件註解、繁體中文註解、PascalCase 公開成員、camelCase 私有欄位、介面以 "I" 前綴、檔案範圍命名空間、nullable reference types。資料庫使用 snake_case 命名規範（tables/columns），C# 實體使用 PascalCase，透過 Dapper 明確映射。

**Three-Layer Architecture**: ✅ **PASS** - 嚴格遵循三層式架構：Controllers (Presentation) 僅處理 HTTP 關注點並委派至 Services；Services (Business Logic) 使用 DTOs 進行資料傳輸並注入 Repositories；Repositories (Data Access) 操作 Entity 模型並處理所有資料持久化。所有依賴項在 Program.cs 中透過 DI 註冊。

**Test-First Development**: ✅ **PASS** - 所有關鍵路徑將先撰寫測試：權限 CRUD、角色管理、權限驗證邏輯、稽核日誌記錄、多角色權限合併。整合測試涵蓋 API 端點、驗證流程、角色指派、權限驗證、跨層交互。測試使用 xUnit、Moq、Microsoft.AspNetCore.Mvc.Testing。

**User Experience Consistency & Admin Interface Standards**: ✅ **PASS** - 所有 API 回應使用 ApiResponseModel 包裝器（Success, Code, Message, Data, Timestamp, TraceId）。雙層設計結合 HTTP 狀態碼（請求處理狀態）和業務邏輯碼（細粒度業務場景）。錯誤訊息使用繁體中文，業務規則違規提供詳細回饋（如 PERMISSION_IN_USE、ROLE_IN_USE、CANNOT_DELETE_PERMISSION）。管理介面操作維持一致模式。權限驗證 <100ms，分頁查詢 <500ms。

**Performance & Security Standards**: ✅ **PASS** - API 端點響應時間符合目標（權限驗證 <100ms、簡單操作 <200ms、複雜操作 <500ms、稽核日誌查詢 <2000ms）。所有 I/O 操作使用非同步模式。JWT 驗證整合到受保護端點。所有輸入使用 FluentValidation 驗證（權限代碼格式、角色名稱長度等）。敏感資訊不記錄日誌。資料庫查詢優化防止 N+1 問題。併發更新使用樂觀鎖定（Version 欄位）。軟刪除機制實作業務規則（防止刪除使用中的權限/角色）。權限驗證失敗記錄用於安全監控。

**Database Naming Convention**: ✅ **PASS** - 資料庫物件使用 snake_case（permissions、roles、user_roles、role_permissions、audit_logs、permission_failure_logs 表；permission_code、role_name、created_at 欄位；idx_permissions_code、fk_role_permissions_role_id 索引/約束）。C# 實體使用 PascalCase（PermissionCode, RoleName, CreatedAt 屬性）。Dapper 查詢明確映射命名規範。

**API Response Design**: ✅ **PASS** - 所有端點返回 ApiResponseModel<T> 結合 HTTP 狀態碼與業務邏輯碼。成功回應使用 CreateSuccess() 並搭配適當業務碼（SUCCESS, CREATED, UPDATED, DELETED）。錯誤回應使用 CreateFailure() 並搭配特定業務碼（VALIDATION_ERROR、PERMISSION_NOT_FOUND、PERMISSION_IN_USE、ROLE_NOT_FOUND、ROLE_IN_USE、USER_NOT_FOUND、DUPLICATE_PERMISSION_CODE、DUPLICATE_ROLE_NAME、INTERNAL_ERROR）。所有回應包含 TraceId。Controllers 實作 helper methods（Success, Created, NotFound, Conflict, BusinessError 等）。

**Audit & Compliance**: ✅ **PASS** - 稽核日誌完整記錄所有權限管理操作（權限/角色/用戶角色的新增/修改/刪除），包含操作者、時間、變更前後狀態、IP、UserAgent。稽核日誌僅可新增和查詢，不可修改或刪除，永久保留。權限驗證失敗記錄用於安全監控。

**Violation Justification**: ⚠️ **需要說明** - 本功能引入 5 個新的實體（Permission, Role, RolePermission, UserRole, AuditLog, PermissionFailureLog），複雜度略高於 constitution 預設的簡單架構。理由：RBAC 本質上是多對多關係（用戶-角色、角色-權限），需要獨立的關聯表和審計機制。簡化方案（如直接用戶-權限關聯）會失去角色抽象層的管理優勢，且無法滿足規格要求（User Story 2, 3, 6）。

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# C# ASP.NET Core 權限管理專案結構

Controllers/                    # Presentation Layer - API endpoints
├── PermissionController.cs     # 權限管理 CRUD API
├── RoleController.cs           # 角色管理與權限分配 API
├── UserRoleController.cs       # 用戶角色指派 API
├── AuditLogController.cs       # 稽核日誌查詢 API
└── BaseApiController.cs        # (已存在) 基礎控制器

Services/                       # Business Logic Layer  
├── PermissionService.cs        # 權限業務邏輯
├── RoleService.cs              # 角色業務邏輯
├── UserRoleService.cs          # 用戶角色指派業務邏輯
├── PermissionValidationService.cs  # 權限驗證邏輯（即時驗證）
├── AuditLogService.cs          # 稽核日誌記錄與查詢
├── Interfaces/
│   ├── IPermissionService.cs
│   ├── IRoleService.cs
│   ├── IUserRoleService.cs
│   ├── IPermissionValidationService.cs
│   └── IAuditLogService.cs
└── (已存在) JwtService.cs, AccountService.cs, AuthService.cs

Repositories/                   # Data Access Layer
├── PermissionRepository.cs     # 權限資料存取
├── RoleRepository.cs           # 角色資料存取
├── RolePermissionRepository.cs # 角色權限關聯資料存取
├── UserRoleRepository.cs       # 用戶角色關聯資料存取
├── AuditLogRepository.cs       # 稽核日誌資料存取（僅新增與查詢）
├── PermissionFailureLogRepository.cs # 權限驗證失敗記錄
├── Interfaces/
│   ├── IPermissionRepository.cs
│   ├── IRoleRepository.cs
│   ├── IRolePermissionRepository.cs
│   ├── IUserRoleRepository.cs
│   ├── IAuditLogRepository.cs
│   └── IPermissionFailureLogRepository.cs
└── (已存在) UserRepository.cs

Models/                         # Data models and DTOs
├── Entities/
│   ├── Permission.cs           # 權限實體
│   ├── Role.cs                 # 角色實體
│   ├── RolePermission.cs       # 角色權限關聯實體
│   ├── UserRole.cs             # 用戶角色關聯實體
│   ├── AuditLog.cs             # 稽核日誌實體
│   ├── PermissionFailureLog.cs # 權限驗證失敗記錄實體
│   └── (已存在) User.cs
├── Dtos/
│   ├── PermissionDto.cs        # 權限 DTO
│   ├── RoleDto.cs              # 角色 DTO
│   ├── RoleDetailDto.cs        # 角色詳細資訊（含權限列表）
│   ├── UserRoleDto.cs          # 用戶角色 DTO
│   ├── UserEffectivePermissionsDto.cs # 用戶有效權限 DTO（合併後）
│   ├── AuditLogDto.cs          # 稽核日誌 DTO
│   └── PermissionFailureLogDto.cs # 權限驗證失敗記錄 DTO
├── Requests/
│   ├── CreatePermissionRequest.cs    # 建立權限請求
│   ├── UpdatePermissionRequest.cs    # 更新權限請求
│   ├── DeletePermissionRequest.cs    # 刪除權限請求
│   ├── CreateRoleRequest.cs          # 建立角色請求
│   ├── UpdateRoleRequest.cs          # 更新角色請求
│   ├── DeleteRoleRequest.cs          # 刪除角色請求
│   ├── AssignRolePermissionsRequest.cs # 分配角色權限請求
│   ├── AssignUserRoleRequest.cs      # 指派用戶角色請求
│   ├── RemoveUserRoleRequest.cs      # 移除用戶角色請求
│   ├── ValidatePermissionRequest.cs  # 驗證權限請求
│   └── QueryAuditLogRequest.cs       # 查詢稽核日誌請求
├── Responses/
│   ├── PermissionResponse.cs         # 權限回應
│   ├── PermissionListResponse.cs     # 權限列表回應
│   ├── RoleResponse.cs               # 角色回應
│   ├── RoleListResponse.cs           # 角色列表回應
│   ├── RoleDetailResponse.cs         # 角色詳細資訊回應
│   ├── UserRoleResponse.cs           # 用戶角色回應
│   ├── UserEffectivePermissionsResponse.cs # 用戶有效權限回應
│   ├── AuditLogListResponse.cs       # 稽核日誌列表回應
│   └── PermissionValidationResponse.cs # 權限驗證結果回應
├── (已存在) ApiResponseModel.cs, ResponseCodes.cs
└── (已存在) AccountDto.cs, LoginDto.cs...

Validators/                     # FluentValidation validators
├── CreatePermissionRequestValidator.cs
├── UpdatePermissionRequestValidator.cs
├── CreateRoleRequestValidator.cs
├── UpdateRoleRequestValidator.cs
├── AssignRolePermissionsRequestValidator.cs
├── AssignUserRoleRequestValidator.cs
└── QueryAuditLogRequestValidator.cs

Middleware/                     # (已存在) 中介軟體
├── ExceptionHandlingMiddleware.cs
├── TraceIdMiddleware.cs
└── PermissionAuthorizationMiddleware.cs # (新增) 權限驗證中介軟體

Database/
├── Migrations/
│   ├── (已存在) 001_CreateUsersTable.sql
│   ├── 002_CreatePermissionsTable.sql     # 建立權限表
│   ├── 003_CreateRolesTable.sql           # 建立角色表
│   ├── 004_CreateRolePermissionsTable.sql # 建立角色權限關聯表
│   ├── 005_CreateUserRolesTable.sql       # 建立用戶角色關聯表
│   ├── 006_CreateAuditLogsTable.sql       # 建立稽核日誌表
│   └── 007_CreatePermissionFailureLogsTable.sql # 建立權限驗證失敗記錄表
└── Scripts/
    └── seed_permissions.sql               # 初始權限數據種子

Tests/                          # Test projects
├── Unit/
│   ├── Services/
│   │   ├── PermissionServiceTests.cs
│   │   ├── RoleServiceTests.cs
│   │   ├── UserRoleServiceTests.cs
│   │   ├── PermissionValidationServiceTests.cs
│   │   └── AuditLogServiceTests.cs
│   └── Repositories/
│       ├── PermissionRepositoryTests.cs
│       ├── RoleRepositoryTests.cs
│       └── AuditLogRepositoryTests.cs
└── Integration/
    ├── PermissionControllerIntegrationTests.cs
    ├── RoleControllerIntegrationTests.cs
    ├── UserRoleControllerIntegrationTests.cs
    ├── PermissionValidationIntegrationTests.cs
    └── AuditLogControllerIntegrationTests.cs
```

**Structure Decision**: 採用 C# ASP.NET Core 三層式架構，Controllers（Presentation）負責 API 端點、Services（Business Logic）負責業務邏輯與權限驗證、Repositories（Data Access）負責資料持久化。所有介面統一放置在各自的 Interfaces 資料夾中，維持清晰的關注點分離。權限驗證服務（PermissionValidationService）獨立於角色服務，支援即時權限驗證且不依賴快取。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 6 個新實體（Permission, Role, RolePermission, UserRole, AuditLog, PermissionFailureLog） | RBAC 機制本質上需要多對多關係模型。用戶與角色是多對多（一個用戶可有多角色，一個角色可指派給多用戶）。角色與權限也是多對多（一個角色可有多權限，一個權限可屬於多角色）。稽核日誌和失敗記錄是合規與安全監控的必要需求（FR-019~FR-024, FR-031）。 | **方案 A（直接用戶-權限關聯）**：移除角色層，直接為用戶分配權限。被拒絕原因：失去角色抽象層的管理優勢，無法批量管理用戶權限，不符合 User Story 2 和 3 的需求（角色是簡化權限管理的核心概念）。管理員需要逐一為每個用戶配置權限，管理成本極高。<br><br>**方案 B（硬編碼角色）**：使用固定的角色枚舉（如 Admin, User, Manager），不支援自訂角色。被拒絕原因：不符合 FR-006~FR-010 的需求（系統必須允許管理員新增、修改、刪除角色）。無法滿足不同組織的自訂權限需求，擴展性差。<br><br>**方案 C（合併關聯表）**：將 RolePermission 和 UserRole 合併為單一表。被拒絕原因：違反正規化原則，資料冗餘嚴重。當角色權限變更時需要更新所有用戶記錄，效能差且容易出錯。無法追蹤角色權限的歷史變更。 |
| 獨立的權限驗證服務（PermissionValidationService） | 權限驗證邏輯複雜（需合併多角色權限、即時驗證最新配置、記錄失敗嘗試），與角色管理業務邏輯分離可提高可測試性和可維護性。符合單一職責原則。 | **合併到 RoleService**：將權限驗證邏輯放入 RoleService。被拒絕原因：RoleService 負責角色 CRUD，加入驗證邏輯會違反單一職責原則。權限驗證是高頻操作（每次請求都需驗證），與低頻的角色管理混在一起會影響程式碼可讀性和測試獨立性。 |
