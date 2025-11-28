# Feature Specification: 權限控制器回傳模型重構

**Feature Branch**: `006-refactor-permission-controller`  
**Created**: 2025年11月25日  
**Status**: Draft  
**Input**: User description: "配合consitution.md 來重構 PermissionController使用PagedApiResponseModel做回傳； service使用PageResultDto做回傳，內容必須使用繁體中文撰寫"

## User Scenarios & Testing (強制性)

### User Story 1 - 檢索分頁權限列表 (優先級: P1)

API消費者能夠以分頁的方式檢索權限列表，並且回傳的資料結構符合新的分頁API回應模型 (`PagedApiResponseModel`)。

**為何此優先級**: 這是此重構最核心的功能，確保權限列表的API能正常運作並使用新的回傳模型。

**獨立測試**: 可以透過呼叫權限列表API並驗證回傳的JSON結構是否包含 `PageSize`, `CurrentPage`, `TotalPages`, `TotalCount`, `Items` 等分頁資訊，以及 `Success`, `Message`, `Code` 等API回應標準資訊來獨立測試。

**驗收情境**:

1.  **Given** API消費者發送一個帶有分頁參數 (例如 `page=1`, `pageSize=10`) 的GET請求到權限列表端點，**When** 請求被處理，**Then** 系統回傳一個HTTP 200 OK狀態碼，並且回應主體是一個 `PagedApiResponseModel` 結構，其中 `Items` 包含指定頁數的權限資料，且分頁資訊正確。

---

### User Story 2 - 檢索單一權限詳細資料 (優先級: P2)

API消費者能夠檢索單一權限的詳細資料，並且回傳的資料結構符合標準API回應模型 (`ApiResponseModel`)，儘管此重構主要針對分頁，但仍需確保非分頁回傳的API不受影響。

**為何此優先級**: 確保現有非分頁API的穩定性。

**獨立測試**: 可以透過呼叫單一權限API並驗證回傳的JSON結構是否符合 `ApiResponseModel` 且資料正確。

**驗收情境**:

1.  **Given** API消費者發送一個帶有有效權限ID的GET請求到單一權限詳細資料端點，**When** 請求被處理，**Then** 系統回傳一個HTTP 200 OK狀態碼，並且回應主體是一個 `ApiResponseModel` 結構，其中 `Data` 包含該權限的詳細資料。

---

### Edge Cases (邊緣情況)

- 當請求的分頁參數 (例如 `page`, `pageSize`) 無效或為負數時，系統應如何處理？ (例如回傳預設值或錯誤訊息)。
- 當請求的分頁頁碼超出總頁數時，系統應回傳空列表或第一頁資料？
- 當權限列表為空時，`PagedApiResponseModel` 應如何表示 (例如 `Items` 為空陣列，`TotalCount` 為 0)。

## Requirements (強制性)

### Functional Requirements (功能需求)

- **FR-001**: 系統必須重構 `PermissionController` 中的權限列表相關API端點，使其回傳 `PagedApiResponseModel<PermissionDto>`。
- **FR-002**: 系統必須重構 `PermissionService` 中的權限列表相關方法，使其回傳 `PageResultDto<PermissionDto>`。
- **FR-003**: 系統必須確保 `PermissionController` 中的非列表相關API端點 (例如取得單一權限) 繼續回傳 `ApiResponseModel<PermissionDto>` 或 `ApiResponseModel<object>`，不應受此次重構影響。
- **FR-004**: 系統必須處理分頁參數的驗證，確保 `page` 和 `pageSize` 為有效數值。
- **FR-005**: 系統必須依據 `constitution.md` 中C#開發指南的規範進行代碼風格和命名慣例的調整。

### Key Entities (關鍵實體)

-   **Permission (權限)**: 代表系統中的一個權限實體，具有ID、名稱、描述等屬性。
-   **PermissionDto (權限資料傳輸物件)**: 用於在服務層和控制層之間傳輸權限資料的物件。
-   **PagedApiResponseModel (分頁API回應模型)**: 作為HTTP API回應的標準結構，包含分頁元資料 (例如頁碼、每頁大小、總頁數、總筆數) 和資料列表。
-   **ApiResponseModel (API回應模型)**: 作為HTTP API回應的標準結構，包含操作結果 (例如成功/失敗)、訊息和資料。
-   **PageResultDto (分頁結果資料傳輸物件)**: 用於在服務層內部傳輸分頁結果的標準結構，包含資料列表和總筆數。

## Success Criteria (成功標準)

### Measurable Outcomes (可衡量結果)

-   **SC-001**: 重構後的權限列表API在請求分頁資料時，回傳的JSON結構符合 `PagedApiResponseModel` 規範，並包含正確的分頁資訊。
-   **SC-002**: 針對權限列表API，99%的有效分頁請求能在500毫秒內獲得響應。
-   **SC-003**: 重構後，所有現有權限相關的單元測試和整合測試都能成功通過，確保功能無迴歸。
-   **SC-004**: Code Review結果顯示，所有修改都符合 `constitution.md` 中C#開發指南的程式碼風格、命名慣例和註釋要求。
-   **SC-005**: API文檔 (Swagger/OpenAPI) 能夠正確反映重構後權限列表API的回傳模型變更。