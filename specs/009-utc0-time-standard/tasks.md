# Task Breakdown: UTC0 時間標準化

**Feature**: UTC0 時間標準化  
**Branch**: `009-utc0-time-standard`  
**Generated**: 2026-01-24

## 📋 概覽

本文件將功能規格分解為可執行的開發任務,按照用戶故事優先級組織。

**總任務數**: 37 個任務  
**用戶故事**: 4 個 (US1-P1, US2-P1, US3-P2, US4-P3)  
**預計工期**: 6-7 個工作天

## 🎯 實作策略

### MVP 範圍 (最小可行產品)
建議先完成 **User Story 1 + User Story 2** 作為 MVP:
- US1: 前端送出 UTC0 時間至後端 (P1)
- US2: 後端回傳 UTC0 時間至前端 (P1)

這兩個故事構成時間標準化的核心流程,完成後即可交付基本功能。

### 漸進式交付
1. **第一階段 (MVP)**: Setup + Foundational + US1 + US2 = 核心時間處理功能
2. **第二階段**: US3 = 資料庫遷移和永久儲存
3. **第三階段**: US4 + Polish = 文件和最佳化

## 📊 任務統計

| Phase | 任務數 | 並行機會 | 預計時間 |
|-------|--------|----------|----------|
| Phase 1: Setup | 5 | 0 | 0.5 天 |
| Phase 2: Foundational | 8 | 3 | 1 天 |
| Phase 3: US1 (P1) | 6 | 2 | 1 天 |
| Phase 4: US2 (P1) | 5 | 2 | 0.75 天 |
| Phase 5: US3 (P2) | 6 | 1 | 1.5 天 |
| Phase 6: US4 (P3) | 4 | 2 | 0.5 天 |
| Phase 7: Polish | 3 | 1 | 0.5 天 |

## 🔄 依賴關係

### 用戶故事完成順序

```
Phase 1 (Setup) → Phase 2 (Foundational)
                    ↓
        ┌───────────┴───────────┐
        ↓                       ↓
   Phase 3 (US1)          Phase 4 (US2)
        └───────────┬───────────┘
                    ↓
             Phase 5 (US3)
                    ↓
             Phase 6 (US4)
                    ↓
             Phase 7 (Polish)
```

**說明**:
- Setup 和 Foundational 是所有故事的前置條件
- US1 和 US2 可以並行開發 (處理不同的資料流向)
- US3 依賴 US1 和 US2 (需要先確定時間格式再遷移資料)
- US4 可在 US3 之後任何時間進行 (文件更新)
- Polish 在所有功能完成後進行

### 技術依賴
- **JSON 序列化器** → 影響所有 API 端點
- **時間驗證器** → 影響所有接收時間的 API
- **資料庫配置** → 影響所有資料持久化

---

## Phase 1: Setup 專案初始化

**目標**: 建立專案基礎結構和配置

**獨立測試標準**: 專案可以成功編譯且所有新資料夾結構正確建立

### 任務清單

- [X] T001 建立 Converters 資料夾於 V3.Admin.Backend/
- [X] T002 建立單元測試資料夾結構 V3.Admin.Backend/Tests/Unit/Validators/ 和 Tests/Unit/Converters/
- [X] T003 建立整合測試資料夾 V3.Admin.Backend/Tests/Integration/Controllers/
- [X] T004 在 V3.Admin.Backend.csproj 中確認所有必要的 NuGet 套件 (FluentValidation, Serilog, xUnit, Testcontainers)
- [X] T005 驗證專案編譯成功 (執行 dotnet build)

---

## Phase 2: Foundational 基礎建設

**目標**: 實作所有用戶故事共用的基礎設施

**獨立測試標準**: 基礎元件可獨立測試且所有單元測試通過

### 任務清單

#### JSON 序列化配置
- [X] T006 [P] 建立 Utc0DateTimeJsonConverter.cs 於 V3.Admin.Backend/Converters/ (實作自訂 JsonConverter,強制序列化/反序列化為 UTC0 格式,毫秒精度)
- [X] T007 [P] 建立 Utc0DateTimeJsonConverterTests.cs 於 V3.Admin.Backend/Tests/Unit/Converters/ (測試各種時間格式的序列化/反序列化,包含邊界條件)
- [X] T008 修改 Program.cs 配置 JsonSerializerOptions 註冊 Utc0DateTimeJsonConverter

#### 時間驗證
- [X] T009 [P] 建立 Utc0DateTimeValidator.cs 於 V3.Admin.Backend/Validators/ (FluentValidation 自訂驗證器,檢查 ISO 8601 格式)
- [X] T010 [P] 建立 Utc0DateTimeValidatorTests.cs 於 V3.Admin.Backend/Tests/Unit/Validators/ (測試各種有效/無效格式,錯誤訊息驗證)

