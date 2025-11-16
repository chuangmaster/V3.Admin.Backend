````markdown
# Implementation Tasks 實施任務清單：權限模型重構 - 移除 RoutePath、整合路由決策至 PermissionCode

**Branch**: `004-permission-refactor` | **Date**: 2025-11-16  
**Status**: ✅ Ready for Implementation  
**Language**: Traditional Chinese (zh-TW)

---

## 任務總覽

**總任務數**: 32 項  
**Phase 1 (Setup)**: 3 項  
**Phase 2 (Foundational)**: 6 項  
**Phase 3 (User Story 1)**: 10 項  
**Phase 4 (User Story 2)**: 6 項  
**Phase 5 (User Story 3)**: 4 項  
**Phase 6 (Polish)**: 3 項  

**總工作量估計**: ~32 小時  
**建議 MVP 範圍**: User Story 1 (移除 RoutePath) → ~10 小時

---

## 用戶故事與優先級

| 故事 | 標題 | 優先級 | 獨立可測 | 依賴 |
|------|------|--------|--------|------|
| US1 | 移除 RoutePath 欄位 | P1 | ✅ 是 | 無 |
| US2 | 支援 View 權限類型 | P1 | ✅ 是 | 無 |
| US3 | 統一編碼規範驗證 | P2 | ✅ 是 | 無 |

---

## 並行執行機會

### 初期並行 (Phase 1 + 2)
- Phase 1 Setup 可與 Phase 2 Foundational 並行

### 用戶故事並行
- US1, US2, US3 可以部分並行執行（不同的文件和組件）
- 建議順序: US1 → US2 → US3 (按優先級)

### 組件級並行 (Phase 3 內)
- [P] 標記的任務可並行執行

---

## Phase 1: Setup 初始設置

### 項目結構和依賴配置

- [ ] T001 檢查開發環境（.NET 9, PostgreSQL, Visual Studio）
- [ ] T002 驗證分支和代碼風格檢查配置 `004-permission-refactor`
- [ ] T003 確認相關 NuGet 套件已安裝 (FluentValidation, Dapper, xUnit)

---

## Phase 2: Foundational 基礎層（阻塞性先決條件）

**完成此 Phase 後，所有用戶故事可以獨立開始**

### 資料庫遷移基礎

- [ ] T004 建立 EF Core 遷移檔案移除 `route_path` `Database/Migrations/`
- [ ] T005 執行資料庫遷移驗證 `RoutePath` 欄位移除成功
- [ ] T006 建立資料庫種子指令碼 `Database/Scripts/seed_permissions.sql`

### 核心服務基礎

- [ ] T007 新增 PermissionType Enum `Models/Entities/PermissionType.cs`
- [ ] T008 實現 IPermissionValidationService 介面 `Services/Interfaces/IPermissionValidationService.cs`
- [ ] T009 [P] 在 DI 容器註冊所有服務 `Program.cs`

**Phase 2 驗收標準**:
- [ ] 資料庫遷移成功執行
- [ ] 種子資料正確插入
- [ ] 所有介面和 Enum 已定義
- [ ] 項目編譯無誤

---

## Phase 3: User Story 1 - 移除 RoutePath 欄位 (Priority: P1)

**故事目標**: 權限管理者不需要在建立或編輯權限時提供 RoutePath，系統從 PermissionCode 推導路由資訊

**獨立測試準則**:
- 新權限能成功建立，無需 RoutePath
- 權限可成功編輯，無需 RoutePath
- 舊格式的權限能正確遷移
- 權限驗證邏輯使用 PermissionCode

**相關文件**: 
- API 端點: POST/PUT `/api/permissions` (contracts/permission-api.yaml)
- 資料模型: Permission 實體 (data-model.md)

### 資料層 (US1)

- [ ] T010 [US1] 移除 Permission 實體 RoutePath 屬性 `Models/Entities/Permission.cs`
- [ ] T011 [US1] [P] 更新 PermissionRepository 移除 RoutePath 查詢 `Repositories/PermissionRepository.cs`
- [ ] T012 [US1] [P] 更新 IPermissionRepository 介面 `Repositories/Interfaces/IPermissionRepository.cs`

### 業務邏輯層 (US1)

- [ ] T013 [US1] 實現 PermissionValidationService `Services/PermissionValidationService.cs`
- [ ] T014 [US1] [P] 更新 PermissionService 移除 RoutePath 邏輯 `Services/PermissionService.cs`
- [ ] T015 [US1] 實現 PermissionCode 格式驗證方法 `Services/PermissionService.cs`

