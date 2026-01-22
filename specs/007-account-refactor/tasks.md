# Tasks: Account Module Refactoring

**Feature Branch**: `007-account-refactor`  
**Input**: Design documents from `/specs/007-account-refactor/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-spec.yaml, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Project root**: `d:\Repository\V3.Admin.Backend\`
- **Controllers**: `Controllers/`
- **Services**: `Services/` and `Services/Interfaces/`
- **Repositories**: `Repositories/` and `Repositories/Interfaces/`
- **Models**: `Models/Entities/`, `Models/Dtos/`, `Models/Requests/`, `Models/Responses/`
- **Validators**: `Validators/`
- **Middleware**: `Middleware/`
- **Database**: `Database/Migrations/`
- **Tests**: `Tests/Unit/`, `Tests/Integration/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 確認專案結構符合 plan.md 定義的三層架構
- [x] T002 確認已安裝必要 NuGet 套件(BCrypt.Net-Next, FluentValidation, Dapper 等)
- [x] T003 [P] 確認 git 分支 007-account-refactor 已建立並切換

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 檢查並確認 VersionValidationMiddleware 已實作在 Middleware/VersionValidationMiddleware.cs
- [x] T005 檢查並確認 JwtService 能夠在 JWT claims 中包含 version 資訊
- [x] T006 [P] 確認 AuditLogRepository 已存在且支援記錄密碼重設操作
- [x] T007 [P] 確認 ResponseCodes 包含 CONCURRENT_UPDATE_CONFLICT 錯誤碼定義在 Models/ResponseCodes.cs
- [x] T008 [P] 確認 BaseApiController 提供統一的錯誤處理機制

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Account Field Migration (Priority: P1) 🎯 MVP

**Goal**: 將 username 欄位重命名為 account,確保所有 API 和資料層使用新欄位名稱

**Independent Test**: 
1. 執行資料庫遷移後,檢查 users 表結構確認欄位已重命名
2. 呼叫任何返回用戶資料的 API,驗證回應使用 "account" 而非 "username"
3. 使用包含 "username" 欄位的請求,驗證系統返回明確錯誤

### Database Migration for User Story 1

- [x] T009 [US1] 建立資料庫遷移腳本 Database/Migrations/014_RenameUsernameToAccount.sql
- [ ] T010 [US1] 在開發環境執行遷移腳本並驗證資料完整性(無資料遺失)

### Entity & DTO Updates for User Story 1

- [x] T011 [P] [US1] 更新 User Entity,將 Username 屬性重命名為 Account 在 Models/Entities/User.cs
- [x] T012 [P] [US1] 更新 UserDto,將 Username 屬性重命名為 Account 在 Models/Dtos/UserDto.cs
- [x] T013 [P] [US1] 更新所有包含用戶資料的 Response 模型(如 AccountResponse, LoginResponse)使用 Account 欄位

### Repository Layer Updates for User Story 1

- [x] T014 [US1] 更新 UserRepository 所有 SQL 查詢,將 username 欄位改為 account 在 Repositories/UserRepository.cs
- [x] T015 [US1] 更新 IUserRepository 介面方法參數名稱(如有使用 username 參數)在 Repositories/Interfaces/IUserRepository.cs

### Service Layer Updates for User Story 1

- [x] T016 [US1] 更新 AuthService 使用 Account 屬性而非 Username 在 Services/AuthService.cs
- [x] T017 [P] [US1] 更新 AccountService 使用 Account 屬性在 Services/AccountService.cs
- [x] T018 [P] [US1] 檢查並更新其他 Service 中所有對 Username 的引用

### Controller Layer Updates for User Story 1

- [x] T019 [P] [US1] 更新 AuthController 使用 Account 欄位在 Controllers/AuthController.cs
- [x] T020 [P] [US1] 更新 AccountController 使用 Account 欄位在 Controllers/AccountController.cs

### Validation for User Story 1

- [x] T021 [P] [US1] 更新所有 Validator 類別,將 username 驗證改為 account 驗證在 Validators/
- [x] T022 [US1] ~~新增驗證邏輯:拒絕包含 "username" 欄位的請求並返回明確錯誤訊息~~ (已決定不實作,API 使用 account 欄位即可)

