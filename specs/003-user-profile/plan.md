# Implementation Plan: 用戶個人資料查詢 API

**Branch**: `003-user-profile` | **Date**: 2025-11-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-user-profile/spec.md`
**Language**: This plan MUST be written in Traditional Chinese (zh-TW) per constitution requirements

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

新增一個 API 端點，允許已登入用戶查詢自己的個人資料。API 將回傳用戶名稱 (username)、顯示名稱 (displayname) 和角色名稱清單 (roles)。採用 RESTful 設計，使用 JWT 身份驗證，並遵循現有的 ApiResponseModel 統一回應格式。此功能為前端顯示用戶資訊的基礎能力。

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: 
- ASP.NET Core 9.0
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0
- Dapper 2.1 (資料存取)
- FluentValidation 11.0 (輸入驗證)
- BCrypt.Net-Next 4.0 (密碼雜湊)

**Storage**: PostgreSQL (現有資料庫，已有 users、roles、user_roles 表)
**Testing**: xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing
**Target Platform**: Cross-platform (Windows/Linux/macOS)
**Project Type**: Web API - ASP.NET Core backend service for v3-admin-frontend
**Performance Goals**: 
- 查詢個人資料：<1000ms (目標 <200ms)
- 並發支援：1000+ 並發請求

**Constraints**: 
- 必須使用 JWT Bearer 身份驗證
- 必須使用 ApiResponseModel 統一回應格式
- 錯誤訊息必須使用繁體中文
- 必須包含 TraceId 用於分散式追蹤
- 停用帳號無法通過 token 驗證
- 不記錄稽核日誌（避免過多記錄）

**Scale/Scope**: 單一端點功能，讀取現有 users 和 user_roles 表資料

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Code Quality Excellence**: ✅ 
- 遵循 C# 13 最佳實踐
- 使用 XML 文件註解（繁體中文）
- 資料庫使用 snake_case，C# 實體使用 PascalCase
- 使用 nullable reference types

**Three-Layer Architecture**: ✅ 
- Controller: AccountController（已存在，新增端點）
- Service: 使用現有 IAccountService 或新增 IUserProfileService
- Repository: UserRepository（已存在）和 UserRoleRepository（已存在）

**Database Design & Foreign Key Integrity**: ✅ 
- 使用現有資料表，無需新增外鍵約束
- 查詢現有 users、roles、user_roles 表

**Permission-Based Authorization**: ✅ 
- 需定義權限：`user.profile.read`（查詢個人資料）
- 使用 `[RequirePermission("user.profile.read")]` 屬性
- 需在 `seed_permissions.sql` 中新增權限定義

**Test-First Development**: ✅ 
- 單元測試：Service 層業務邏輯
- 整合測試：API 端點、JWT 驗證、權限驗證

**User Experience Consistency**: ✅ 
- 使用 ApiResponseModel<UserProfileResponse>
- HTTP 200 OK + SUCCESS 業務代碼（成功）
- HTTP 401 Unauthorized + UNAUTHORIZED（未驗證）
- HTTP 403 Forbidden + FORBIDDEN（無權限）
- 繁體中文錯誤訊息
- 包含 TraceId

**Performance & Security**: ✅ 
- 目標回應時間 <200ms
- JWT 身份驗證（使用現有機制）
- 從 JWT claims 取得 user ID
- 停用帳號在 token 驗證階段拒絕
- 不記錄稽核日誌

**Violations**: 無

## Project Structure

### Documentation (this feature)

```text
specs/003-user-profile/
├── spec.md              # 功能規格（已完成）
├── plan.md              # 本檔案（實作計劃）
├── research.md          # Phase 0 輸出（技術研究）
├── data-model.md        # Phase 1 輸出（資料模型）
├── quickstart.md        # Phase 1 輸出（快速開始指南）
├── contracts/           # Phase 1 輸出（API 合約）
│   └── user-profile-api.yaml
└── tasks.md             # Phase 2 輸出（/speckit.tasks 指令）
```

### Source Code (repository root)

```text
Controllers/
├── AccountController.cs      # 新增 GetMyProfile 端點

Services/
├── AccountService.cs         # 新增 GetUserProfileAsync 方法
├── Interfaces/
    └── IAccountService.cs    # 新增介面方法定義

Repositories/
├── UserRepository.cs         # 使用現有 GetUserByIdAsync
├── UserRoleRepository.cs     # 使用現有 GetRolesByUserIdAsync
├── Interfaces/
    └── IUserRepository.cs    # 現有介面
    └── IUserRoleRepository.cs # 現有介面

Models/
├── Responses/
│   └── UserProfileResponse.cs # 新增回應 DTO
├── Entities/
│   ├── User.cs               # 現有實體
│   └── Role.cs               # 現有實體

Database/Scripts/
└── seed_permissions.sql      # 新增 user.profile.read 權限

Tests/
├── Unit/
│   └── AccountServiceTests.cs # 新增單元測試
└── Integration/
    └── AccountControllerTests.cs # 新增整合測試
```

**Structure Decision**: 
- 使用現有的 AccountController，因為此功能屬於帳號管理範疇
- 重用現有的 UserRepository 和 UserRoleRepository
- 遵循專案既有的三層架構模式
- 新增最小化的程式碼（1 個端點、1 個服務方法、1 個 DTO）

## Complexity Tracking

> 無需填寫 - 所有 Constitution Check 項目皆已通過，無違規需要說明
