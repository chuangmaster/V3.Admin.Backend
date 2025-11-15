# Implementation Plan: 權限模型重構 - 移除 RoutePath、整合路由決策至 PermissionCode

**Branch**: `004-permission-refactor` | **Date**: 2025-11-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-permission-refactor/spec.md`
**Language**: Traditional Chinese (zh-TW)

**Status**: ✅ Phase 0, 1, 2 Complete - Ready for Implementation

## Summary

本重構旨在簡化權限模型，將目前在 `RoutePath` 欄位中管理的路由資訊移至 `PermissionCode` 中，並擴展 `PermissionType` 以支持 `view` 權限（區塊瀏覽權限）。完成此重構後，系統將:

1. **移除 RoutePath**: 完全刪除 RoutePath 欄位，使用 PermissionCode 直接決定路由和 UI 元件權限
2. **支持 Function 和 View**: Function 代表操作權限（按鈕），View 代表 UI 區塊瀏覽權限（面板/小工具）
3. **統一編碼規範**: 所有權限遵循 `resource.action` 或 `resource.subresource.action` 格式
4. **預留擴展機制**: 架構設計支持未來新增其他 PermissionType（如 Report, Api 等）
5. **保證性能**: 權限驗證性能維持 ≤50ms

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: ASP.NET Core 9, Microsoft.AspNetCore.Authentication.JwtBearer, FluentValidation, Dapper
**Storage**: PostgreSQL (recommended for user management)
**Testing**: xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing
**Target Platform**: Cross-platform (Windows/Linux/macOS)
**Project Type**: Web API - ASP.NET Core backend service for v3-admin-frontend
**Performance Goals**: <200ms simple operations (查詢權限), <2000ms complex operations (分頁查詢)
**Constraints**: 
- JWT authentication required (1-hour expiration)
- ApiResponseModel for all responses (HTTP status + business codes)
- Traditional Chinese error messages
- Role-based authorization via PermissionCode
- TraceId for distributed tracing
- RoutePath completely removed
- PermissionType supports: Function (操作), View (UI 區塊瀏覽)
**Scale/Scope**: Permission management subsystem for v3-admin backend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Code Quality Excellence**: ✅ C# 13 best practices, XML documentation, Traditional Chinese comments, Database naming (snake_case for DB tables/columns, PascalCase for C# entities)
**Three-Layer Architecture**: ✅ Controllers/Services/Repositories separation maintained
**Test-First Development**: ✅ Tests written before implementation, critical path coverage
**User Experience Consistency**: ✅ ApiResponseModel usage with HTTP status + business codes, standardized error handling, Traditional Chinese messages
**Performance & Security**: ✅ <200ms simple operations, JWT authentication, role/permission validation, input validation
**User Management Context**: ✅ Role-based access control, permission management, admin interface standards
**API Response Design**: ✅ Dual-layer design (HTTP status + business code), TraceId for distributed tracing

## Project Structure

### Documentation (此功能相關文件)

```text
specs/004-permission-refactor/
├── spec.md              # ✅ 功能規格書（已存在）
├── plan.md              # ✅ 實施計劃（本文件）
├── research.md          # ✅ Phase 0 - 研究報告（已完成）
├── data-model.md        # ✅ Phase 1 - 資料模型設計（已完成）
├── quickstart.md        # ✅ Phase 1 - 快速開始指南（已完成）
├── tasks.md             # ✅ Phase 2 - 實施任務清單（已完成）
└── contracts/
    └── permission-api.yaml  # ✅ Phase 1 - OpenAPI 3.0 規範（已完成）
