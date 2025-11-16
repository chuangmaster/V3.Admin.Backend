# Implementation Tasks 實施任務清單：權限模型重構

**Branch**: `004-permission-refactor` | **Date**: 2025-11-16  
**Specification**: spec.md | **Design**: data-model.md  
**Language**: Traditional Chinese (zh-TW)

---

## 任務概述

本文件按用戶故事組織所有實施任務，每個用戶故事都是獨立可測試的增量。

**任務統計**:
- **總數**: 49 項
- **P0 (Critical)**: 10 項 - 必須優先完成
- **P1 (High)**: 22 項 - 核心功能
- **P2 (Medium)**: 13 項 - 增強功能
- **P3 (Low)**: 4 項 - 優化項目
- **總工作量**: ~34 小時

**用戶故事**:
1. **US1** (P1): 移除 RoutePath 欄位 - ~12 小時
2. **US2** (P1): 支援 View 權限類型 - ~10 小時
3. **US3** (P2): 統一編碼規範驗證 - ~8 小時

**推薦 MVP 範圍**: US1 (移除 RoutePath) - ~12 小時

---

## Phase 1: Setup 初始設置

### Project Setup & Configuration

- [ ] T001 [P2] 驗證開發環境 (.NET 9, PostgreSQL, Visual Studio)
- [ ] T002 [P0] 檢查 NuGet 依賴: FluentValidation, Dapper, xUnit `V3.Admin.Backend.csproj`
- [ ] T003 [P2] 確認功能分支已建立 `004-permission-refactor`

---

## Phase 2: Foundational 基礎層 (所有用戶故事的先決條件)

**完成此 Phase 後，所有用戶故事可獨立進行**

### Database & Entities

- [ ] T004 [P0] 建立 EF Core 遷移移除 RoutePath `Database/Migrations/*_RemoveRoutePath.cs`
- [ ] T005 [P0] 執行資料庫遷移並驗證成功 `dotnet ef database update`
- [ ] T006 [P0] 新增 PermissionType Enum `Models/Entities/PermissionType.cs`

### Core Services

- [ ] T007 [P0] 定義 IPermissionValidationService 介面 `Services/Interfaces/IPermissionValidationService.cs`
- [ ] T008 [P1] [P] 建立 IPermissionRepository 新方法簽名 `Repositories/Interfaces/IPermissionRepository.cs`
- [ ] T009 [P1] [P] 在 Program.cs 註冊所有相關服務 `Program.cs`

### Seed Data

- [ ] T010 [P0] 建立權限種子資料指令碼 `Database/Scripts/seed_permissions.sql`
- [ ] T011 [P1] 執行種子資料初始化 `dotnet ef database update`

---

## Phase 3: User Story 1 - 移除 RoutePath 欄位 (Priority: P1)

**目標**: 權限管理者不需提供 RoutePath，系統從 PermissionCode 推導路由資訊

**獨立測試準則**:
- [ ] 新權限能建立（無 RoutePath）
- [ ] 現有權限能編輯（無 RoutePath）
- [ ] 權限驗證邏輯使用 PermissionCode
- [ ] API 端點測試通過率 100%

**相關端點**: 
- POST `/api/permissions` - 建立
- PUT `/api/permissions/{id}` - 編輯
- DELETE `/api/permissions/{id}` - 刪除
- GET `/api/permissions` - 查詢列表

### Data Layer (US1)

- [ ] T012 [P0] [US1] 移除 Permission.RoutePath 屬性 `Models/Entities/Permission.cs`
- [ ] T013 [P0] [US1] [P] 更新 PermissionRepository 移除 RoutePath 查詢 `Repositories/PermissionRepository.cs`
- [ ] T014 [P0] [US1] [P] 新增 GetByCodeAsync() 方法 `Repositories/PermissionRepository.cs`
- [ ] T015 [P0] [US1] [P] 新增 IsCodeUniqueAsync() 唯一性檢查 `Repositories/PermissionRepository.cs`

### Business Layer (US1)

- [ ] T016 [P1] [US1] 實現 PermissionValidationService `Services/PermissionValidationService.cs`
- [ ] T017 [P1] [US1] [P] 更新 PermissionService 移除 RoutePath 邏輯 `Services/PermissionService.cs`
- [ ] T018 [P1] [US1] 新增 PermissionCode 格式驗證正則 `Services/PermissionValidationService.cs`

### Presentation Layer (US1)