#### 資料庫配置
- [X] T011 修改 Program.cs 或 DatabaseSettings.cs 加入 PostgreSQL 連線字串的 Timezone=UTC 參數
- [X] T012 建立資料庫配置驗證測試 V3.Admin.Backend/Tests/Integration/DatabaseConfigTests.cs (驗證連線使用 UTC 時區)

#### Serilog 配置
- [X] T013 修改 Program.cs 的 Serilog 配置,OutputTemplate 使用 UTC 時間戳記 (UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffZ)

---

## Phase 3: User Story 1 - 前端送出 UTC0 時間至後端 (P1)

**用戶故事**: 當前端使用者進行任何需要時間資料的操作,前端應將時間轉換為 UTC0 格式後傳送至後端 API

**目標**: 確保後端接收並驗證所有從前端送來的 UTC0 時間資料

**獨立測試標準**: 
- 送出正確 UTC0 格式的請求可成功處理
- 送出非 UTC0 格式的請求會被拒絕並回傳 400 Bad Request

### 任務清單

#### API 請求模型更新
- [X] T014 [P] [US1] 識別所有包含時間欄位的 Request 模型 (搜尋 Models/Requests/ 目錄中的 DateTime 屬性)
- [X] T015 [P] [US1] 更新所有 Request 模型的時間屬性為 DateTimeOffset 類型 (確保時區資訊不丟失)

#### 驗證邏輯整合
- [X] T016 [US1] 在所有時間相關的 Validator 中整合 Utc0DateTimeValidator (例如: CreateAccountRequestValidator, UpdateServiceOrderRequestValidator 等)
- [X] T017 [US1] 更新 ExceptionHandlingMiddleware 處理 ValidationException 和 JsonException,回傳標準化錯誤訊息

#### 整合測試
- [X] T018 [US1] 建立 TimeFormatIntegrationTests.cs 於 V3.Admin.Backend/Tests/Integration/Controllers/ (測試場景: 正確 UTC0 格式、錯誤格式、缺少時區識別符)
- [ ] T019 [US1] 執行整合測試驗證所有時間輸入端點 (需要 Docker 環境)

---

## Phase 4: User Story 2 - 後端回傳 UTC0 時間至前端 (P1)

**用戶故事**: 當後端處理完請求並回傳資料時,所有時間欄位都應維持 UTC0 格式

**目標**: 確保後端回傳的所有時間資料都是 UTC0 格式且包含正確的時區識別符

**獨立測試標準**:
- 所有 API 回應中的時間欄位都是 YYYY-MM-DDTHH:mm:ss.fffZ 格式
- 時間值正確且時區為 UTC

### 任務清單

#### API 回應模型更新
- [ ] T020 [P] [US2] 識別所有包含時間欄位的 Response 模型和 DTO (搜尋 Models/Responses/ 和 Models/Dtos/ 目錄)
- [ ] T021 [P] [US2] 更新所有 Response 模型和 DTO 的時間屬性為 DateTimeOffset 類型

#### Service 層更新
- [ ] T022 [US2] 審查所有 Service 層方法,確保從 Repository 取得的時間資料轉換為 UTC0 (如需要)
- [ ] T023 [US2] 更新 Service 層的時間處理邏輯,使用 DateTimeOffset.UtcNow 取代 DateTime.Now

#### 整合測試
- [ ] T024 [US2] 擴充 TimeFormatIntegrationTests.cs 加入回應時間格式驗證測試 (測試各種 GET/POST 端點的回應)

---

## Phase 5: User Story 3 - 資料庫時間儲存標準化 (P2)

**用戶故事**: 系統中所有時間資料都應使用 UTC0 格式儲存於資料庫

**目標**: 確保資料持久層的時間格式統一為 UTC0

**獨立測試標準**:
- 資料庫中所有時間欄位都是 UTC0 格式
- 新寫入的資料使用 UTC0 格式
- 舊資料已成功遷移為 UTC0 格式

### 任務清單

#### 資料分析與遷移準備
- [ ] T025 [US3] 建立資料庫分析腳本 V3.Admin.Backend/Database/Scripts/AnalyzeTimeFields.sql (識別所有時間欄位和當前資料格式)
- [ ] T026 [US3] 執行分析腳本並記錄結果 (產生待遷移欄位清單)

#### Repository 層更新
- [ ] T027 [P] [US3] 更新所有 Repository 的 INSERT/UPDATE 查詢,確保時間參數使用 DateTimeOffset 且明確轉換為 UTC
- [ ] T028 [US3] 更新所有 Repository 的 SELECT 查詢,確保讀取的時間資料正確解析為 DateTimeOffset

#### 資料遷移
- [ ] T029 [US3] 建立資料遷移腳本 V3.Admin.Backend/Database/Migrations/009_MigrateToUtc0.sql (包含備份、轉換、驗證邏輯,使用參數化查詢)
- [ ] T030 [US3] 在測試環境執行遷移腳本並驗證結果 (建立驗證查詢檢查資料正確性)

---