### 展示層與驗證 (US1)

- [ ] T016 [US1] [P] 更新 CreatePermissionRequest DTO `Models/Requests/CreatePermissionRequest.cs`
- [ ] T017 [US1] [P] 更新 UpdatePermissionRequest DTO `Models/Requests/UpdatePermissionRequest.cs`
- [ ] T018 [US1] 更新 CreatePermissionRequestValidator `Validators/CreatePermissionRequestValidator.cs`
- [ ] T019 [US1] [P] 更新 PermissionController 端點 `Controllers/PermissionController.cs`

### 測試 (US1)

- [ ] T020 [US1] 單元測試: PermissionValidationService `Tests/Unit/Services/PermissionValidationServiceTests.cs`

**US1 驗收檢查**:
- [ ] 所有 RoutePath 相關代碼已移除 (grep 確認)
- [ ] Permission API 端點測試通過 100%
- [ ] 資料庫遷移正確執行
- [ ] 編譯無誤和警告

---

## Phase 4: User Story 2 - 支援 View 權限類型 (Priority: P1)

**故事目標**: 系統支援 `view` 權限類型，用於控制 UI 元件或頁面區塊的顯示/隱藏

**獨立測試準則**:
- 能建立 view 類型權限
- 能查詢 view 權限並驗證
- 前端能檢查用戶是否有 view 權限
- 角色能分配 view 權限

**相關文件**:
- API 端點: GET `/api/permissions/check/{code}` (contracts/permission-api.yaml)
- DTO: CheckPermissionResponse (data-model.md)

### 業務邏輯層 (US2)

- [ ] T021 [US2] 新增 HasPermissionTypeAsync 方法 `Services/PermissionValidationService.cs`
- [ ] T022 [US2] [P] 為角色新增權限查詢方法 `Repositories/RolePermissionRepository.cs`

### 展示層 (US2)

- [ ] T023 [US2] [P] 新增 CheckPermissionResponse DTO `Models/Responses/CheckPermissionResponse.cs`
- [ ] T024 [US2] 新增 GET /permissions/check/{code} 端點 `Controllers/PermissionController.cs`
- [ ] T025 [US2] 新增權限檢查端點測試 `Tests/Integration/Controllers/PermissionControllerTests.cs`

### 中介軟體 (US2)

- [ ] T026 [US2] 確認 PermissionAuthorizationMiddleware 使用 PermissionCode `Middleware/PermissionAuthorizationMiddleware.cs`

**US2 驗收檢查**:
- [ ] view 型別權限能正確建立和儲存
- [ ] 權限檢查端點返回正確結果
- [ ] 中介軟體正確執行授權檢查
- [ ] 前端可查詢權限用於 UI 渲染

---

## Phase 5: User Story 3 - 統一編碼規範驗證 (Priority: P2)

**故事目標**: 所有權限代碼遵循統一編碼規範 `resource.action`，系統驗證並拒絕不符合規範的代碼

**獨立測試準則**:
- 有效代碼通過驗證
- 無效代碼被拒絕
- 錯誤訊息清晰
- 規範文件已更新

**相關文件**:
- 編碼規範: research.md, quickstart.md
- 驗證邏輯: PermissionValidationService

### 驗證規則 (US3)

- [ ] T027 [US3] 完善 ValidatePermissionCode 正則表達式 `Services/PermissionValidationService.cs`
- [ ] T028 [US3] [P] 新增 PermissionCodeValidator 單獨驗證類 `Validators/PermissionCodeValidator.cs`

### 文件與測試 (US3)

- [ ] T029 [US3] 單元測試: PermissionCode 格式驗證 `Tests/Unit/Validators/PermissionCodeValidatorTests.cs`
- [ ] T030 [US3] [P] 整合測試: 驗證錯誤場景 `Tests/Integration/Controllers/PermissionControllerTests.cs`

### 稽核與日誌 (US3)

- [ ] T031 [US3] 更新 PermissionFailureLog 記錄邏輯 `Repositories/PermissionFailureLogRepository.cs`

**US3 驗收檢查**:
- [ ] PermissionCode 驗證率 100%
- [ ] 多層級資源格式 (resource.subresource.action) 支持
- [ ] 驗證測試覆蓋所有邊界情況
- [ ] 稽核日誌記錄完整

---

## Phase 6: Polish 優化與交叉關注

### 文件與清理

- [ ] T032 更新 API 文件 README.md 移除 RoutePath 說明
- [ ] T033 代碼清理: 移除被註解的 RoutePath 代碼 `grep -r "RoutePath"` 確認零引用
- [ ] T034 [P] 執行靜態代碼分析和修復編譯警告

