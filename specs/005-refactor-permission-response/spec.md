# Feature Specification: Refactor PermissionController API Responses

**Feature Branch**: `005-refactor-permission-response`
**Created**: 2025-11-25
**Status**: Draft
**Input**: User description: "重構PermissionController相關的api 為了更方便與精簡程式碼，提升維護效果 請重構該頁面的程式 重點在使用的response物件"

## 概述

本功能旨在重構 `PermissionController` 及其相關的 API 端點，主要目標是簡化程式碼、提升可維護性，並根據專案最新版的 constitution 標準化回應物件。這包含確保所有端點都返回專用的 `xxxResponse` DTO，而不是直接暴露服務層的 DTO。

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 開發者遵循標準化回應 (Priority: P1)

開發者在維護或擴充 `PermissionController` 時，發現所有 API 端點都遵循一致且解耦的回應物件架構，從而降低了理解成本並提升了開發效率。

**Why this priority**: 這是本次重構的核心價值，確保 API 層與業務邏輯層的清晰分離，提升程式碼品質與長期可維護性。

**Independent Test**: 可透過審查 `PermissionController` 的程式碼來獨立測試，驗證所有公開的端點方法都不再直接返回服務層 DTO，而是返回專用的 `*Response` 物件。

**Acceptance Scenarios**:

1.  **Given** 開發者檢查 `GET /api/permissions` 端點，**When** 追溯其回傳型別，**Then** 最終回傳的資料模型是 `ApiResponseModel<PermissionListResponse>` (或類似的 Response DTO)，而不是 `ApiResponseModel<List<PermissionDto>>`。
2.  **Given** 開發者檢查 `POST /api/permissions` 端點，**When** 追溯其成功回傳的資料模型，**Then** 其型別為 `ApiResponseModel<PermissionResponse>`，且該 `PermissionResponse` 是在 Controller 層進行轉換的。
3.  **Given** `PermissionController` 中任何一個需要回傳權限資料的端點，**When** 檢查其程式碼，**Then** DTO 的轉換邏輯（從 Service DTO 到 Response DTO）明確存在於 Controller 的方法中。
4.  **Given** 專案的 constitution，**When** 審查 `PermissionController` 的所有端點，**Then** 所有端點的回應方式都完全符合 `Principle VIII: Controller Response DTO Architecture` 和 `Principle IX: Paginated Response Design` 的規範。

---

### Edge Cases

-   **What happens when** 權限列表為空？`GET /api/permissions` 端點應返回一個包含空列表的成功回應，而不是 `null` 或錯誤。
-   **What happens when** 請求一個不存在的權限 ID？`GET /api/permissions/{id}` 應返回 `404 Not Found`，並帶有標準的錯誤回應格式。

## Requirements *(mandatory)*

### Functional Requirements

-   **FR-001**: `PermissionController` 中的所有 API 端點 MUST 回傳 `IActionResult`，其最終解析的物件必須是標準化的 `ApiResponseModel<T>` 或 `PagedApiResponseModel<TItem>`。
-   **FR-002**: 上述 `T` 或 `TItem` 型別 MUST 是位於 `Models/Responses/` 目錄下的專用 Response DTO (例如 `PermissionResponse`)。
-   **FR-003**: Response DTO MUST 不依賴於服務層 DTO (例如 `PermissionDto`)，包含屬性、建構函式或方法。
-   **FR-004**: 從 Service DTO 到 Response DTO 的對應轉換邏輯 MUST 在 `PermissionController` 的方法內部完成。
-   **FR-005**: 重構後的程式碼品質（如可讀性、圈複雜度）應優於或等於重構前。
-   **FR-006**: 若因重構導致公開的 API JSON 結構發生變化（例如欄位名稱變更），則變更必須記錄在本規格文件中。

### Key Entities *(include if feature involves data)*

-   **PermissionResponse**: 代表在 API 回應中的單一權限物件。
    -   `Id` (or `PermissionId`)
    -   `PermissionCode`
    -   `Name`
    -   `Description`
-   **PermissionListResponse**: 代表權限列表的回應物件。
    -   `Items`: `List<PermissionResponse>`
    -   (如果適用，包含分頁相關欄位)

## Success Criteria *(mandatory)*

### Measurable Outcomes

-   **SC-001**: 程式碼審查確認 `PermissionController` 中 100% 的端點都遵循專案 constitution 中的 `Principle VIII` 和 `Principle IX`。
-   **SC-002**: `PermissionController.cs` 的程式碼圈複雜度 (Cyclomatic Complexity) 相比重構前降低或持平。
-   **SC-003**: 針對 `PermissionController` 的所有現有自動化測試（單元測試、整合測試）在重構後必須全部通過（測試代碼可能需要更新以適應新的 Response DTO）。
-   **SC-004**: 公開的 API 合約（JSON 回應結構）保持不變，或任何變更都是經過同意且有文件記錄的。
-   **SC-005**: 本次重構不引入任何新功能。

## Assumptions

1.  **API Contract Stability**: 優先假設 API 的外部合約（JSON 格式）應盡量保持不變，以避免對前端造成破壞性變更。如果為了對齊 constitution 而必須變更，則應明確記錄。
2.  **Testing**: 假設專案已存在針對 `PermissionController` 的基本測試集，可用於迴歸測試。