### Testing for User Story 1

- [x] T023 [P] [US1] 更新單元測試:將測試中的 Username 改為 Account 在 Tests/Unit/
- [x] T024 [P] [US1] 更新整合測試:將測試中的 Username 改為 Account 在 Tests/Integration/
- [x] T025 [US1] ~~撰寫整合測試:驗證使用 username 欄位的請求被拒絕並返回適當錯誤~~ (T022 不實作,此測試不需要)

**Checkpoint**: User Story 1 完成 - 所有欄位已重命名,API 使用 account,username 請求被拒絕

---

## Phase 4: User Story 2 - User Self Password Change (Priority: P2)

**Goal**: 實作用戶自助密碼修改功能,包含舊密碼驗證和併發控制

**Independent Test**:
1. 用戶登入後使用正確舊密碼和新密碼呼叫 PUT /api/account/me/password
2. 驗證使用新密碼可以成功登入
3. 使用錯誤舊密碼嘗試修改,驗證請求被拒絕
4. 使用過時 version 嘗試修改,驗證返回 409 Conflict

### Models for User Story 2

- [ ] T026 [P] [US2] 建立 ChangePasswordRequest 模型在 Models/Requests/ChangePasswordRequest.cs
- [ ] T027 [P] [US2] 建立 ChangePasswordRequestValidator,驗證 oldPassword/newPassword 必填、newPassword 符合密碼強度要求(參照現有系統的密碼驗證器規則:最小長度、字元類型要求等)、且新密碼不得與當前密碼相同 (FR-016) 在 Validators/ChangePasswordRequestValidator.cs

### Repository Layer for User Story 2

- [ ] T028 [US2] 在 IUserRepository 新增 UpdatePasswordAsync 方法定義在 Repositories/Interfaces/IUserRepository.cs
- [ ] T029 [US2] 在 UserRepository 實作 UpdatePasswordAsync(使用 WHERE version=X 和 RETURNING)在 Repositories/UserRepository.cs
- [ ] T030 [US2] 在 IUserRepository 新增 GetByIdWithVersionAsync 方法定義(如不存在)在 Repositories/Interfaces/IUserRepository.cs
- [ ] T031 [US2] 在 UserRepository 實作 GetByIdWithVersionAsync 方法在 Repositories/UserRepository.cs

### Service Layer for User Story 2

- [ ] T032 [US2] 在 IAccountService 新增 ChangePasswordAsync 方法定義在 Services/Interfaces/IAccountService.cs
- [ ] T033 [US2] 在 AccountService 實作 ChangePasswordAsync 方法,包含舊密碼驗證和版本檢查在 Services/AccountService.cs
- [x] T033a [US2] 在 AccountService 建構函式中注入 IDistributedCache,並在 ChangePasswordAsync 成功後清除版本號快取在 Services/AccountService.cs

### Controller Layer for User Story 2

- [ ] T034 [US2] 在 AccountController 實作 PUT /api/account/me/password 端點在 Controllers/AccountController.cs
- [ ] T035 [US2] 為端點新增 [Authorize] 和權限驗證(user.profile.update)在 Controllers/AccountController.cs
- [ ] T036 [US2] 新增 XML 文件註解說明端點用途、參數和回應在 Controllers/AccountController.cs

### JWT Version Validation for User Story 2

- [ ] T037 [US2] 更新 JwtService.GenerateToken 在 JWT claims 中包含 version 在 Services/JwtService.cs
- [ ] T038 [US2] 確認 VersionValidationMiddleware 已註冊在 Program.cs(在 UseAuthorization 之前)

### Testing for User Story 2

- [ ] T039 [P] [US2] 撰寫 Validator 單元測試:驗證各種無效輸入被拒絕在 Tests/Unit/Validators/ChangePasswordRequestValidatorTests.cs
- [ ] T040 [P] [US2] 撰寫 Service 單元測試:驗證舊密碼驗證邏輯在 Tests/Unit/Services/AccountServiceTests.cs
- [ ] T041 [P] [US2] 撰寫 Service 單元測試:驗證併發控制邏輯(version 不匹配)在 Tests/Unit/Services/AccountServiceTests.cs
- [ ] T042 [US2] 撰寫整合測試:完整密碼修改流程(使用 Testcontainers)在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T043 [US2] 撰寫整合測試:驗證錯誤舊密碼場景在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T044 [US2] 撰寫整合測試:驗證併發衝突場景(409 Conflict)在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T045 [US2] 撰寫整合測試:驗證權限控制(無 user.profile.update 權限被拒絕)在 Tests/Integration/Controllers/AccountControllerTests.cs

