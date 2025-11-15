# Feature Specification: 權限模型重構 - 移除 RoutePath、整合路由決策至 PermissionCode

**Feature Branch**: `004-permission-refactor`  
**Created**: 2025-11-16  
**Status**: Draft  
**Input**: 重構功能，想要移除 'RoutePath' 的相關功能，把決定路由權限放在permission code 本身，同時PermissionType未來就可能是function 或是 view；view表示某個區塊的瀏覽權限  
**Language**: Traditional Chinese (zh-TW)

## 功能概述

本重構旨在簡化權限模型，將目前在 `RoutePath` 欄位中管理的路由資訊移至 `PermissionCode` 中，並擴展 `PermissionType` 的語義以支持 `view`（區塊瀏覽權限），使權限管理更加一致、靈活且可擴展。

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 權限管理者移除路由路徑欄位 (Priority: P1)

權限管理者不需再在創建或編輯權限時填入 `RoutePath` 欄位，系統自動從 `PermissionCode` 推導路由資訊。

**Why this priority**: 這是重構的核心價值，消除冗餘欄位、簡化資料結構、降低數據管理複雜度。

**Independent Test**: 可以透過建立新權限並驗證無需提供 RoutePath，系統仍能正確識別和驗證路由權限。

**Acceptance Scenarios**:

1. **Given** 權限管理頁面已開啟，**When** 管理者建立新的路由權限（如 `inventory.view`），**Then** 系統不要求輸入 `RoutePath`，權限代碼本身包含足夠的路由資訊
2. **Given** 存在舊格式的權限（含 `RoutePath`），**When** 執行遷移指令碼，**Then** 系統從 `PermissionCode` 正確推導路由資訊，`RoutePath` 被標記為已移除或廢棄
3. **Given** 現有系統正使用 `RoutePath` 進行驗證，**When** 遷移完成，**Then** 系統轉而使用 `PermissionCode` 進行相同的路由驗證，行為一致

---

### User Story 2 - 支援 View 權限類型 (Priority: P1)

系統支援第三種權限類型 `view`，用於控制 UI 元件或頁面區塊的顯示/隱藏。

**Why this priority**: 支持更細粒度的權限控制，符合現代前端 UI 元件層級的權限需求，提升使用者體驗。

**Independent Test**: 可以建立、分配和驗證 `view` 類型權限，在進行權限檢查時正確識別該用戶是否有瀏覽特定區塊的權限。

**Acceptance Scenarios**:

1. **Given** 權限管理介面已開啟，**When** 管理者選擇建立新權限並指定類型為 `view`，**Then** 系統接受此類型並儲存權限（如 `dashboard.summary_widget`）
2. **Given** 用戶已獲分配包含 `view` 類型權限的角色，**When** 前端查詢該用戶是否有特定 view 權限（如 `reports.analytics_panel`），**Then** API 返回權限驗證結果為 true
3. **Given** 用戶無該 view 權限，**When** 前端查詢該權限，**Then** API 返回 false，前端據此隱藏對應 UI 元件

---

### User Story 3 - 權限代碼編碼規範統一 (Priority: P2)

所有權限類型（function、view、route）的 `PermissionCode` 遵循統一編碼規範，便於識別和管理。

**Why this priority**: 統一編碼規範提高代碼可維護性和可讀性，便於開發者快速理解權限含義，減少錯誤。

**Independent Test**: 驗證系統接受並正確驗證遵循規範的權限代碼（如 `resource.action`、`resource.sub_resource.view`），拒絕不符合規範的代碼。

**Acceptance Scenarios**:

1. **Given** 系統已定義新的編碼規範（如 `[resource].[subresource?].[action|view]`），**When** 管理者建立權限代碼 `inventory.create` 或 `reports.sales.view`，**Then** 系統驗證通過並儲存
2. **Given** 管理者嘗試建立不符合規範的代碼（如 `inv create`），**When** 提交表單，**Then** 系統返回驗證錯誤，提示正確格式
3. **Given** 遷移舊系統中的權限，**When** 檢查其 PermissionCode 格式，**Then** 系統識別需要更新的權限並記錄

---

### Edge Cases

