# Feature Specification: 重構權限路由機制

**Feature Branch**: `004-refactor-permission-routing`  
**Created**: 2025-11-15  
**Status**: Draft  
**Input**: User description: "重構功能。 #permisssion 想要移除 'RoutePath' 的相關功能，把決定路由權限放在permission code 本身，同時PermissionType未來就可能是function 或是 view，簡化功能"
**Language**: Traditional Chinese (zh-TW)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 系統管理員使用簡化的權限代碼管理權限 (Priority: P1)

系統管理員需要管理系統權限時，不再需要同時維護權限代碼（permission_code）和路由路徑（route_path）兩個欄位。權限的類型和路由資訊完全由權限代碼本身表達，使權限管理更加直觀和一致。

**為何是此優先級**: 這是核心重構目標，簡化權限資料結構是所有其他變更的基礎，必須優先完成。

**獨立測試**: 可以通過建立新權限、查詢現有權限來完整測試。系統管理員能夠僅使用權限代碼來定義和識別任何權限，不需要額外的路由路徑欄位。

**驗收場景**:

1. **Given** 系統管理員要建立一個新的查看權限，**When** 建立權限代碼為 "inventory.view" 且權限類型為 "view"，**Then** 系統成功建立權限，不需要提供 route_path 欄位
2. **Given** 系統管理員要建立一個功能權限，**When** 建立權限代碼為 "inventory.create" 且權限類型為 "function"，**Then** 系統成功建立權限，permission_code 自動決定其作用範圍
3. **Given** 系統中已有使用 route_path 的舊權限資料，**When** 系統管理員查詢這些權限，**Then** 系統正確顯示這些權限，route_path 資訊已被移除或遷移至 permission_code

---

### User Story 2 - 開發人員通過權限代碼進行權限驗證 (Priority: P2)

開發人員在實作功能時，只需要檢查使用者是否擁有特定的權限代碼（如 "inventory.view" 或 "order.create"），不需要同時考慮路由路徑和權限類型的組合邏輯。

**為何是此優先級**: 這影響到開發人員如何使用權限系統，但可以在資料結構重構完成後再調整應用層邏輯。

**獨立測試**: 可以通過模擬 API 請求，檢查權限驗證中間件是否正確使用權限代碼進行驗證，而不依賴 route_path。

**驗收場景**:

1. **Given** 使用者擁有 "inventory.view" 權限，**When** 使用者嘗試存取庫存檢視功能，**Then** 系統僅基於權限代碼 "inventory.view" 判斷是否授予存取權限
2. **Given** 使用者擁有 "order.create" 功能權限，**When** 使用者嘗試建立訂單，**Then** 系統基於權限代碼進行驗證，不需要檢查任何路由路徑配置
3. **Given** 權限驗證失敗，**When** 記錄失敗日誌，**Then** 日誌中僅記錄權限代碼和使用者資訊，不包含 route_path 欄位

---

### User Story 3 - 系統自動遷移現有權限資料 (Priority: P1)

系統需要將現有的 permission 資料表中的權限資料遷移至新結構，確保既有的權限配置不會遺失，且 permission_type 從 'route'/'function' 轉換為 'view'/'function'。

**為何是此優先級**: 資料遷移是重構的必要步驟，必須與資料結構變更同時完成，以確保系統持續運作。

**獨立測試**: 可以通過執行資料庫遷移腳本，然後驗證所有現有權限是否正確轉換且功能正常。

**驗收場景**:

1. **Given** 資料表中存在 permission_type = 'route' 的權限，**When** 執行資料遷移，**Then** 這些權限的 permission_type 轉換為 'view'，route_path 資訊整合至 permission_code 或被移除
2. **Given** 資料表中存在 permission_type = 'function' 的權限，**When** 執行資料遷移，**Then** 這些權限的 permission_type 維持為 'function'，route_path 欄位被移除
3. **Given** 遷移腳本執行完成，**When** 查詢所有權限，**Then** 所有權限都符合新的資料結構，沒有遺失任何權限配置

---

### Edge Cases

- 當權限代碼格式不符合新的命名規範時（如缺少資源名稱或操作類型），系統如何處理？
- 如果舊資料中存在重複的 route_path 但不同 permission_code 的情況，遷移時如何避免衝突？
- 當使用者嘗試建立權限時未指定 permission_type，系統是否有預設值或要求必填？
- 如果權限代碼中包含多層級結構（如 "inventory.product.view"），系統如何解析和驗證？

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統必須移除 permissions 資料表中的 route_path 欄位
- **FR-002**: 系統必須將 permission_type 的允許值從 ('route', 'function') 改為 ('view', 'function')
- **FR-003**: 系統必須移除與 route_path 相關的資料庫約束（chk_route_path_required）
- **FR-004**: 系統必須移除與 route_path 相關的檢查約束，僅保留 permission_code 格式驗證
- **FR-005**: 權限代碼（permission_code）必須能夠完整表達權限的作用範圍，格式為 "resource.action"（如 "inventory.view", "order.create"）
- **FR-006**: 系統必須提供資料遷移腳本，將現有 permission_type = 'route' 的權限轉換為 'view' 類型
- **FR-007**: 系統必須更新所有相關的程式碼（Controllers, Services, Repositories）以移除 RoutePath 屬性和相關邏輯
- **FR-008**: 系統必須更新權限驗證中間件（PermissionAuthorizationMiddleware），使其僅基於 permission_code 進行驗證
- **FR-009**: 系統必須更新所有 DTO、Entity、Request、Response 模型，移除 RoutePath 相關屬性
- **FR-010**: 系統必須更新 Validators，移除 RoutePath 欄位的驗證邏輯
- **FR-011**: 系統必須確保 permission_code 的唯一性約束在新結構下仍然有效
- **FR-012**: 系統必須更新 API 文檔（OpenAPI/Swagger），反映權限結構的變更

### Key Entities

- **Permission（權限）**: 代表系統中的一個權限項目
  - permission_code: 權限代碼（唯一識別碼，格式為 "resource.action"）
  - name: 權限名稱
  - description: 權限描述
  - permission_type: 權限類型（'view' 表示檢視權限，'function' 表示功能權限）
  - ~~route_path: 路由路徑~~（已移除）
  - 審計欄位：created_at, updated_at, created_by, updated_by, is_deleted, deleted_at, deleted_by, version

- **RolePermission（角色權限關聯）**: 關聯角色與權限的多對多關係
  - 不受此重構影響，維持原有結構

- **UserRole（使用者角色關聯）**: 關聯使用者與角色的多對多關係
  - 不受此重構影響，維持原有結構

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 資料庫 permissions 資料表中不再存在 route_path 欄位，所有資料庫遷移成功執行且無資料遺失
- **SC-002**: 所有現有的權限驗證功能在重構後仍能正常運作，使用者無法存取其權限範圍外的資源
- **SC-003**: 系統管理員建立新權限時，僅需提供 permission_code、name、description 和 permission_type，無需額外提供路由路徑資訊
- **SC-004**: 所有單元測試和整合測試在重構後均能通過，涵蓋權限建立、查詢、驗證等核心功能
- **SC-005**: API 文檔正確反映新的權限資料結構，開發人員能夠清楚了解如何使用新的權限系統
- **SC-006**: 權限驗證的效能不因重構而降低，平均回應時間保持在原有水準（建議 < 100ms）
- **SC-007**: 資料遷移過程可以安全地回滾，確保在遷移失敗時不會造成資料損壞
