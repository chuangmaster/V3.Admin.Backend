# Tasks: 用戶個人資料查詢 API

**Input**: Design documents from `/specs/003-user-profile/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/user-profile-api.yaml
**Language**: Tasks and descriptions MUST be written in Traditional Chinese (zh-TW) per constitution requirements

**Tests**: 本功能包含單元測試和整合測試任務（根據 Constitution 的 Test-First Development 要求）

**Organization**: 任務按用戶故事組織，實現獨立實作和測試

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行（不同檔案，無相依性）
- **[Story]**: 任務所屬的用戶故事（例如：US1, US2, US3）
- 描述中包含確切的檔案路徑

## Path Conventions

- **C# ASP.NET Core Project**: `Controllers/`, `Services/`, `Repositories/`, `Models/` 位於專案根目錄
- **Tests**: `Tests/Unit/`, `Tests/Integration/`
- **Interfaces**: `Services/Interfaces/`, `Repositories/Interfaces/`
- **Database Scripts**: `Database/Scripts/`

---

## Phase 1: Setup（共用基礎設施）

**Purpose**: 專案初始化與基本結構（本功能無需額外設定，使用現有專案結構）

_無任務 - 使用現有專案設定_

---

## Phase 2: Foundational（阻塞性前置需求）

**Purpose**: 在任何用戶故事實作前必須完成的核心基礎設施

**⚠️ CRITICAL**: 此階段完成前無法開始用戶故事工作。所有基礎任務必須遵循 Constitution 原則。

### 資料庫設定

- [x] T001 [P] 在 `Database/Migrations/011_AddUserProfileReadPermission.sql` 中建立 migration 新增 `user.profile.read` 權限定義
- [x] T001b 在 `Database/Scripts/seed_permissions.sql` 中新增 `user.profile.read` 權限定義（seed 腳本備用）

### 資料存取層準備

- [x] T002 檢查 `Repositories/Interfaces/IUserRepository.cs` 確認 `GetUserByIdAsync(Guid userId)` 方法存在
- [x] T003 檢查 `Repositories/Interfaces/IUserRoleRepository.cs` 確認角色查詢方法存在或新增 `GetRoleNamesByUserIdAsync(Guid userId)` 介面方法
- [x] T004 [P] 在 `Repositories/UserRoleRepository.cs` 實作 `GetRoleNamesByUserIdAsync(Guid userId)` 方法（如不存在），使用 LEFT JOIN 查詢用戶角色

**Checkpoint**: 基礎設施就緒 - 可開始用戶故事實作

---

## Phase 3: User Story 1 - 查詢自己的個人資料 (Priority: P1) 🎯 MVP

**Goal**: 允許已登入用戶查詢自己的個人資料，包含 username、displayname 和 roles

**Independent Test**: 使用有效 JWT token 呼叫 `GET /api/account/me` 端點，驗證回應包含正確的 username、displayname 和 roles 資料

### Tests for User Story 1 ⚠️

> **NOTE: 遵循 Test-First Development，先撰寫測試，確保測試失敗後再實作**

- [x] T005 [P] [US1] 在 `Tests/Unit/AccountServiceTests.cs` 撰寫 `GetUserProfileAsync_WithValidUser_ReturnsProfile` 單元測試
- [x] T006 [P] [US1] 在 `Tests/Unit/AccountServiceTests.cs` 撰寫 `GetUserProfileAsync_WithDeletedUser_ReturnsNull` 單元測試
- [x] T007 [P] [US1] 在 `Tests/Unit/AccountServiceTests.cs` 撰寫 `GetUserProfileAsync_WithNoRoles_ReturnsEmptyRolesList` 單元測試
- [x] T008 [P] [US1] 在 `Tests/Unit/AccountServiceTests.cs` 撰寫 `GetUserProfileAsync_WithNullDisplayName_ReturnsNullDisplayName` 單元測試
- [x] T009 [P] [US1] 在 `Tests/Integration/AccountControllerTests.cs` 撰寫 `GetMyProfile_WithValidToken_ReturnsUserProfile` 整合測試
- [x] T010 [P] [US1] 在 `Tests/Integration/AccountControllerTests.cs` 撰寫 `GetMyProfile_WithoutToken_ReturnsUnauthorized` 整合測試
- [x] T011 [P] [US1] 在 `Tests/Integration/AccountControllerTests.cs` 撰寫 `GetMyProfile_WithInvalidToken_ReturnsUnauthorized` 整合測試
- [x] T012 [P] [US1] 在 `Tests/Integration/AccountControllerTests.cs` 撰寫 `GetMyProfile_WithoutPermission_ReturnsForbidden` 整合測試

### Implementation for User Story 1

#### 資料模型層

- [x] T013 [P] [US1] 在 `Models/Responses/UserProfileResponse.cs` 建立 UserProfileResponse DTO，包含 Username (string)、DisplayName (string?)、Roles (List&lt;string&gt;) 屬性，加上 XML 註解

#### 服務層

- [x] T014 [US1] 在 `Services/Interfaces/IAccountService.cs` 新增 `Task<UserProfileResponse?> GetUserProfileAsync(Guid userId)` 介面方法定義，加上 XML 註解
- [x] T015 [US1] 在 `Services/AccountService.cs` 實作 `GetUserProfileAsync(Guid userId)` 方法：
  - 呼叫 `_userRepository.GetUserByIdAsync(userId)` 查詢用戶
  - 檢查用戶是否存在且未刪除（is_deleted = false）
  - 呼叫 `_userRoleRepository.GetRoleNamesByUserIdAsync(userId)` 查詢角色
  - 組合 UserProfileResponse 物件
  - 確保 DisplayName 為空時回傳 null
  - 確保無角色時 Roles 為空陣列
  - 加上 XML 註解

#### 控制器層

- [x] T016 [US1] 在 `Controllers/AccountController.cs` 新增 `GetMyProfile()` 端點：
  - 使用 `[HttpGet("me")]` 路由屬性
  - 使用 `[RequirePermission("user.profile.read")]` 權限屬性
  - 使用 `GetUserId()` 方法從 JWT token 提取用戶 ID
  - 處理 userId 為 null 的情況，使用 `UnauthorizedResponse()` 回傳 401
  - 呼叫 `_accountService.GetUserProfileAsync(userId.Value)`
  - 處理 profile 為 null 的情況，使用 `NotFound()` 回傳 404
  - 使用 `Success()` 方法回傳成功結果
  - 處理例外，使用 `InternalError()` 回傳 500
  - 加上完整 XML 註解（繁體中文）
  - 加上 ProducesResponseType 屬性標註各種回應類型

#### 驗證與錯誤處理

- [x] T017 [US1] 確認錯誤訊息使用繁體中文且包含 TraceId
- [x] T018 [US1] 驗證所有回應使用 ApiResponseModel 包裝，包含 success、code、message、data、timestamp、traceId 欄位

**Checkpoint**: 此時 User Story 1 應完全正常運作且可獨立測試

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: 影響多個用戶故事的改善

- [x] T019 [P] 執行所有測試，確保測試通過率 100%
- [x] T020 [P] 使用 `dotnet test` 執行單元測試，驗證測試覆蓋率
- [x] T021 [P] 使用 Postman 或類似工具進行手動 API 測試，驗證所有場景
- [x] T022 [P] 檢查程式碼遵循 C# 13 最佳實踐和 Constitution 規範
- [x] T023 [P] 檢查所有公開 API 都有 XML 文件註解（繁體中文）
- [x] T024 [P] 驗證資料庫命名使用 snake_case，C# 程式碼使用 PascalCase
- [x] T025 [P] 檢查 nullable reference types 使用正確
- [x] T026 [P] 驗證回應時間符合效能目標（<200ms）
- [x] T027 [P] 更新 Swagger/OpenAPI 文件，確保端點正確顯示
- [x] T028 [P] 執行 Constitution Check 驗證，確保所有原則都已遵循
- [x] T029 執行 `quickstart.md` 中的驗證步驟
- [x] T030 程式碼審查與重構
- [x] T031 建立資料庫遷移檔 `Database/Migrations/011_AddUserProfileReadPermission.sql`
- [x] T032 建立遷移指南 `Database/Migrations/011_MIGRATION_GUIDE.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無相依性 - 可立即開始（本功能無任務）
- **Foundational (Phase 2)**: 無相依性 - 可立即開始 - **阻塞所有用戶故事**
- **User Story 1 (Phase 3)**: 依賴 Foundational (Phase 2) 完成
- **Polish (Phase 4)**: 依賴 User Story 1 完成

