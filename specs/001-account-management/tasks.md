# Tasks: 帳號管理系統

**Input**: Design documents from `/specs/001-account-management/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api-spec.yaml ✅
**Language**: Traditional Chinese (zh-TW) per constitution requirements

**Tests**: Test tasks are NOT included in this implementation plan as they were not explicitly requested in the feature specification. Tests can be added later if TDD approach is required.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (專案初始化)

**目的**: 建立專案基礎結構與必要的資料夾

- [X] T001 建立資料夾結構 - 在專案根目錄建立 Models/Entities/, Models/Requests/, Models/Responses/, Models/Dtos/, Models/Views/, Configuration/, Validators/, Middleware/, Database/Migrations/, Database/Scripts/
- [X] T002 安裝必要的 NuGet 套件 - Npgsql, Dapper, BCrypt.Net-Next, Microsoft.AspNetCore.Authentication.JwtBearer, FluentValidation, FluentValidation.AspNetCore
- [X] T003 [P] 建立 .editorconfig 檔案,設定 C# 13 程式碼風格與繁體中文註解規範
- [X] T004 [P] 更新 appsettings.json 與 appsettings.Development.json,加入 ConnectionStrings (PostgreSQL) 與 JwtSettings 組態區段

---

## Phase 2: Foundational (基礎建設 - 阻塞所有使用者故事)

**目的**: 建立核心基礎設施,必須完成才能開始任何使用者故事的實作

**⚠️ 重要**: 所有使用者故事的工作必須等待此階段完成才能開始。所有基礎任務必須遵循專案憲法原則。

### 資料庫與資料存取

- [X] T005 建立 Database/Migrations/001_CreateUsersTable.sql - PostgreSQL 建表腳本,包含 users 資料表定義、索引、約束 (參考 data-model.md 的 User Entity 定義)
- [X] T006 [P] 建立 Database/Scripts/seed.sql - 初始資料腳本,建立預設管理員帳號 (admin/Admin@123)
- [X] T007 建立 Models/Entities/User.cs - User 實體類別,對應 users 資料表,包含 Id, Username, PasswordHash, DisplayName, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, DeletedBy, Version 屬性

### 組態管理

- [X] T008 [P] 建立 Configuration/DatabaseSettings.cs - 資料庫連線設定模型
- [X] T009 [P] 建立 Configuration/JwtSettings.cs - JWT 組態模型,包含 SecretKey, Issuer, Audience, ExpirationMinutes

### 回應模型與錯誤處理

- [X] T010 建立 Models/ResponseCodes.cs - 業務邏輯代碼常數類別,定義 SUCCESS, CREATED, VALIDATION_ERROR, INVALID_CREDENTIALS, UNAUTHORIZED, FORBIDDEN, NOT_FOUND, USERNAME_EXISTS, PASSWORD_SAME_AS_OLD, CANNOT_DELETE_SELF, LAST_ACCOUNT_CANNOT_DELETE, CONCURRENT_UPDATE_CONFLICT, INTERNAL_ERROR
- [X] T011 更新 Models/ApiResponseModel.cs - 確保包含 Success, Code, Message, Data, Timestamp, TraceId 屬性,並提供靜態工廠方法 (Success, Created, Error, ValidationError, NotFound 等)

### 中介軟體

- [X] T012 [P] 建立 Middleware/TraceIdMiddleware.cs - 自動產生 TraceId 並注入到 HttpContext.Items 與回應標頭
- [X] T013 [P] 建立 Middleware/ExceptionHandlingMiddleware.cs - 全域異常處理,捕捉所有未處理的例外並包裝為 ApiResponseModel 回應 (繁體中文錯誤訊息)

### JWT 驗證

- [X] T014 在 Program.cs 中設定 JWT Bearer Authentication - 註冊 JwtSettings 組態、設定 Authentication 與 Authorization 中介軟體
- [X] T015 建立 Services/Interfaces/IJwtService.cs - JWT 服務介面,定義 GenerateToken(User user) 方法
- [X] T016 建立 Services/JwtService.cs - 實作 IJwtService,使用 System.IdentityModel.Tokens.Jwt 產生 JWT Token (包含 sub, unique_name, name, jti, iat, exp, nbf claims)

### Repository 基礎設施

- [X] T017 建立 Repositories/Interfaces/IUserRepository.cs - User Repository 介面,定義 GetByIdAsync, GetByUsernameAsync, ExistsAsync, GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync (軟刪除), CountActiveAsync 方法
- [X] T018 建立 Repositories/UserRepository.cs - 實作 IUserRepository,使用 Dapper 進行 PostgreSQL 資料存取 (注入 IDbConnection,使用參數化查詢)

### 依賴注入設定

- [X] T019 在 Program.cs 中註冊服務 - 使用 AddScoped 註冊 IDbConnection (Npgsql), IUserRepository, IJwtService,設定 FluentValidation (AddValidatorsFromAssemblyContaining<Program>())
- [X] T020 [P] 設定 Swagger/OpenAPI - 加入 JWT Bearer 認證支援、設定 XML 文件註解路徑、啟用繁體中文描述

**Checkpoint**: 基礎建設完成 - 使用者故事實作現在可以並行開始

---

## Phase 3: User Story 1 - 帳號密碼登入 (Priority: P1) 🎯 MVP

**目標**: 實作使用者登入功能,驗證帳號密碼並回傳 JWT Token,支援多裝置登入

**獨立測試**: 使用 Swagger UI 或 Postman 呼叫 POST /api/auth/login,輸入正確的帳號密碼 (admin/Admin@123),驗證能成功取得 JWT Token

### US1 - 資料模型

- [X] T021 [P] [US1] 建立 Models/Requests/LoginRequest.cs - 登入請求模型,包含 Username, Password 屬性
- [X] T022 [P] [US1] 建立 Models/Responses/LoginResponse.cs - 登入回應模型,包含 Token, ExpiresAt, User (AccountResponse) 屬性
- [X] T023 [P] [US1] 建立 Models/Responses/AccountResponse.cs - 帳號回應模型,包含 Id, Username, DisplayName, CreatedAt, UpdatedAt 屬性
- [X] T024 [P] [US1] 建立 Models/Dtos/LoginDto.cs - 登入 Dto,包含 Username, Password 屬性
- [X] T025 [P] [US1] 建立 Models/Dtos/LoginResultDto.cs - 登入結果 Dto,包含 Token, ExpiresAt, User (AccountDto) 屬性
- [X] T026 [P] [US1] 建立 Models/Dtos/AccountDto.cs - 帳號 Dto,包含 Id, Username, DisplayName, CreatedAt, UpdatedAt, Version 屬性

### US1 - 驗證器

- [X] T027 [US1] 建立 Validators/LoginRequestValidator.cs - LoginRequest 驗證器,使用 FluentValidation,驗證 Username (必填, 3-20 字元) 與 Password (必填, 最少 8 字元),錯誤訊息使用繁體中文

### US1 - 服務層

- [X] T028 建立 Services/Interfaces/IAuthService.cs - 身份驗證服務介面,定義 LoginAsync(LoginDto loginDto) 方法,回傳 LoginResultDto
- [X] T029 建立 Services/AuthService.cs - 實作 IAuthService,注入 IUserRepository, IJwtService,實作登入邏輯 (查詢使用者、驗證密碼 BCrypt.Verify、產生 Token、記錄登入失敗嘗試)

### US1 - 控制器

- [X] T030 [US1] 更新 Controllers/AuthController.cs - 實作 POST /api/auth/login 端點,注入 IAuthService,接收 LoginRequest,轉換為 LoginDto,呼叫 AuthService.LoginAsync,將 LoginResultDto 轉換為 LoginResponse,包裝為 ApiResponseModel<LoginResponse> 回傳 (處理 INVALID_CREDENTIALS, VALIDATION_ERROR, INTERNAL_ERROR 錯誤碼)

**Checkpoint**: 此時 User Story 1 應該完全功能正常且可獨立測試

---

## Phase 4: User Story 2 - 新增帳號 (Priority: P2)

**目標**: 實作新增帳號功能,驗證帳號唯一性、密碼強度,使用 BCrypt 雜湊儲存密碼

**獨立測試**: 登入系統後 (使用 US1 取得 Token),使用 Swagger UI 或 Postman 呼叫 POST /api/accounts,帶入 JWT Token,輸入新帳號資訊,驗證能成功建立帳號並允許新帳號登入

### US2 - 資料模型

- [X] T031 [P] [US2] 建立 Models/Requests/CreateAccountRequest.cs - 新增帳號請求模型,包含 Username, Password, DisplayName 屬性
- [X] T032 [P] [US2] 建立 Models/Dtos/CreateAccountDto.cs - 新增帳號 Dto,包含 Username, Password, DisplayName 屬性

### US2 - 驗證器

- [X] T033 [US2] 建立 Validators/CreateAccountRequestValidator.cs - CreateAccountRequest 驗證器,驗證 Username (必填, 3-20 字元, 正規表示式 ^[a-zA-Z0-9_]+$), Password (必填, 最少 8 字元), DisplayName (必填, 最大 100 字元),繁體中文錯誤訊息

### US2 - 服務層

- [X] T034 建立 Services/Interfaces/IAccountService.cs - 帳號管理服務介面,定義 CreateAccountAsync(CreateAccountDto dto) 方法,回傳 AccountDto
- [X] T035 建立 Services/AccountService.cs - 實作 IAccountService,注入 IUserRepository,實作 CreateAccountAsync (檢查帳號唯一性、BCrypt 雜湊密碼、建立 User Entity、呼叫 Repository.CreateAsync、轉換為 AccountDto)
- [X] T036 在 Program.cs 中註冊 IAccountService - 使用 AddScoped<IAccountService, AccountService>()

### US2 - 控制器

- [X] T037 [US2] 建立 Controllers/AccountController.cs - 繼承 BaseApiController,加入 [Authorize] 屬性,實作 POST /api/accounts 端點,接收 CreateAccountRequest,轉換為 CreateAccountDto,呼叫 AccountService.CreateAccountAsync,將 AccountDto 轉換為 AccountResponse,包裝為 ApiResponseModel<AccountResponse> 回傳 201 Created (處理 USERNAME_EXISTS, VALIDATION_ERROR, INTERNAL_ERROR 錯誤碼)

**Checkpoint**: 此時 User Stories 1 與 2 應該都能獨立運作

---

## Phase 5: User Story 3 - 修改個人資料 (Priority: P2)

**目標**: 實作修改密碼與姓名功能,驗證舊密碼正確性,新舊密碼不可相同

**獨立測試**: 登入系統後,使用 Swagger UI 或 Postman 呼叫 PUT /api/accounts/{id} (更新姓名) 與 PUT /api/accounts/{id}/password (變更密碼),驗證變更生效 (使用新密碼登入或檢查姓名更新)

### US3 - 資料模型

- [ ] T038 [P] [US3] 建立 Models/Requests/UpdateAccountRequest.cs - 更新帳號請求模型,包含 DisplayName 屬性
- [ ] T039 [P] [US3] 建立 Models/Requests/ChangePasswordRequest.cs - 變更密碼請求模型,包含 OldPassword, NewPassword 屬性
- [ ] T040 [P] [US3] 建立 Models/Dtos/UpdateAccountDto.cs - 更新帳號 Dto,包含 Id, DisplayName, Version 屬性
- [ ] T041 [P] [US3] 建立 Models/Dtos/ChangePasswordDto.cs - 變更密碼 Dto,包含 UserId, OldPassword, NewPassword, Version 屬性

### US3 - 驗證器

- [ ] T042 [P] [US3] 建立 Validators/UpdateAccountRequestValidator.cs - UpdateAccountRequest 驗證器,驗證 DisplayName (必填, 最大 100 字元),繁體中文錯誤訊息
- [ ] T043 [P] [US3] 建立 Validators/ChangePasswordRequestValidator.cs - ChangePasswordRequest 驗證器,驗證 OldPassword (必填), NewPassword (必填, 最少 8 字元),繁體中文錯誤訊息

### US3 - 服務層

- [ ] T044 更新 Services/Interfaces/IAccountService.cs - 加入 UpdateAccountAsync(UpdateAccountDto dto), ChangePasswordAsync(ChangePasswordDto dto) 方法
- [ ] T045 更新 Services/AccountService.cs - 實作 UpdateAccountAsync (查詢使用者、檢查版本號、更新 DisplayName、處理並發衝突),實作 ChangePasswordAsync (驗證舊密碼、檢查新舊密碼不同、BCrypt 雜湊新密碼、更新資料、處理並發衝突)

### US3 - 控制器

- [ ] T046 [US3] 更新 Controllers/AccountController.cs - 實作 PUT /api/accounts/{id} 端點,接收 UpdateAccountRequest,轉換為 UpdateAccountDto,呼叫 AccountService.UpdateAccountAsync,回傳 ApiResponseModel<AccountResponse> (處理 NOT_FOUND, CONCURRENT_UPDATE_CONFLICT, VALIDATION_ERROR 錯誤碼)
- [ ] T047 [US3] 更新 Controllers/AccountController.cs - 實作 PUT /api/accounts/{id}/password 端點,接收 ChangePasswordRequest,轉換為 ChangePasswordDto,呼叫 AccountService.ChangePasswordAsync,回傳 ApiResponseModel (處理 INVALID_CREDENTIALS, PASSWORD_SAME_AS_OLD, CONCURRENT_UPDATE_CONFLICT 錯誤碼)

**Checkpoint**: 此時 User Stories 1, 2, 3 應該都能獨立運作

---

## Phase 6: User Story 4 - 刪除帳號 (Priority: P3)

**目標**: 實作刪除帳號功能 (軟刪除),驗證不可刪除當前登入帳號與最後一個有效帳號,需二次確認

**獨立測試**: 登入系統後,使用 Swagger UI 或 Postman 呼叫 DELETE /api/accounts/{id},驗證帳號被標記為已刪除且無法登入

### US4 - 資料模型

- [X] T048 [P] [US4] 建立 Models/Requests/DeleteAccountRequest.cs - 刪除帳號請求模型,包含 ConfirmText 屬性 (必須為 "CONFIRM")
- [X] T049 [P] [US4] 建立 Models/Responses/AccountListResponse.cs - 帳號列表回應模型,包含 Items (List<AccountResponse>), TotalCount, PageNumber, PageSize 屬性
- [X] T050 [P] [US4] 建立 Models/Dtos/AccountListDto.cs - 帳號列表 Dto,包含 Items (List<AccountDto>), TotalCount, PageNumber, PageSize 屬性

### US4 - 驗證器

- [X] T051 [US4] 建立 Validators/DeleteAccountRequestValidator.cs - DeleteAccountRequest 驗證器,驗證 ConfirmText (必填, 必須等於 "CONFIRM"),繁體中文錯誤訊息

### US4 - 服務層

- [X] T052 更新 Services/Interfaces/IAccountService.cs - 加入 GetAccountByIdAsync(Guid id), GetAccountsAsync(int pageNumber, int pageSize), DeleteAccountAsync(Guid id, Guid operatorId) 方法
- [X] T053 更新 Services/AccountService.cs - 實作 GetAccountByIdAsync (查詢單一使用者並轉換為 AccountDto),實作 GetAccountsAsync (分頁查詢有效帳號並轉換為 AccountListDto),實作 DeleteAccountAsync (軟刪除邏輯: 檢查不可刪除自己、檢查至少保留一個有效帳號、設定 IsDeleted, DeletedAt, DeletedBy)

### US4 - 控制器

- [X] T054 [US4] 更新 Controllers/AccountController.cs - 實作 GET /api/accounts 端點 (查詢帳號列表),接收 pageNumber, pageSize 查詢參數,呼叫 AccountService.GetAccountsAsync,將 AccountListDto 轉換為 AccountListResponse,包裝為 ApiResponseModel<AccountListResponse> 回傳
- [X] T055 [US4] 更新 Controllers/AccountController.cs - 實作 GET /api/accounts/{id} 端點 (查詢單一帳號),呼叫 AccountService.GetAccountByIdAsync,將 AccountDto 轉換為 AccountResponse,包裝為 ApiResponseModel<AccountResponse> 回傳 (處理 NOT_FOUND 錯誤碼)
- [X] T056 [US4] 更新 Controllers/AccountController.cs - 實作 DELETE /api/accounts/{id} 端點,接收 DeleteAccountRequest,驗證 ConfirmText,從 JWT Claims 取得當前使用者 ID,呼叫 AccountService.DeleteAccountAsync,回傳 ApiResponseModel (處理 CANNOT_DELETE_SELF, LAST_ACCOUNT_CANNOT_DELETE, NOT_FOUND 錯誤碼)

**Checkpoint**: 所有使用者故事現在應該都能獨立運作

---

## Phase 7: Polish & Cross-Cutting Concerns (完善與跨領域關注點)

**目的**: 改善影響多個使用者故事的功能

- [ ] T057 [P] 為所有 Models, Services, Controllers 補充完整的 XML 文件註解 (繁體中文,遵循 C# 13 最佳實踐)
- [ ] T058 [P] 更新 README.md - 加入專案說明、技術堆疊、環境設定、API 端點清單、快速開始指南
- [ ] T059 程式碼重構與清理 - 遵循 C# 13 最佳實踐、移除重複程式碼、改善命名、確保一致的錯誤處理
- [ ] T060 [P] 效能最佳化 - 確保所有資料庫操作使用 async/await、檢查查詢效能、加入適當的索引
- [ ] T061 [P] 安全性強化 - 驗證 JWT Token 配置、檢查輸入驗證完整性、確認密碼雜湊 work factor、審查 SQL 參數化查詢
- [ ] T062 專案憲法合規性驗證 - 檢查所有功能是否符合專案憲法要求 (ApiResponseModel, TraceId, 繁體中文錯誤訊息, 三層架構, XML 註解)
- [ ] T063 執行 quickstart.md 驗證 - 按照 quickstart.md 步驟執行環境設定、資料庫遷移、API 測試,確保文件正確性
- [ ] T064 [P] 更新 Swagger/OpenAPI 文件 - 確保所有端點都有完整的描述、範例、錯誤碼說明,與 api-spec.yaml 一致

---

## Dependencies & Execution Order (相依性與執行順序)

### Phase Dependencies (階段相依性)

- **Setup (Phase 1)**: 無相依性 - 可立即開始
- **Foundational (Phase 2)**: 相依於 Setup 完成 - 阻塞所有使用者故事
- **User Stories (Phase 3-6)**: 全部相依於 Foundational phase 完成
  - 使用者故事之後可以並行進行 (如果有足夠人力)
  - 或按優先順序循序執行 (P1 → P2 → P2 → P3)
- **Polish (Final Phase)**: 相依於所有期望的使用者故事完成

### User Story Dependencies (使用者故事相依性)

- **User Story 1 (P1) - 帳號密碼登入**: 可在 Foundational (Phase 2) 完成後開始 - 不相依於其他故事
- **User Story 2 (P2) - 新增帳號**: 可在 Foundational (Phase 2) 完成後開始 - 不相依於 US1,但實務上需要 US1 來取得 JWT Token 進行測試
- **User Story 3 (P2) - 修改個人資料**: 可在 Foundational (Phase 2) 完成後開始 - 需要 US1 (登入) 與 US2 (建立測試帳號) 來進行完整測試,但核心邏輯獨立
- **User Story 4 (P3) - 刪除帳號**: 可在 Foundational (Phase 2) 完成後開始 - 需要 US1 (登入) 與 US2 (建立測試帳號) 來進行完整測試,但核心邏輯獨立

### Within Each User Story (每個使用者故事內部)

- 資料模型優先於服務
- 驗證器優先於控制器
- 服務優先於控制器
- 核心實作優先於整合
- 故事完成後才進入下一個優先順序

### Parallel Opportunities (並行機會)

#### Phase 1 (Setup) - 並行任務
- T003 (editorconfig) 可與 T004 (appsettings) 並行

#### Phase 2 (Foundational) - 並行任務
- T006 (seed.sql) 可在 T005 完成後開始
- T008 (DatabaseSettings) 與 T009 (JwtSettings) 可並行
- T012 (TraceIdMiddleware) 與 T013 (ExceptionHandlingMiddleware) 可並行
- T015 與 T016 (IJwtService 與實作) 需循序執行
- T017 與 T018 (IUserRepository 與實作) 需循序執行
- T020 (Swagger) 可獨立並行

#### Phase 3 (US1) - 並行任務
```bash
# 資料模型可並行建立:
T021 (LoginRequest), T022 (LoginResponse), T023 (AccountResponse)
T024 (LoginDto), T025 (LoginResultDto), T026 (AccountDto)