**全體驗收標準**:
- [ ] 所有 32 項任務完成
- [ ] 單元測試覆蓋率 > 80% (PermissionService, PermissionValidationService)
- [ ] 整合測試全部通過
- [ ] 無編譯錯誤和警告
- [ ] RoutePath 相關代碼參考數 = 0 (完全移除)
- [ ] 資料庫遷移成功
- [ ] 代碼審查批准

---

## 實施策略

### MVP 優先順序 (第一版)
**目標完成時間**: ~10 小時

1. **完成 Phase 1-2** (Setup + Foundational) - ~3 小時
   - 資料庫遷移
   - 核心服務設置

2. **完成 US1** (移除 RoutePath) - ~7 小時
   - 資料層更新
   - 業務邏輯層更新
   - 驗證器和控制器更新
   - 基礎測試

### 增量交付
**第二版** (+ US2) - 再加 ~10 小時
- 支援 View 權限類型
- 權限檢查端點

**第三版** (+ US3) - 再加 ~12 小時
- 完整編碼規範驗證
- 全部測試和文件

---

## 依賴關係圖

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational - 必須完成)
    ↓
┌─────────────────────────────────┐
│ Phase 3 (US1)                   │
│ 移除 RoutePath                   │
│ 優先級: P1, ~10 小時             │
│ ┌─────────────────────────────┐ │
│ │ 必須先完成 Phase 2          │ │
│ │ 可與 US2/US3 並行           │ │
│ └─────────────────────────────┘ │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ Phase 4 (US2)                   │
│ 支援 View 權限類型                │
│ 優先級: P1, ~6 小時              │
│ ┌─────────────────────────────┐ │
│ │ 可與 US1 並行               │ │
│ │ 可與 US3 並行               │ │
│ └─────────────────────────────┘ │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ Phase 5 (US3)                   │
│ 統一編碼規範驗證                  │
│ 優先級: P2, ~4 小時              │
│ ┌─────────────────────────────┐ │
│ │ 可與 US1/US2 並行           │ │
│ └─────────────────────────────┘ │
└─────────────────────────────────┘
    ↓
Phase 6 (Polish) - ~3 小時
```

---

## 執行檢查清單

### 開始前準備
- [ ] 所有團隊成員已讀 spec.md 和 quickstart.md
- [ ] 開發環境已設置 (.NET 9, PostgreSQL, VS Code/Visual Studio)
- [ ] 功能分支已建立並檢出 (004-permission-refactor)
- [ ] Git 提交約定已確認 (Conventional Commits)

### 執行期間
- [ ] 每日更新 task 完成進度
- [ ] 每 Phase 完成後執行代碼審查
- [ ] 測試失敗時立即修復（不允許跳過測試）
- [ ] 及時文件同步

### 完成前檢查
- [ ] 所有 32 項任務標記為 ✅ 完成
- [ ] 所有測試通過 (100% pass rate)
- [ ] 代碼審查批准
- [ ] PR 合併至 develop 分支
- [ ] 部署準備完成

---

## 工作量估計細節

| Phase | 任務數 | 估計 | 說明 |
|-------|--------|------|------|
| 1 | 3 | 1.5 h | Setup 和環境驗證 |
| 2 | 6 | 4 h | 遷移、種子資料、核心服務 |
| 3 | 11 | 10 h | 移除 RoutePath 的完整實現 |
| 4 | 6 | 6 h | View 權限支持 |
| 5 | 4 | 4 h | 編碼規範驗證 |
| 6 | 3 | 2.5 h | 文件和優化 |
| **總計** | **32** | **~32 h** | 4 個工作天 |

---

## 故障排除指南

### 編譯錯誤
- 檢查所有 DTO 和實體類的屬性映射
- 確認 Enum 值正確
- 驗證 DI 註冊完整

### 測試失敗
- 檢查資料庫遷移是否成功執行
- 驗證種子資料完整
- 確認模擬對象 (Mocks) 正確配置

### 性能問題
- 檢查權限查詢是否已優化 (< 50ms)
- 驗證資料庫索引是否建立
- 確認快取機制正確實現

---

## 相關文件參考

| 文件 | 用途 |
|------|------|
| spec.md | 完整功能需求 |
| plan.md | 技術棧和架構 |
| research.md | 設計決策背景 |
| data-model.md | 實體和 DTO 設計 |
| quickstart.md | 開發者指南 |
| contracts/permission-api.yaml | API 規範 |
| constitution.md | 代碼風格指南 |

````
