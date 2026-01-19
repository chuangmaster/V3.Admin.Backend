# Feature Specification: Account Module Refactoring

**Feature Branch**: `007-account-refactor`  
**Created**: 2026-01-20  
**Status**: Draft  
**Input**: 重構 User 模組,將 username 重命名為 Account,並將密碼修改功能分離成兩個獨立 API(用戶自己修改密碼和管理者重設密碼),同時補足 Account 相關權限

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Account Field Migration (Priority: P1)

管理員和用戶需要系統將現有的用戶名稱欄位(username)重新命名為帳號(account),以提高模組的可識別性和語義清晰度。這個變更需要透明地進行,不影響現有用戶的使用體驗。

**Why this priority**: 這是整個重構的基礎,所有其他功能都依賴於正確的資料結構。必須優先完成以確保後續開發的穩定性。

**Independent Test**: 可以通過以下方式獨立測試:
1. 檢查資料庫 schema,確認欄位名稱已從 username 更新為 account
2. 呼叫任何返回用戶資料的 API,驗證回應中使用 "account" 欄位而非 "username"
3. 確認現有用戶資料完整遷移,無資料遺失

**Acceptance Scenarios**:

1. **Given** 系統中存在使用舊 username 欄位的用戶資料, **When** 執行資料庫遷移, **Then** 所有 username 資料成功遷移到 account 欄位,且無資料遺失
2. **Given** API 回應包含用戶資料, **When** 客戶端請求用戶資訊, **Then** 回應 JSON 中使用 "account" 鍵而非 "username"
3. **Given** 客戶端使用 "username" 欄位發送請求, **When** 系統接收到請求, **Then** 系統返回錯誤訊息說明 "username" 欄位已棄用,必須使用 "account" 欄位

---

### User Story 2 - User Self Password Change (Priority: P2)

用戶需要能夠修改自己的密碼,並在修改過程中需要驗證舊密碼以確保帳號安全性。系統必須支援併發控制,防止在密碼修改過程中發生資料衝突。

**Why this priority**: 密碼修改是用戶自主管理帳號安全的核心功能,直接影響用戶體驗和安全性。優先於管理員功能,因為用戶自助功能使用頻率更高。

**Independent Test**: 可以通過以下方式獨立測試:
1. 用戶登入後,使用正確的舊密碼和新密碼呼叫密碼修改 API
2. 驗證系統要求提供舊密碼
3. 使用新密碼成功登入
4. 使用過時的 version 參數嘗試修改密碼,驗證併發控制機制

**Acceptance Scenarios**:

1. **Given** 用戶已登入且擁有 user.profile.update 權限, **When** 用戶提供正確的舊密碼、新密碼和當前 version, **Then** 密碼成功更新,version 遞增,用戶可使用新密碼登入
2. **Given** 用戶已登入, **When** 用戶提供錯誤的舊密碼, **Then** 系統拒絕修改請求並返回適當錯誤訊息
3. **Given** 用戶提供過時的 version 參數, **When** 嘗試修改密碼, **Then** 系統返回併發衝突錯誤,提示用戶重新獲取最新資料
4. **Given** 用戶未登入或無 user.profile.update 權限, **When** 嘗試修改密碼, **Then** 系統返回未授權錯誤
5. **Given** 新密碼不符合密碼強度要求, **When** 嘗試修改密碼, **Then** 系統返回驗證錯誤並說明密碼要求

---

### User Story 3 - Admin Password Reset (Priority: P3)

系統管理員需要能夠重設其他用戶的密碼,無需知道用戶的舊密碼。這個操作必須記錄審計日誌,包含操作者和被操作者資訊,以確保系統安全性和可追溯性。

**Why this priority**: 這是管理功能,使用頻率低於用戶自助功能,但對於處理用戶忘記密碼或帳號鎖定等情況至關重要。

**Independent Test**: 可以通過以下方式獨立測試:
1. 管理員使用 account.update 權限呼叫密碼重設 API
2. 驗證無需提供舊密碼即可成功重設
3. 檢查審計日誌中是否記錄了操作者 ID、被操作者 ID 和操作時間
4. 驗證併發控制機制(version 參數)

**Acceptance Scenarios**:

1. **Given** 管理員擁有 account.update 權限, **When** 管理員為指定用戶重設密碼並提供正確的 version, **Then** 密碼成功重設,version 遞增,審計日誌記錄此操作
2. **Given** 管理員提供過時的 version 參數, **When** 嘗試重設密碼, **Then** 系統返回併發衝突錯誤
3. **Given** 用戶無 account.update 權限, **When** 嘗試重設他人密碼, **Then** 系統拒絕操作並返回權限不足錯誤
4. **Given** 密碼重設成功, **When** 查詢審計日誌, **Then** 日誌包含操作者 ID、被操作者 ID、操作時間和操作類型
5. **Given** 管理員嘗試重設不存在的用戶密碼, **When** 執行操作, **Then** 系統返回用戶不存在錯誤

---

### User Story 4 - Account Permission Management (Priority: P3)

系統需要補足完整的 Account 模組權限設定,包含查詢、修改和刪除帳號的權限,以實現細粒度的權限控制。

**Why this priority**: 權限控制是安全性的基礎,但相較於核心功能(資料遷移和密碼管理),屬於系統完善性功能,因此優先級較低。

**Independent Test**: 可以通過以下方式獨立測試:
1. 創建具有特定權限的測試角色(如僅有 account.read)
2. 使用該角色嘗試各種 Account 操作
3. 驗證權限控制按預期工作(允許讀取,拒絕修改/刪除)

**Acceptance Scenarios**:

1. **Given** 用戶擁有 account.read 權限, **When** 嘗試查詢帳號資訊, **Then** 系統允許操作並返回資料
2. **Given** 用戶擁有 account.update 權限, **When** 嘗試修改帳號資訊, **Then** 系統允許操作
3. **Given** 用戶擁有 account.delete 權限, **When** 嘗試刪除帳號, **Then** 系統允許操作
4. **Given** 用戶無相應權限, **When** 嘗試執行對應操作, **Then** 系統拒絕並返回權限不足錯誤
5. **Given** 用戶嘗試查詢、修改或刪除不存在的帳號, **When** 執行操作, **Then** 系統返回資源不存在錯誤

---

## Clarifications

### Session 2026-01-20

- Q: 當用戶修改密碼後,現有的登入會話應該如何處理? → A: 保持當前會話有效,使其他設備的會話失效
- Q: 舊的 API 客戶端可能仍然發送 "username" 欄位,系統應該如何處理? → A: 立即拒絕 username 欄位,返回明確錯誤要求使用 account
- Q: 當管理員重設用戶的密碼後,是否應該通知該用戶? → A: 不發送任何通知,由管理員自行告知用戶
- Q: 資料遷移期間如果有 API 請求進來,系統如何處理? → A: 前後端同時調整部署,不需特別處理
- Q: 如果兩個管理員同時重設同一用戶密碼且使用相同 version,如何處理? → A: 使用資料庫 WHERE 條件確保原子性,第一個請求成功,第二個因 version 不匹配失敗

---

### Edge Cases

- **密碼重設的頻率限制**: 是否需要限制密碼修改/重設的頻率,以防止濫用?
- **審計日誌的保留期限**: 密碼重設的審計日誌應該保留多久?
- **權限變更的即時生效**: 當用戶的 account.* 權限被修改後,是否立即生效還是需要重新登入?

## Requirements *(mandatory)*

### Functional Requirements

#### 資料遷移與 API 更新
- **FR-001**: 系統必須提供資料庫遷移腳本,將所有現有的 username 欄位重新命名為 account,且保持資料完整性
- **FR-002**: 所有返回用戶資料的 API 回應必須使用 "account" 欄位名稱而非 "username"
- **FR-003**: 系統必須在 Repository、Service 和 Controller 層統一使用 account 命名,確保程式碼一致性
- **FR-003a**: 系統必須拒絕包含 "username" 欄位的 API 請求,並返回明確錯誤訊息說明該欄位已棄用,要求使用 "account" 欄位