# 之後循序執行:
T027 (驗證器) → T028/T029 (服務介面與實作) → T030 (控制器)
```

#### Phase 4 (US2) - 並行任務
```bash
# 資料模型可並行建立:
T031 (CreateAccountRequest), T032 (CreateAccountDto)

# 之後循序執行:
T033 (驗證器) → T034/T035 (服務介面與實作) → T036 (DI 註冊) → T037 (控制器)
```

#### Phase 5 (US3) - 並行任務
```bash
# 資料模型可並行建立:
T038 (UpdateAccountRequest), T039 (ChangePasswordRequest)
T040 (UpdateAccountDto), T041 (ChangePasswordDto)

# 驗證器可並行建立:
T042 (UpdateAccountRequestValidator), T043 (ChangePasswordRequestValidator)

# 之後循序執行:
T044/T045 (更新服務介面與實作) → T046/T047 (控制器端點)
```

#### Phase 6 (US4) - 並行任務
```bash
# 資料模型可並行建立:
T048 (DeleteAccountRequest), T049 (AccountListResponse), T050 (AccountListDto)

# 之後循序執行:
T051 (驗證器) → T052/T053 (更新服務介面與實作) → T054/T055/T056 (控制器端點)
```

#### Phase 7 (Polish) - 並行任務
```bash
# 大部分任務可並行:
T057 (XML 註解), T058 (README), T060 (效能), T061 (安全性), T064 (Swagger)

