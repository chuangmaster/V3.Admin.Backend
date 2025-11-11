# Feature Specification: 用戶個人資料查詢 API

**Feature Branch**: `003-user-profile`  
**Created**: 2025-11-11  
**Status**: Draft  
**Input**: User description: "新增一支API,可以用於查詢當前用戶的詳情,回傳包含role, username, displayname"
**Language**: This specification MUST be written in Traditional Chinese (zh-TW) per constitution requirements

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 查詢自己的個人資料 (Priority: P1)

已登入的用戶需要查看自己的個人資料,包括角色、用戶名稱和顯示名稱,以確認當前登入身份和權限。

**Why this priority**: 這是核心功能,提供用戶基本的身份確認能力,是其他功能的基礎。任何需要顯示用戶資訊的界面都依賴此功能。

**Independent Test**: 可以透過呼叫 API 端點並驗證回傳的資料是否包含正確的 role、username 和 displayname 來獨立測試。

**Acceptance Scenarios**:

1. **Given** 用戶已成功登入系統,**When** 用戶請求查詢自己的個人資料,**Then** 系統回傳包含該用戶的角色、用戶名稱和顯示名稱
2. **Given** 用戶擁有多個角色,**When** 用戶請求查詢自己的個人資料,**Then** 系統回傳所有分配給該用戶的角色清單
3. **Given** 用戶的顯示名稱為空白,**When** 用戶請求查詢自己的個人資料,**Then** 系統仍正確回傳其他資訊,顯示名稱欄位為空值或預設值

---

### Edge Cases

- 用戶未登入或身份驗證 token 已過期時,系統如何回應?
- 用戶帳號已被停用或刪除時,系統如何處理?
- 用戶沒有任何角色時,角色欄位應該回傳什麼?
- 並發請求時,系統如何確保資料一致性?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統必須提供一個 API 端點,允許已登入用戶查詢自己的個人資料
- **FR-002**: 系統必須驗證請求者的身份,只允許已通過身份驗證的用戶存取此功能
- **FR-003**: 系統必須從身份驗證 token 中識別當前用戶身份
- **FR-004**: 系統必須回傳用戶的用戶名稱 (username)
- **FR-005**: 系統必須回傳用戶的顯示名稱 (displayname)
- **FR-006**: 系統必須回傳用戶被分配的所有角色 (roles)
- **FR-007**: 系統必須在用戶未登入或 token 無效時回傳適當的錯誤訊息
- **FR-008**: 系統必須在用戶帳號不存在時回傳適當的錯誤訊息
- **FR-009**: 系統必須記錄所有個人資料查詢請求到稽核日誌

### Key Entities

- **用戶 (User)**: 代表系統中的用戶帳號,包含用戶名稱、顯示名稱等基本資訊
- **角色 (Role)**: 代表用戶在系統中的角色,一個用戶可以擁有多個角色
- **用戶角色關聯 (UserRole)**: 表示用戶與角色之間的對應關係

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 已登入用戶可以在 1 秒內成功取得自己的個人資料
- **SC-002**: API 可以支援至少 1000 個並發請求而不影響回應時間
- **SC-003**: 100% 的有效請求都能正確回傳用戶的 username、displayname 和 roles
- **SC-004**: 未授權的請求在 500 毫秒內回傳適當的錯誤狀態碼和訊息
- **SC-005**: 所有查詢請求都被正確記錄到稽核日誌,記錄完整率達 100%

## Assumptions

- 用戶身份驗證採用 JWT token 機制
- 用戶資料存儲在現有的資料庫中
- 角色資訊已經在系統中建立並分配給用戶
- API 遵循 RESTful 設計原則
- 回傳格式為 JSON
- 使用標準的 HTTP 狀態碼表示成功或錯誤