- [ ] T019 [P0] [US1] [P] 移除 CreatePermissionRequest.RoutePath `Models/Requests/CreatePermissionRequest.cs`
- [ ] T020 [P0] [US1] [P] 移除 UpdatePermissionRequest.RoutePath `Models/Requests/UpdatePermissionRequest.cs`
- [ ] T021 [P0] [US1] [P] 更新 PermissionResponse 移除 RoutePath `Models/Responses/PermissionResponse.cs`
- [ ] T022 [P1] [US1] 更新 CreatePermissionRequestValidator `Validators/CreatePermissionRequestValidator.cs`
- [ ] T023 [P1] [US1] [P] 更新 UpdatePermissionRequestValidator `Validators/UpdatePermissionRequestValidator.cs`

### Controller & Endpoints (US1)

- [ ] T024 [P1] [US1] 更新 PermissionController POST 端點 `Controllers/PermissionController.cs`
- [ ] T025 [P1] [US1] 更新 PermissionController PUT 端點 `Controllers/PermissionController.cs`
- [ ] T026 [P1] [US1] 更新 PermissionController DELETE 端點 `Controllers/PermissionController.cs`
- [ ] T027 [P2] [US1] 更新 PermissionController GET 端點 `Controllers/PermissionController.cs`

### Testing (US1)

- [ ] T028 [P1] [US1] 單元測試: PermissionValidationService `Tests/Unit/Services/PermissionValidationServiceTests.cs`
- [ ] T029 [P1] [US1] 整合測試: Permission API `Tests/Integration/Controllers/PermissionControllerTests.cs`

**US1 驗收檢查**:
- [ ] Permission 實體編譯無誤
- [ ] 所有 RoutePath 引用已移除 (grep 確認)
- [ ] PermissionCode 驗證率 100%
- [ ] API 端點測試通過率 100%
- [ ] 資料庫遷移成功

---

## Phase 4: User Story 2 - 支援 View 權限類型 (Priority: P1)

**目標**: 系統支援 view 型別權限，用於控制 UI 元件顯示/隱藏

**獨立測試準則**:
- [ ] view 型別權限能建立
- [ ] 能查詢用戶 view 權限
- [ ] 權限檢查端點返回正確結果
- [ ] 中介軟體執行授權檢查

**相關端點**:
- GET `/api/permissions/check/{permissionCode}` - 檢查權限
- POST `/api/roles/{roleId}/permissions` - 分配角色權限

### Data Models (US2)

- [ ] T030 [P1] [US2] [P] 新增 CheckPermissionResponse DTO `Models/Responses/CheckPermissionResponse.cs`
- [ ] T031 [P2] [US2] [P] 新增 AssignPermissionsRequest DTO `Models/Requests/AssignPermissionsRequest.cs`

### Business Layer (US2)

- [ ] T032 [P1] [US2] 新增 HasPermissionTypeAsync() 方法 `Services/PermissionValidationService.cs`
- [ ] T033 [P2] [US2] [P] 新增角色權限查詢方法 `Services/RoleService.cs`

### Middleware (US2)

- [ ] T034 [P1] [US2] 確認 PermissionAuthorizationMiddleware 使用 PermissionCode `Middleware/PermissionAuthorizationMiddleware.cs`

### Controller & Endpoints (US2)

- [ ] T035 [P1] [US2] 新增 GET /permissions/check/{code} 端點 `Controllers/PermissionController.cs`
- [ ] T036 [P2] [US2] 新增 POST /roles/{id}/permissions 端點 `Controllers/RoleController.cs`

### Testing (US2)

- [ ] T037 [P2] [US2] 單元測試: HasPermissionTypeAsync `Tests/Unit/Services/PermissionValidationServiceTests.cs`
- [ ] T038 [P2] [US2] 整合測試: 權限檢查端點 `Tests/Integration/Controllers/PermissionControllerTests.cs`

**US2 驗收檢查**:
- [ ] view 型別權限能正確儲存
- [ ] 權限檢查端點返回正確值
- [ ] 中介軟體授權檢查通過
- [ ] 無編譯錯誤

---

## Phase 5: User Story 3 - 統一編碼規範驗證 (Priority: P2)

**目標**: 所有 PermissionCode 遵循統一編碼規範，系統驗證並拒絕不符合規範的代碼

**獨立測試準則**:
- [ ] 有效代碼通過驗證
- [ ] 無效代碼被拒絕
- [ ] 多層級資源格式支持
- [ ] 驗證錯誤訊息清晰

**編碼規範**: `resource.action` 或 `resource.subresource.action`

### Validation Rules (US3)

- [ ] T039 [P2] [US3] 完善 ValidatePermissionCode 正則表達式 `Services/PermissionValidationService.cs`
- [ ] T040 [P2] [US3] [P] 新增 PermissionCodeValidator 類 `Validators/PermissionCodeValidator.cs`