# 需循序執行:
T059 (重構) → T062 (憲法驗證) → T063 (quickstart 驗證)
```

### Parallel Team Strategy (並行團隊策略)

**多人開發時**:

1. **階段 1-2**: 團隊一起完成 Setup + Foundational
2. **Foundational 完成後**:
   - 開發者 A: User Story 1 (T021-T030)
   - 開發者 B: User Story 2 (T031-T037)
   - 開發者 C: User Story 3 (T038-T047)
   - 開發者 D: User Story 4 (T048-T056)
3. 各故事獨立完成並整合

**單人開發時** (建議順序):
1. Setup (T001-T004)
2. Foundational (T005-T020) - 完整建立基礎設施
3. US1 (T021-T030) - MVP: 登入功能
4. US2 (T031-T037) - 新增帳號
5. US3 (T038-T047) - 修改資料
6. US4 (T048-T056) - 刪除帳號
7. Polish (T057-T064) - 完善與優化

---

## Parallel Example: User Story 1 (並行範例)

```bash
# 同時啟動 User Story 1 的所有資料模型任務:
Task T021: "建立 Models/Requests/LoginRequest.cs"
Task T022: "建立 Models/Responses/LoginResponse.cs"
Task T023: "建立 Models/Responses/AccountResponse.cs"
Task T024: "建立 Models/Dtos/LoginDto.cs"
Task T025: "建立 Models/Dtos/LoginResultDto.cs"
Task T026: "建立 Models/Dtos/AccountDto.cs"