## Phase 6: User Story 4 - API 文件時間格式規範 (P3)

**用戶故事**: API 文件應明確標示時間欄位的格式要求

**目標**: 更新 Swagger 文件,為所有時間欄位加入格式說明和範例

**獨立測試標準**:
- Swagger UI 中所有時間欄位都有格式描述
- 範例時間都是正確的 UTC0 格式

### 任務清單

#### XML 文件註解
- [ ] T031 [P] [US4] 為所有包含時間欄位的 Request/Response 模型加入 XML 文件註解 (說明格式要求: YYYY-MM-DDTHH:mm:ss.fffZ)
- [ ] T032 [P] [US4] 為所有時間相關的 Controller 端點加入 XML 註解和範例 (使用 <example> 標籤提供正確範例)

#### Swagger 配置
- [ ] T033 [US4] 修改 Program.cs 的 Swagger 配置,啟用 XML 註解讀取 (IncludeXmlComments)
- [ ] T034 [US4] 驗證 Swagger UI 顯示正確的時間格式說明 (啟動應用程式檢查 /swagger 端點)

---

## Phase 7: Polish 收尾與跨領域關注點

**目標**: 完善整體實作,處理跨領域的優化和文件

**獨立測試標準**:
- 所有測試通過
- 效能符合預期
- 文件完整

### 任務清單

#### 效能測試
- [ ] T035 [P] 建立效能基準測試 V3.Admin.Backend/Tests/Performance/TimeSerializationBenchmark.cs (測試 JSON 序列化/反序列化效能)
- [ ] T036 執行完整的測試套件 (dotnet test) 確認所有測試通過且覆蓋率達標

#### 文件完善
- [ ] T037 建立 quickstart.md 於 specs/009-utc0-time-standard/ (說明如何在新端點中正確處理時間資料,包含程式碼範例)

---

## 🔀 並行執行機會

### Phase 2 (Foundational) 並行組
**組 1** (可同時進行):
- T006: 建立 Utc0DateTimeJsonConverter
- T009: 建立 Utc0DateTimeValidator

**組 2** (需要組 1 完成):
- T007: JsonConverter 測試
- T010: Validator 測試

**組 3** (獨立任務):
- T011: 資料庫配置
- T013: Serilog 配置

### Phase 3 (US1) 並行組
- T014 和 T015 可同時進行 (不同開發者處理不同模型檔案)

### Phase 4 (US2) 並行組
- T020 和 T021 可同時進行 (Response 模型和 DTO 是不同檔案)

### Phase 5 (US3) 並行組
- T027 (Repository 層) 可在 T026 完成後與 T028 部分並行

### Phase 6 (US4) 並行組
- T031 和 T032 可同時進行 (不同開發者處理不同檔案)

### Phase 7 (Polish) 並行組
- T035 (效能測試) 和 T037 (文件) 可同時進行

---

## 📝 實作注意事項

### 時間類型選擇
- **建議使用**: `DateTimeOffset` (明確包含時區資訊)
- **避免使用**: `DateTime` (時區資訊不明確,容易造成混淆)

### JSON 序列化格式
```json
{
  "createdAt": "2026-01-24T06:00:00.000Z",
  "updatedAt": "2026-01-24T08:30:15.123Z"
}
```

### 驗證錯誤訊息範例
```json
{
  "success": false,
  "message": "Invalid datetime format. Expected UTC0 ISO 8601 format with milliseconds: YYYY-MM-DDTHH:mm:ss.fffZ",
  "data": null
}
```

### 資料庫連線字串範例
```
Host=localhost;Database=v3admin;Username=postgres;Password=***;Timezone=UTC
```

### Serilog 配置範例
```csharp
.WriteTo.Console(outputTemplate: "[{UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffZ}] {Level:u3} {Message:lj}{NewLine}{Exception}")
```

---

## ✅ 驗收檢查清單

完成所有任務後,確認以下項目:

- [ ] 所有 37 個任務都已完成
- [ ] 所有單元測試通過 (覆蓋率 100%)
- [ ] 所有整合測試通過
- [ ] API 接收非 UTC0 格式時間會正確拒絕並回傳 400
- [ ] API 回應中的所有時間都是 UTC0 格式
- [ ] 資料庫中所有時間資料都是 UTC0 格式
- [ ] Swagger 文件正確顯示時間格式說明和範例
- [ ] 系統日誌使用 UTC 時間戳記
- [ ] 資料遷移腳本已在測試環境驗證成功
- [ ] 效能測試通過 (序列化效能無顯著退化)
- [ ] quickstart.md 文件完整且清晰
- [ ] 與前端團隊協調發布時程

---

## 📚 相關文件

- [spec.md](spec.md) - 功能規格
- [plan.md](plan.md) - 實作計畫
- [quickstart.md](quickstart.md) - 快速開始指南 (Phase 7 產出)

**完成時間**: Phase 1-7 預計 6-7 個工作天