### Audit & Logging (US3)

- [ ] T041 [P3] [US3] 更新 PermissionFailureLog 記錄 `Repositories/PermissionFailureLogRepository.cs`

### Testing (US3)

- [ ] T042 [P2] [US3] 單元測試: PermissionCode 格式 `Tests/Unit/Validators/PermissionCodeValidatorTests.cs`
- [ ] T043 [P3] [US3] 整合測試: 驗證錯誤 `Tests/Integration/Controllers/PermissionControllerTests.cs`

**US3 驗收檢查**:
- [ ] PermissionCode 驗證率 100%
- [ ] 所有測試通過
- [ ] 錯誤訊息使用繁體中文
- [ ] 無編譯警告

---

## Phase 6: Polish 優化與交叉關注

### Documentation & Cleanup

- [ ] T044 [P3] 更新 README.md 移除 RoutePath 說明 `README.md`
- [ ] T045 [P3] [P] 代碼清理: grep 檢查零 RoutePath 參考 `.`
- [ ] T046 [P3] 更新 API 文件 `specs/V3.Admin.Backend.API.yaml`

### Final Validation

- [ ] T047 [P2] 執行靜態代碼分析 `.`
- [ ] T048 [P2] 代碼審查: 所有更改已批准
- [ ] T049 [P2] 最終編譯驗證: 無誤警告 `V3.Admin.Backend.csproj`

---

## 並行執行機會

### 優先級並行
- **P0 任務** (T004-T007, T012-T015, T019-T021): 不能並行（順序依賴）
- **US1 中 [P] 標記的任務**: 可與其他 [P] 並行
  - T013, T014, T015 (Repository 方法) 可並行
  - T019, T020, T021 (DTO 更新) 可並行

### 用戶故事並行
- **US1 完成後**: US2 和 US3 可獨立並行執行
- **跨故事無依賴**: 建議順序執行保持進度清晰

---

## 依賴與完成順序

```
T001-T003 (Setup)
    ↓
T004-T011 (Foundational - 必須)
    ├─→ T012-T029 (US1 移除 RoutePath) → 可並行 [P] 任務
    │   ↓
    ├─→ T030-T038 (US2 支援 View) → 可與 US3 並行
    │   ↓
    └─→ T039-T043 (US3 統一規範) → 可與 US2 並行
        ↓
    T044-T049 (Polish)
```

---

## 實施建議

### MVP 優先 (第一版)
**目標**: ~12 小時，完成移除 RoutePath

1. T001-T011 (Setup + Foundational) - 4h
2. T012-T029 (US1 完整) - 8h

**交付物**: 
- Permission API 無 RoutePath
- 100% 測試通過
- 資料庫正確遷移

### 增量交付
**第二版** (+ US2): 再 10h
**第三版** (+ US3): 再 8h

---

## 驗收標準檢查表

- [ ] 所有 49 項任務完成 ✅
- [ ] 單元測試覆蓋 > 80% (PermissionService, PermissionValidationService)
- [ ] 整合測試通過率 100%
- [ ] RoutePath 代碼參考 = 0 (完全移除)
- [ ] 資料庫遷移成功執行
- [ ] API 合約與實現一致
- [ ] 編譯無誤和警告
- [ ] 代碼審查批准
- [ ] 文件更新完整

---

## 命令參考

```bash
# 檢查 RoutePath 引用
grep -r "RoutePath" --include="*.cs" .

# 執行單元測試
dotnet test Tests/V3.Admin.Backend.Tests.csproj -l:trx

# 執行整合測試
dotnet test Tests/V3.Admin.Backend.Tests.csproj --filter "Integration" -l:trx

# 資料庫遷移
dotnet ef database update -p V3.Admin.Backend.csproj

# 代碼分析
dotnet analyze V3.Admin.Backend.csproj

# 編譯驗證
dotnet build V3.Admin.Backend.csproj --configuration Release
```

---

## 時程表

| Phase | 任務 | 工時 | 開始 | 完成 |
|-------|------|------|------|------|
| 1 | T001-T003 | 1.5h | Day 1 | Day 1 |
| 2 | T004-T011 | 4h | Day 1 | Day 1 |
| 3 | T012-T029 | 12h | Day 2 | Day 3 |
| 4 | T030-T038 | 10h | Day 3 | Day 4 |
| 5 | T039-T043 | 4h | Day 4 | Day 4 |
| 6 | T044-T049 | 2.5h | Day 5 | Day 5 |
| 總計 | 49 tasks | ~34h | - | - |

**推薦速度**: 每天 8-10 小時 (考慮代碼審查時間)