# 資料模型完成後,循序執行:
Task T027: "建立驗證器"
Task T028-T029: "建立服務介面與實作"
Task T030: "實作控制器端點"
```

---

## Implementation Strategy (實作策略)

### MVP First (僅 User Story 1)

1. 完成 Phase 1: Setup (T001-T004)
2. 完成 Phase 2: Foundational (T005-T020) **關鍵 - 阻塞所有故事**
3. 完成 Phase 3: User Story 1 (T021-T030)
4. **停止並驗證**: 獨立測試 User Story 1
5. 準備好時部署/展示

### Incremental Delivery (增量交付)

1. **完成 Setup + Foundational** → 基礎就緒
2. **加入 User Story 1** → 獨立測試 → 部署/展示 (MVP!)
3. **加入 User Story 2** → 獨立測試 → 部署/展示
4. **加入 User Story 3** → 獨立測試 → 部署/展示
5. **加入 User Story 4** → 獨立測試 → 部署/展示
6. **完成 Polish** → 最終版本
7. 每個故事都增加價值而不會破壞先前的故事

### Recommended MVP Scope (建議的 MVP 範圍)

**最小可行產品 (MVP)**: Phase 1 + Phase 2 + Phase 3 (User Story 1)

這將提供:
- ✅ 基礎設施完整 (資料庫、JWT、中介軟體、錯誤處理)
- ✅ 使用者可以登入系統
- ✅ JWT Token 驗證機制運作
- ✅ ApiResponseModel 回應格式統一
- ✅ 可以展示核心功能並收集回饋

**建議的第二個增量**: 加入 User Story 2 (新增帳號)
- 這使系統能夠管理多個使用者
- 可以開始進行使用者管理測試

---

## Task Summary (任務總結)

- **總任務數**: 64 個任務
- **Phase 1 (Setup)**: 4 個任務
- **Phase 2 (Foundational)**: 16 個任務
- **Phase 3 (US1 - 登入)**: 10 個任務
- **Phase 4 (US2 - 新增帳號)**: 7 個任務
- **Phase 5 (US3 - 修改資料)**: 10 個任務
- **Phase 6 (US4 - 刪除帳號)**: 9 個任務
- **Phase 7 (Polish)**: 8 個任務

### Parallelizable Tasks (可並行任務)

- Phase 1: 2 個並行機會
- Phase 2: 6 個並行機會
- Phase 3: 6 個並行任務 (資料模型)
- Phase 4: 2 個並行任務 (資料模型)
- Phase 5: 4 個並行任務 (資料模型與驗證器)
- Phase 6: 3 個並行任務 (資料模型)
- Phase 7: 5 個並行任務

**總並行機會**: 28 個任務可並行執行

### Independent Test Criteria (獨立測試準則)

每個使用者故事都可以獨立測試:

- **US1**: 使用 Swagger 呼叫登入 API,驗證能取得 JWT Token
- **US2**: 帶入 Token 呼叫新增帳號 API,驗證新帳號可以登入
- **US3**: 帶入 Token 呼叫修改資料 API,驗證變更生效
- **US4**: 帶入 Token 呼叫刪除帳號 API,驗證帳號無法登入

---

## Notes (注意事項)

- **[P] 任務** = 不同檔案,無相依性,可並行執行
- **[Story] 標籤** = 將任務映射到特定的使用者故事以便追蹤
- 每個使用者故事應該可以獨立完成和測試
- 在實作前先建立資料模型
- 每個任務或邏輯群組完成後提交
- 在任何檢查點停止以獨立驗證故事
- **避免**: 模糊的任務、同一檔案衝突、破壞獨立性的跨故事相依性

---

## Format Validation (格式驗證)

✅ 所有任務都遵循檢查清單格式:
- ✅ 核取方塊: `- [ ]`
- ✅ 任務 ID: T001, T002, ..., T064
- ✅ [P] 標記: 用於可並行任務
- ✅ [Story] 標籤: US1, US2, US3, US4 用於使用者故事階段
- ✅ 描述: 清晰的動作與確切的檔案路徑

✅ **所有任務都包含特定的檔案路徑**,使 LLM 可以在沒有額外上下文的情況下完成它們。

---

**Generated**: 2025-10-27
**Branch**: 001-account-management
**Status**: Ready for Implementation
**Next**: 開始執行 Phase 1 任務