- **What happens when** 現有應用程式仍在使用 route 類型的權限進行授權檢查？系統應提供遷移工具，將舊的 route 權限轉換為 view 權限。
- **What happens when** 同一權限代碼在舊系統中對應多個不同的 `RoutePath`？系統應記錄衝突並提示管理者手動調整為 view 權限。
- **What happens when** 新建立的 view 權限未被分配給任何角色？系統應允許創建但提示管理者此權限未生效。
- **What happens when** 刪除某個權限類型（如移除所有 `view` 類型）？系統應進行級聯檢查以確保不破壞依賴。

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統 MUST 移除 Permission 實體中的 `RoutePath` 欄位（或標記為廢棄），後續不再維護此欄位
- **FR-002**: 系統 MUST 支援兩種 PermissionType：`function`（操作權限）和 `view`（區塊瀏覽權限），完全移除 `route` 類型，舊的路由權限應遷移為 `view` 類型
- **FR-003**: 系統 MUST 定義並驗證 PermissionCode 編碼規範，支援多層級資源表示（如 `resource.subresource.action`）
- **FR-004**: 系統 MUST 提供資料遷移方案，將現有的 `RoutePath` 資訊安全轉換至新的 PermissionCode 格式
- **FR-005**: 系統 MUST 在 Permission 建立/編輯 API 中移除 `RoutePath` 必填驗證，改為可選或廢棄狀態
- **FR-006**: 系統 MUST 更新所有權限驗證邏輯（PermissionValidationService、PermissionAuthorizationMiddleware 等）以適配新模型
- **FR-007**: 系統 MUST 更新權限管理 UI（控制器、DTO、表單驗證器）以隱藏或移除 RoutePath 欄位
- **FR-008**: 系統 MUST 對現有 permission 記錄建立向後相容層，確保舊的授權檢查邏輯仍能工作（過渡期）
- **FR-009**: 系統 MUST 保留 Permission 實體中的版本號（version），支援樂觀並發控制
- **FR-010**: 系統 MUST 在所有 Permission 異動操作（CREATE、UPDATE、DELETE）時記錄稽核日誌

### Key Entities

- **Permission**: 
  - 移除或廢棄 `RoutePath` 欄位
  - 擴展 `PermissionType` 支持 `view` 類型
  - `PermissionCode` 編碼規範：支持多層級（如 `resource.subresource.action`）
  - 保留 `CreatedAt`, `UpdatedAt`, `DeletedAt`, `Version` 等審計欄位

- **Role**: 無變動，仍為權限集合

- **RolePermission**: 無直接變動，但隨 Permission 變動而調整查詢邏輯

- **UserRole**: 無直接變動

- **PermissionFailureLog**: 記錄欄位 `AttemptedResource` 應調整為記錄新格式的 PermissionCode 而非 RoutePath

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 所有權限管理 API（建立、編輯、刪除、查詢權限）在移除 RoutePath 後，測試通過率達 100%
- **SC-002**: 資料遷移完成後，現有的 1000+ 個權限記錄中，95% 以上的 PermissionCode 格式合法且路由資訊可正確推導
- **SC-003**: 權限驗證性能不降低，授權檢查平均耗時在 50ms 內（與遷移前相同）
- **SC-004**: view 和 function 權限類型建立、分配、驗證功能完全可用，route 類型已完全移除
- **SC-005**: 舊 route 類型權限遷移完成，所有轉換為 view 類型的權限能正確驗證，零授權檢查錯誤
- **SC-006**: 完成 RoutePath 廢棄後，系統程式碼中 RoutePath 和 route 類型引用數量下降 100%

---

## Assumptions

根據現有資訊和業界標準，本規格假設：

1. **Permission Type 的完整列表**: 重構後支持 `function` 和 `view` 兩種類型，完全移除 `route` 類型
2. **PermissionCode 編碼規範**: 遵循 `[resource].[subresource?].[action]` 格式，如 `inventory.create`、`reports.sales.view`、`dashboard.summary_widget`
3. **舊 route 權限遷移策略**: 將現有所有 route 類型權限轉換為 view 類型，例如舊的 `route` + `RoutePath=/inventory` 轉為 `view` + `PermissionCode=inventory.view`
4. **向後相容期**: 預計 2-3 個月內維持相容層，之後完全移除 RoutePath 和 route 類型相關程式碼
5. **稽核需求**: 所有權限變動必須記錄稽核日誌（已在現有 AuditLog 機制中）
6. **前端改造**: 前端應調整 UI，改由 PermissionCode 和 PermissionType 決定 UI 元件的渲染（`function` 用於操作按鈕，`view` 用於區塊顯示）

---

## 相關依賴

- 現有功能：002-permission-management（權限管理基礎）
- 受影響模組：
  - Controllers: PermissionController, AccountController 等（返回 Permission DTO 需調整）
  - Services: PermissionService, PermissionValidationService（權限驗證邏輯需調整）
  - Middleware: PermissionAuthorizationMiddleware（授權檢查需適配新格式）
  - Validators: 權限建立/編輯驗證器需更新
  - Repositories: PermissionRepository 需調整查詢邏輯