**Checkpoint**: User Story 2 完成 - 用戶可以修改自己的密碼,併發控制有效,權限驗證正常

---

## Phase 5: User Story 3 - Admin Password Reset (Priority: P3)

**Goal**: 實作管理員密碼重設功能,無需舊密碼,記錄審計日誌

**Independent Test**:
1. 管理員使用 account.update 權限呼叫 PUT /api/account/{id}/reset-password
2. 驗證無需舊密碼即可成功重設
3. 檢查 audit_logs 表記錄了操作
4. 使用過時 version 嘗試重設,驗證返回 409 Conflict

### Models for User Story 3

- [ ] T046 [P] [US3] 建立 ResetPasswordRequest 模型在 Models/Requests/ResetPasswordRequest.cs
- [ ] T047 [P] [US3] 建立 ResetPasswordRequestValidator 在 Validators/ResetPasswordRequestValidator.cs

### Repository Layer for User Story 3

- [ ] T048 [US3] 檢查 UserRepository.UpdatePasswordAsync 是否支援無舊密碼驗證的重設場景(可能需要新增 ResetPasswordAsync 方法)在 Repositories/UserRepository.cs

### Service Layer for User Story 3

- [ ] T049 [US3] 在 IAccountService 新增 ResetPasswordAsync 方法定義在 Services/Interfaces/IAccountService.cs
- [ ] T050 [US3] 在 AccountService 實作 ResetPasswordAsync 方法,包含版本檢查和審計日誌記錄在 Services/AccountService.cs
- [ ] T051 [US3] 在 ResetPasswordAsync 中呼叫 AuditLogRepository 記錄操作(OperatorId, TargetUserId, Action)在 Services/AccountService.cs

### Controller Layer for User Story 3

- [ ] T052 [US3] 在 AccountController 實作 PUT /api/account/{id}/reset-password 端點在 Controllers/AccountController.cs
- [x] T052a [US3] 在 AccountController 建構函式中注入 IDistributedCache,並在 ResetPassword 成功後清除版本號快取在 Controllers/AccountController.cs
- [ ] T053 [US3] 為端點新增 [Authorize] 和權限驗證(account.update)在 Controllers/AccountController.cs
- [ ] T054 [US3] 新增 XML 文件註解說明端點用途、參數和回應在 Controllers/AccountController.cs

### Testing for User Story 3

- [ ] T055 [P] [US3] 撰寫 Validator 單元測試:驗證 ResetPasswordRequestValidator 在 Tests/Unit/Validators/ResetPasswordRequestValidatorTests.cs
- [ ] T056 [P] [US3] 撰寫 Service 單元測試:驗證 ResetPasswordAsync 邏輯在 Tests/Unit/Services/AccountServiceTests.cs
- [ ] T057 [P] [US3] 撰寫 Service 單元測試:驗證審計日誌記錄在 Tests/Unit/Services/AccountServiceTests.cs
- [ ] T058 [US3] 撰寫整合測試:完整密碼重設流程(使用 Testcontainers)在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T059 [US3] 撰寫整合測試:驗證權限控制(無 account.update 權限被拒絕)在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T060 [US3] 撰寫整合測試:驗證併發衝突場景(409 Conflict)在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T061 [US3] 撰寫整合測試:驗證審計日誌確實被寫入資料庫在 Tests/Integration/Controllers/AccountControllerTests.cs

**Checkpoint**: User Story 3 完成 - 管理員可以重設用戶密碼,審計日誌記錄完整,併發控制有效

---

## Phase 6: User Story 4 - Account Permission Management (Priority: P3)

**Goal**: 補足 Account 模組權限設定(account.read, account.update, account.delete)