### User Story Dependencies

- **User Story 1 (P1)**: 可在 Foundational (Phase 2) 完成後開始 - 無其他故事相依性

### Within User Story 1

執行順序：
1. **Tests (T005-T012)**: 先寫測試，確保失敗
2. **Models (T013)**: 建立 DTO
3. **Services (T014-T015)**: 實作服務層（依賴 Models）
4. **Controllers (T016)**: 實作控制器（依賴 Services）
5. **Validation (T017-T018)**: 驗證與錯誤處理
6. **重新執行測試**: 確保測試通過

### Parallel Opportunities

#### Phase 2: Foundational
所有標記 [P] 的任務可平行執行：
- T001: 權限定義
- T004: Repository 實作

#### Phase 3: User Story 1 Tests
所有測試任務 (T005-T012) 可平行撰寫：
```bash
Task: "GetUserProfileAsync_WithValidUser_ReturnsProfile"
Task: "GetUserProfileAsync_WithDeletedUser_ReturnsNull"
Task: "GetUserProfileAsync_WithNoRoles_ReturnsEmptyRolesList"
Task: "GetUserProfileAsync_WithNullDisplayName_ReturnsNullDisplayName"
Task: "GetMyProfile_WithValidToken_ReturnsUserProfile"
Task: "GetMyProfile_WithoutToken_ReturnsUnauthorized"
Task: "GetMyProfile_WithInvalidToken_ReturnsUnauthorized"
Task: "GetMyProfile_WithoutPermission_ReturnsForbidden"
```

