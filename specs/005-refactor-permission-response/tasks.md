---
description: "功能實作的任務列表範本"
---

# 任務：重構 PermissionController API 回應

**輸入**: 來自 `/specs/005-refactor-permission-response/` 的設計文件
**先決條件**: plan.md (必需), spec.md (使用者故事必需), research.md, data-model.md, contracts/

**測試**: 下方的範例包含測試任務。測試是**可選的** - 僅在功能規格中明確要求或使用者要求 TDD 方法時才包含它們。

**組織**: 任務按使用者故事分組，以便每個故事都能獨立實作和測試。

## 格式: `[ID] [P?] [故事] 描述`

- **[P]**: 可平行執行 (不同檔案，無相依性)
- **[故事]**: 此任務所屬的使用者故事 (例如，US1, US2, US3)
- 描述中包含確切的檔案路徑

## 階段 1：設定 (共享基礎設施)

**目的**: 準備重構所需的資料傳輸物件 (DTO)。

- [ ] T001 [P] 根據 `data-model.md` 中的定義，建立 `Models/Responses/PermissionResponse.cs` 回應 DTO，包含 `Id`、`PermissionCode`、`Name`、`Description` 和 `PermissionType` 屬性。確保它不引用任何服務層 DTOs。

---

## 階段 2：使用者故事 1 - 開發者遵循標準化回應 (優先級: P1) 🎯 MVP

**目標**: 重構 `PermissionController` 以使用新的 `Response` DTOs，並對分頁端點使用標準化的 `PagedApiResponseModel`。

**獨立測試**: 對重構後的端點進行 API 呼叫，`GET /api/permissions` 端點返回 `PagedApiResponseModel` 結構，其他端點返回標準 `ApiResponseModel` 結構，且所有整合測試通過。

### 使用者故事 1 的實作

- [ ] T002 [US1] 在 `Controllers/PermissionController.cs` 中，重構 `GetPermissions` 方法。將服務層返回的 `List<PermissionDto>` 映射為 `List<PermissionResponse>`，然後使用 `BaseController` 的輔助方法（如 `CreatePagedSuccess`）來建構一個 `PagedApiResponseModel<PermissionResponse>` 作為回應。
- [ ] T003 [US1] 在 `Controllers/PermissionController.cs` 中，重構 `CreatePermission` 方法，手動將返回的 `PermissionDto` 映射到新的 `PermissionResponse` 物件，然後使用 `Created()` 返回。
- [ ] T004 [US1] 在 `Controllers/PermissionController.cs` 中，重構 `GetPermission` 方法，手動將 `PermissionDto` 映射到 `PermissionResponse` 物件。
- [ ] T005 [US1] 在 `Controllers/PermissionController.cs` 中，重構 `UpdatePermission` 方法，手動將更新後的 `PermissionDto` 映射到 `PermissionResponse` 物件。
- [ ] T006 [US1] 更新 `Tests/Integration/PermissionControllerIntegrationTests.cs` 中的整合測試，以斷言 `GetPermissions` 的新 `PagedApiResponseModel<PermissionResponse>` 結構，並更新其他測試以使用 `PermissionResponse` DTO，確保所有測試通過。

---

## 階段 3：完善與跨領域考量

**目的**: 最終審查與清理。

- [ ] T007 審查 `PermissionController.cs` 中的所有更改，確保 100% 符合 constitution 的 `Principle VIII` 和 `Principle IX`。
- [ ] T008 刪除不再使用的 `Models/Responses/PermissionListResponse.cs` 檔案（如果存在且已無用）。
- [ ] T009 執行所有專案測試，確保重構未引入任何回歸。
- [ ] T010 透過 `quickstart.md` 測試端點，驗證 API 契約未被破壞。

---

## 相依性與執行順序

### 階段相依性

- **設定 (階段 1)**: 無相依性 - 可立即開始。
- **使用者故事 1 (階段 2)**: 依賴於設定完成。`T002` 到 `T005` 可以任意順序進行，但 `T006` (測試) 應隨著每個控制器方法的重構而更新。
- **完善 (階段 3)**: 依賴於使用者故事 1 完成。

### 平行執行機會

- 控制器方法的重構任務 (`T002` - `T005`) 可以由不同的開發人員平行處理，如果他們處理不同的方法。更新測試 (`T006`) 則在之後進行。

---

## 實作策略

### 優先 MVP (完整重構)

1.  完成階段 1：設定 (建立 DTO)。
2.  完成階段 2：使用者故事 1 (重構控制器並更新測試)。
3.  **停止並驗證**: 執行 `PermissionControllerIntegrationTests.cs` 中的所有測試，並根據 `quickstart.md` 手動測試端點。
4.  完成階段 3：完善。