**Independent Test**:
1. 建立測試角色分別擁有 account.read, account.update, account.delete 權限
2. 驗證權限控制:有權限的請求成功,無權限的請求被拒絕
3. 檢查所有 Account 相關端點都配置了適當權限

### Database for User Story 4

- [ ] T062 [US4] 檢查 permissions 表是否已包含 account.read, account.update, account.delete 權限
- [ ] T063 [US4] 如不存在,建立遷移腳本或 seed script 新增這些權限在 Database/Migrations/ 或 Database/Scripts/seed.sql

### Middleware/Service for User Story 4

- [ ] T064 [US4] 檢查 PermissionAuthorizationMiddleware 是否支援檢查 account.* 權限在 Middleware/PermissionAuthorizationMiddleware.cs

### Controller Updates for User Story 4

- [x] T065 [P] [US4] 為 AccountController 的查詢端點新增 account.read 權限檢查在 Controllers/AccountController.cs
- [x] T066 [P] [US4] 為 AccountController 的修改端點新增 account.update 權限檢查在 Controllers/AccountController.cs
- [x] T067 [P] [US4] 為 AccountController 的刪除端點新增 account.delete 權限檢查在 Controllers/AccountController.cs

### Testing for User Story 4

- [ ] T068 [P] [US4] 撰寫整合測試:驗證 account.read 權限控制在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T069 [P] [US4] 撰寫整合測試:驗證 account.update 權限控制在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T070 [P] [US4] 撰寫整合測試:驗證 account.delete 權限控制在 Tests/Integration/Controllers/AccountControllerTests.cs
- [ ] T071 [US4] 撰寫整合測試:驗證無權限用戶被拒絕訪問 Account 端點在 Tests/Integration/Controllers/AccountControllerTests.cs

**Checkpoint**: User Story 4 完成 - 所有 Account 端點都有適當權限控制,細粒度權限管理生效

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T072 [P] 更新 OpenAPI/Swagger 文件,確保新端點出現在 API 文件中
- [ ] T073 [P] 檢查並更新 README.md 或 API 文件說明新的密碼修改和重設流程
- [ ] T074 [P] 程式碼審查:確保所有中文註解清晰,XML 文件註解完整
- [ ] T075 [P] 檢查日誌記錄:確保密碼相關操作不記錄敏感資訊(密碼本身)
- [ ] T076 執行完整的整合測試套件(所有 user stories)
- [ ] T077 執行 quickstart.md 中的驗證步驟,確保所有 checklist 項目通過
- [ ] T078 [P] 效能測試:驗證併發控制在高負載下的表現
- [ ] T079 程式碼清理和重構:移除任何舊的 username 相關註解或死程式碼
- [ ] T080 最終安全審查:確認所有新端點符合 Security Non-Negotiable 原則

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User Story 1 (P1): Can start after Foundational - No dependencies on other stories
  - User Story 2 (P2): Can start after Foundational - No hard dependency on US1 but recommended to complete US1 first for clarity
  - User Story 3 (P3): Can start after Foundational - Depends on US2 infrastructure (password update logic)
  - User Story 4 (P3): Can start after Foundational - Independent of other stories
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Foundation only - Must complete first as it's the base for all other work
- **User Story 2 (P2)**: Foundation + US1 recommended - Core password change functionality
- **User Story 3 (P3)**: Foundation + US2 infrastructure - Reuses password update logic
- **User Story 4 (P3)**: Foundation only - Independent permission setup

### Within Each User Story

- Database migrations before entity updates
- Entity/DTO updates before repository layer
- Repository layer before service layer
- Service layer before controller layer
- Implementation before tests (TDD: write tests first, ensure they FAIL, then implement)

### Parallel Opportunities

#### Phase 1 (Setup)
- T001, T002, T003 can all run in parallel

#### Phase 2 (Foundational)
- T006, T007, T008 can run in parallel (different components)

#### Phase 3 (User Story 1)
- T011, T012, T013 can run in parallel (different model files)
- T017, T018 can run in parallel (different service files)
- T019, T020 can run in parallel (different controller files)
- T021, T022 need sequential (T022 depends on T021 context)
- T023, T024 can run in parallel (different test files)

#### Phase 4 (User Story 2)
- T026, T027 can run in parallel (different files)
- T040, T041 can run in parallel (different test scenarios)
- T042, T043, T044, T045 need sequential (integration test setup dependencies)