#### Phase 4: Polish
所有標記 [P] 的任務可平行執行：
- T019-T028: 各種驗證和檢查任務

---

## Parallel Example: User Story 1

### 測試階段（平行）
```bash
# 同時撰寫所有單元測試
Task: "GetUserProfileAsync_WithValidUser_ReturnsProfile 單元測試"
Task: "GetUserProfileAsync_WithDeletedUser_ReturnsNull 單元測試"
Task: "GetUserProfileAsync_WithNoRoles_ReturnsEmptyRolesList 單元測試"
Task: "GetUserProfileAsync_WithNullDisplayName_ReturnsNullDisplayName 單元測試"

# 同時撰寫所有整合測試
Task: "GetMyProfile_WithValidToken_ReturnsUserProfile 整合測試"
Task: "GetMyProfile_WithoutToken_ReturnsUnauthorized 整合測試"
Task: "GetMyProfile_WithInvalidToken_ReturnsUnauthorized 整合測試"
Task: "GetMyProfile_WithoutPermission_ReturnsForbidden 整合測試"
```

### 實作階段（依序，但準備工作可平行）
```bash
# Models 可獨立建立
Task: "建立 UserProfileResponse DTO"

# 然後依序實作
Task: "IAccountService 介面方法" → "AccountService 實作" → "AccountController 端點"
```

---

## Implementation Strategy

### MVP First (僅 User Story 1)