#### 用戶自助密碼修改
- **FR-004**: 系統必須提供 `PUT /api/account/me/password` 端點,允許用戶修改自己的密碼
- **FR-005**: 密碼修改請求必須包含 oldPassword、newPassword 和 version 三個必填欄位
- **FR-006**: 系統必須驗證提供的 oldPassword 與當前密碼匹配,否則拒絕修改
- **FR-007**: 系統必須驗證 version 參數與資料庫中的版本一致,否則返回併發衝突錯誤
- **FR-007a**: 系統必須使用資料庫 WHERE 條件(如 UPDATE ... WHERE version=X)確保併發更新的原子性,防止競爭條件
- **FR-008**: 密碼修改端點必須要求用戶擁有 user.profile.update 權限
- **FR-009**: 密碼成功修改後,系統必須將用戶的 version 欄位遞增 1
- **FR-009a**: 密碼修改成功後,系統必須保持當前會話有效,但使該用戶在其他設備上的所有會話失效

#### 管理員密碼重設
- **FR-010**: 系統必須提供 `PUT /api/account/{id}/reset-password` 端點,允許管理員重設指定用戶的密碼
- **FR-011**: 密碼重設請求必須包含 newPassword 和 version 兩個必填欄位,不需要 oldPassword
- **FR-012**: 密碼重設端點必須要求操作者擁有 account.update 權限
- **FR-013**: 系統必須驗證 version 參數與資料庫中的版本一致,否則返回併發衝突錯誤
- **FR-013a**: 系統必須使用資料庫 WHERE 條件確保密碼重設操作的原子性,當多個管理員同時操作時,僅第一個請求成功
- **FR-014**: 密碼重設成功後,系統必須在審計日誌中記錄操作者 ID、被操作者 ID、操作時間和操作類型
- **FR-014a**: 密碼重設成功後,系統不發送通知給被重設密碼的用戶,由管理員自行告知

#### 密碼驗證
- **FR-015**: 系統必須驗證新密碼符合密碼強度要求(具體要求取決於現有系統規則)
- **FR-016**: 系統必須拒絕與當前密碼相同的新密碼

#### 權限管理
- **FR-017**: 系統必須支援 account.read 權限,控制查詢帳號資訊的能力
- **FR-018**: 系統必須支援 account.update 權限,控制修改帳號資訊和重設密碼的能力
- **FR-019**: 系統必須支援 account.delete 權限,控制刪除帳號的能力
- **FR-020**: 所有 Account 相關的 Controller 端點必須配置適當的權限驗證

#### 錯誤處理
- **FR-021**: 系統必須為所有失敗場景返回清晰的錯誤訊息(如:舊密碼錯誤、併發衝突、權限不足)
- **FR-022**: 系統必須使用統一的錯誤回應格式(BaseResponseModel)

### Key Entities

- **Account (User)**: 代表系統中的用戶帳號,關鍵屬性包含:
  - account (原 username): 用於登入的唯一識別符
  - password: 加密後的密碼
  - version: 用於併發控制的版本號
  - 相關關聯: 角色(Roles)、權限(Permissions)、審計日誌(AuditLogs)

- **AuditLog**: 記錄系統中的重要操作,特別是密碼重設操作,關鍵屬性包含:
  - operatorId: 執行操作的用戶 ID
  - targetUserId: 被操作的用戶 ID
  - operationType: 操作類型(如 "PasswordReset")
  - timestamp: 操作時間

- **Permission**: 代表系統權限,新增的權限包含:
  - account.read: 查詢帳號權限
  - account.update: 修改帳號和重設密碼權限
  - account.delete: 刪除帳號權限
  - user.profile.update: 用戶修改自己資料的權限

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 所有現有用戶資料在遷移後完整保留,資料遺失率為 0%
- **SC-002**: API 回應中 100% 使用 "account" 欄位名稱,無 "username" 欄位出現
- **SC-003**: 用戶能在 30 秒內完成自助密碼修改操作(從填寫表單到收到成功確認)
- **SC-004**: 管理員能在 20 秒內完成密碼重設操作
- **SC-005**: 併發控制機制有效阻止 100% 的併發衝突場景(使用過時 version 的請求全部被拒絕)
- **SC-006**: 所有密碼重設操作都被記錄在審計日誌中,審計完整率達 100%
- **SC-007**: 權限控制準確率達 100%,無權限的用戶無法執行受保護的操作
- **SC-008**: 密碼修改和重設操作的錯誤訊息清晰明確,用戶能在不查閱文件的情況下理解錯誤原因並採取正確行動