#### Phase 5 (User Story 3)
- T046, T047 can run in parallel (different files)
- T056, T057 can run in parallel (different test aspects)
- T058, T059, T060, T061 need sequential (integration test dependencies)

#### Phase 6 (User Story 4)
- T065, T066, T067 can run in parallel (different endpoints)
- T068, T069, T070 can run in parallel (different test files)

#### Phase 7 (Polish)
- T072, T073, T074, T075, T078 can run in parallel (different concerns)

---

## Parallel Example: User Story 1

```bash
# Launch entity/DTO updates in parallel:
Task: "T011 - Update User Entity Account field in Models/Entities/User.cs"
Task: "T012 - Update UserDto Account field in Models/Dtos/UserDto.cs"
Task: "T013 - Update Response models Account field"

# Then launch service updates in parallel:
Task: "T017 - Update AccountService in Services/AccountService.cs"
Task: "T018 - Update other Services references"

# Then launch controller updates in parallel:
Task: "T019 - Update AuthController in Controllers/AuthController.cs"
Task: "T020 - Update AccountController in Controllers/AccountController.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Account Field Migration)
4. **STOP and VALIDATE**: 
   - Run database migration
   - Test API responses use "account"
   - Test "username" requests are rejected
5. Deploy/demo if ready - this is a deployable increment

### Incremental Delivery

1. **Foundation** (Phase 1 + 2): ~0.5 days
2. **US1** (Phase 3): ~0.5 days → Deploy (MVP! Field migration complete)
3. **US2** (Phase 4): ~1 day → Deploy (User password change ready)
4. **US3** (Phase 5): ~0.5 days → Deploy (Admin password reset ready)
5. **US4** (Phase 6): ~0.5 days → Deploy (Permissions complete)
6. **Polish** (Phase 7): ~0.5 days → Final deployment

**Total Estimated Time**: 3.5-4 days

### Parallel Team Strategy

With multiple developers:

1. **All team members**: Complete Phase 1 + 2 together (Foundation) - ~0.5 days
2. **Once Foundational is done**:
   - Developer A: User Story 1 (Phase 3) - 0.5 days
   - Developer B: User Story 4 (Phase 6) - can start in parallel - 0.5 days
3. **After US1 complete**:
   - Developer A: User Story 2 (Phase 4) - 1 day
   - Developer C: User Story 3 (Phase 5) - can start in parallel - 0.5 days
4. **All together**: Phase 7 (Polish) - 0.5 days

**Parallel Team Time**: ~2 days (vs 3.5-4 days sequential)

---

## Task Summary

- **Total Tasks**: 80
- **Setup (Phase 1)**: 3 tasks
- **Foundational (Phase 2)**: 5 tasks (BLOCKING)
- **User Story 1 (Phase 3)**: 17 tasks (P1 - MVP)
- **User Story 2 (Phase 4)**: 20 tasks (P2)
- **User Story 3 (Phase 5)**: 16 tasks (P3)
- **User Story 4 (Phase 6)**: 10 tasks (P3)
- **Polish (Phase 7)**: 9 tasks

**Parallel Opportunities**: 28 tasks marked [P] can run in parallel within their phase

**MVP Scope**: Phase 1 + Phase 2 + Phase 3 (User Story 1) = 25 tasks = ~1 day

**Success Criteria Coverage**:
- SC-001 (資料遷移無遺失): T009, T010, T023
- SC-002 (API 使用 account): T011-T020, T024
- SC-003 (密碼修改 30 秒內完成): T026-T045
- SC-004 (密碼重設 20 秒內完成): T046-T061
- SC-005 (併發控制 100% 有效): T041, T044, T056, T060
- SC-006 (審計日誌 100% 記錄): T051, T057, T061
- SC-007 (權限控制 100% 準確): T045, T059, T068-T071
- SC-008 (錯誤訊息清晰): T022, T039, T055, T075

---

## Notes

- [P] tasks = different files, no dependencies within phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing (TDD approach)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, breaking changes without migration path
- Security reminder: Never log passwords or sensitive data
- All public methods need XML documentation comments in Traditional Chinese