1. 完成 Phase 1: Setup（無任務）
2. 完成 Phase 2: Foundational（T001-T004）- **關鍵阻塞階段**
3. 完成 Phase 3: User Story 1（T005-T018）
4. **停止並驗證**: 獨立測試 User Story 1
5. 如果就緒則部署/展示

### 驗證檢查點

完成 User Story 1 後驗證：
- ✅ 使用有效 JWT token 呼叫 `GET /api/account/me` 回傳正確資料
- ✅ 無 token 或 token 無效時回傳 401 Unauthorized
- ✅ 無權限時回傳 403 Forbidden
- ✅ 用戶不存在時回傳 404 Not Found
- ✅ DisplayName 為空時回傳 null
- ✅ 無角色時回傳空陣列 []
- ✅ 所有回應包含 TraceId
- ✅ 所有測試通過
- ✅ 回應時間 <200ms

### 測試驅動開發流程

1. 撰寫測試 (T005-T012) - **測試必須失敗**
2. 實作最小程度的程式碼使測試通過 (T013-T016)
3. 重構程式碼以改善品質
4. 重新執行測試確保仍然通過
5. 重複直到所有需求完成

---

## Task Summary

### 總任務數: 30

### 按階段分類:
- **Phase 1 (Setup)**: 0 任務
- **Phase 2 (Foundational)**: 4 任務 (T001-T004)
- **Phase 3 (User Story 1)**: 14 任務 (T005-T018)
  - Tests: 8 任務 (T005-T012)
  - Implementation: 6 任務 (T013-T018)
- **Phase 4 (Polish)**: 12 任務 (T019-T030)

### 平行執行機會:
- Phase 2: 2 個任務可平行 (T001, T004)
- Phase 3 Tests: 8 個測試可平行撰寫 (T005-T012)
- Phase 4: 10 個任務可平行執行 (T019-T028)

### MVP 範圍:
- **建議 MVP**: Phase 2 + Phase 3 (User Story 1)
- **任務數**: 18 任務
- **預估時間**: 1-2 天（單一開發者）

### 獨立測試標準:
- User Story 1 可完全獨立實作和測試
- 不依賴其他用戶故事
- 具備完整的單元測試和整合測試
- 可作為獨立功能部署

---

## Notes

- **[P]** 標記 = 不同檔案，無相依性，可平行執行
- **[US1]** 標記 = 屬於 User Story 1 的任務
- 每個用戶故事應可獨立完成和測試
- 測試失敗後再實作
- 每個任務或邏輯群組後提交
- 在各檢查點停止以獨立驗證故事
- **避免**: 模糊任務、相同檔案衝突、破壞獨立性的跨故事相依性

---

## Quick Start

### 快速開始實作步驟

1. **執行權限設定** (T001):
   ```bash
   psql -U your_user -d your_database -f Database/Scripts/seed_permissions.sql
   ```

2. **檢查現有 Repository** (T002-T004):
   - 確認 `GetUserByIdAsync` 存在
   - 確認或新增 `GetRoleNamesByUserIdAsync`

3. **撰寫測試** (T005-T012):
   - 在 `Tests/Unit/AccountServiceTests.cs` 撰寫單元測試
   - 在 `Tests/Integration/AccountControllerTests.cs` 撰寫整合測試
   - 執行測試確保失敗

4. **實作功能** (T013-T018):
   - 建立 UserProfileResponse DTO
   - 擴展 IAccountService 介面
   - 實作 AccountService 方法
   - 新增 AccountController 端點

5. **驗證** (T019-T030):
   - 執行所有測試
   - 手動測試 API
   - 檢查程式碼品質
   - 驗證效能

### 預期成果

完成所有任務後，您將擁有：
- ✅ 完整運作的 `GET /api/account/me` API 端點
- ✅ JWT 身份驗證和權限驗證
- ✅ 完整的單元測試和整合測試覆蓋
- ✅ 符合 Constitution 的程式碼品質
- ✅ 繁體中文錯誤訊息和文件
- ✅ 效能符合目標 (<200ms)