```

### 源代碼結構

```text
V3.Admin.Backend/
├── Models/
│   ├── Entities/
│   │   └── Permission.cs        # ← 移除 RoutePath 屬性
│   ├── Requests/
│   │   ├── CreatePermissionRequest.cs    # ← 移除 RoutePath
│   │   └── UpdatePermissionRequest.cs    # ← 移除 RoutePath
│   └── Responses/
│       ├── PermissionResponse.cs         # ← 移除 RoutePath
│       └── CheckPermissionResponse.cs    # ← 新增（檢查權限回應）
├── Controllers/
│   └── PermissionController.cs   # ← 新增 /check/{code} 端點
├── Services/
│   ├── PermissionService.cs      # ← 更新邏輯
│   ├── PermissionValidationService.cs   # ← 新增/更新
│   └── Interfaces/
│       ├── IPermissionService.cs
│       └── IPermissionValidationService.cs
├── Repositories/
│   ├── PermissionRepository.cs   # ← 移除 RoutePath 查詢
│   ├── PermissionFailureLogRepository.cs  # ← 更新日誌記錄
│   └── Interfaces/
│       └── IPermissionRepository.cs
├── Validators/
│   ├── CreatePermissionRequestValidator.cs   # ← 移除 RoutePath 驗證
│   └── UpdatePermissionRequestValidator.cs   # ← 移除 RoutePath 驗證
├── Middleware/
│   └── PermissionAuthorizationMiddleware.cs  # ← 確認使用 PermissionCode
├── Database/
│   ├── Migrations/
│   │   └── [timestamp]_RemoveRoutePath.cs   # ← 新增遷移
│   └── Scripts/
│       └── seed_permissions.sql             # ← 更新種子資料
└── Tests/
    ├── Unit/
    │   └── Services/
    │       └── PermissionValidationServiceTests.cs
    └── Integration/
        └── Controllers/
            └── PermissionControllerTests.cs
```

## Complexity Tracking

> **本重構無憲法原則違反** - 所有設計遵循 Constitution Principle 要求

| 項目 | 決策 | 理由 |
|------|------|------|
| Enum vs DB Table | 混合方案（短期 Enum，長期可遷移至 DB） | 初期性能優先，預留未來擴展機制 |
| PermissionCode 驗證 | 靈活格式 + 規範驗證 | 支持多層級資源，無硬格式限制 |
| RoutePath 移除 | 立即完全刪除 | 當前系統無 RoutePath 資料，無遷移成本 |
| 權限驗證邏輯 | PermissionCode 直接決策 | 簡化授權檢查，性能更優 |

## 實施交付物清單

✅ **Phase 0 已完成**
- [x] research.md - 所有設計決策的研究報告
- [x] 澄清了 PermissionType 擴展機制
- [x] 確定 PermissionCode 編碼規範
- [x] 制定資料庫遷移策略

✅ **Phase 1 已完成**
- [x] data-model.md - 完整的資料實體設計
- [x] contracts/permission-api.yaml - OpenAPI 3.0 規範
- [x] quickstart.md - 前後端開發快速指南
- [x] 代理上下文已更新（GitHub Copilot）

✅ **Phase 2 已完成**
- [x] tasks.md - 25 個詳細實施任務（共 ~32 小時工作量）
  - 8 個 P0 Critical 任務
  - 12 個 P1 High 任務
  - 3 個 P2 Medium 任務
  - 2 個 P3 Low 任務

## 後續步驟（實施準備）

1. **環境準備**
   - [ ] 本地開發環境已設置（.NET 9, PostgreSQL）
   - [ ] 專案文件已備份

2. **開始實施**
   - [ ] 開發者審閱 tasks.md 中的任務清單
   - [ ] 按優先級依序實施任務（P0 → P1 → P2 → P3）
   - [ ] 每個 Phase 完成後進行代碼審查

3. **質量保證**
   - [ ] 執行單元測試（Task 2.7.1）
   - [ ] 執行整合測試（Task 2.7.2）
   - [ ] 執行資料庫遷移測試（Task 2.7.4）
   - [ ] 最終代碼審查與批准

4. **部署準備**
   - [ ] 更新部署文件
   - [ ] 準備生產環境遷移指令碼
   - [ ] 通知相關利益相關者

## 相關文件參考

- **Specification**: ./spec.md - 完整需求說明
- **Research**: ./research.md - 所有設計決策背景
- **Data Model**: ./data-model.md - 詳細實體與 DTO 設計
- **API Contract**: ./contracts/permission-api.yaml - 完整 API 規範
- **Tasks**: ./tasks.md - 實施任務詳細清單
- **Quickstart**: ./quickstart.md - 開發者快速開始指南
- **Constitution**: ../../.specify/memory/constitution.md - 專案開發指南
- **Copilot Instructions**: ../../.github/copilot-instructions.md - Copilot 上下文
