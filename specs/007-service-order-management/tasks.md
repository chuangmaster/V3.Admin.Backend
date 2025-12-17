# Tasks: 服務單管理模組

**Input**: Design documents from `/specs/007-service-order-management/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md  
**Feature Branch**: `007-service-order-management`  
**Date**: 2025-12-18

**Tests**: 依據規格要求,本功能需包含單元測試與整合測試

**Organization**: 任務依使用者故事分組,以實現獨立實作與測試

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行 (不同檔案,無依賴關係)
- **[Story]**: 任務所屬使用者故事 (US1, US2, US3...)
- 描述中包含明確的檔案路徑

---

## Phase 1: Setup (專案初始化)

**Purpose**: 建立專案基礎結構與必要依賴套件

- [ ] T001 安裝 Azure Blob Storage SDK (`Azure.Storage.Blobs` 12.x)
- [ ] T002 安裝 Azure Computer Vision SDK (`Azure.AI.Vision.ImageAnalysis` 1.x)
- [ ] T003 安裝 Google Cloud AI Platform SDK (`Google.Cloud.AIPlatform.V1` 3.x)
- [ ] T004 安裝 PDFsharp (`PDFsharp` 6.x)
- [ ] T005 [P] 新增環境變數配置 (`appsettings.Development.json`): `AZURE_BLOB_CONNECTION_STRING`, `AZURE_VISION_ENDPOINT`, `AZURE_VISION_API_KEY`, `GOOGLE_GEMINI_API_KEY`, `DROPBOX_SIGN_API_KEY`
- [ ] T006 [P] 建立 Azure Blob Storage 配置類別 `Configuration/AzureBlobStorageSettings.cs`
- [ ] T007 [P] 建立 Azure Vision 配置類別 `Configuration/AzureVisionSettings.cs`
- [ ] T008 [P] 建立 Google Gemini 配置類別 `Configuration/GoogleGeminiSettings.cs`
- [ ] T009 [P] 建立 Dropbox Sign 配置類別 `Configuration/DropboxSignSettings.cs`

---

## Phase 2: Foundational (核心基礎建設)

**Purpose**: 完成所有使用者故事依賴的基礎建設

**⚠️ CRITICAL**: 此階段完成前無法開始任何使用者故事實作

### 資料庫 Schema

- [ ] T010 建立資料庫遷移腳本 `Database/Migrations/007_CreateServiceOrderTables.sql` (建立 `customers`, `service_orders`, `product_items`, `attachments`, `signature_records`, `attachment_view_logs`, `dropbox_sign_webhook_events` 表)
- [ ] T011 建立序號生成函數與觸發器腳本 `Database/Migrations/008_CreateTriggers.sql` (`generate_daily_service_order_number` 函數與 `trg_service_orders_sequence` 觸發器)
- [ ] T012 建立測試資料種子腳本 `Database/Scripts/seed_service_order_test_data.sql`
- [ ] T013 執行資料庫遷移腳本 (007, 008) 於本地 PostgreSQL

### 實體模型 (Entities)

- [ ] T014 [P] 建立 `Customer` 實體 `Models/Entities/Customer.cs`
- [ ] T015 [P] 建立 `ServiceOrder` 實體 `Models/Entities/ServiceOrder.cs`
- [ ] T016 [P] 建立 `ProductItem` 實體 `Models/Entities/ProductItem.cs`
- [ ] T017 [P] 建立 `Attachment` 實體 `Models/Entities/Attachment.cs`
- [ ] T018 [P] 建立 `SignatureRecord` 實體 `Models/Entities/SignatureRecord.cs`
- [ ] T019 [P] 建立 `AttachmentViewLog` 實體 `Models/Entities/AttachmentViewLog.cs`
- [ ] T020 [P] 建立 `DropboxSignWebhookEvent` 實體 `Models/Entities/DropboxSignWebhookEvent.cs`

### 基礎服務介面

- [ ] T021 [P] 建立 `IBlobStorageService` 介面 `Services/Interfaces/IBlobStorageService.cs`
- [ ] T022 [P] 建立 `IIdCardOcrService` 介面 `Services/Interfaces/IIdCardOcrService.cs`
- [ ] T023 [P] 建立 `IPdfGeneratorService` 介面 `Services/Interfaces/IPdfGeneratorService.cs`
- [ ] T024 [P] 建立 `IDropboxSignService` 介面 `Services/Interfaces/IDropboxSignService.cs`

### 基礎服務實作

- [ ] T025 實作 `BlobStorageService` `Services/BlobStorageService.cs` (支援上傳檔案、產生 SAS Token、MemoryCache 快取機制)
- [ ] T026 實作 `IdCardOcrService` `Services/IdCardOcrService.cs` (Azure Vision 文字擷取 + Google Gemini 結構化解析,信心度評分機制,降級策略)
- [ ] T027 實作 `PdfGeneratorService` `Services/PdfGeneratorService.cs` (PDFsharp 填充模板、合併簽章、繁體中文字體支援)
- [ ] T028 實作 `DropboxSignService` `Services/DropboxSignService.cs` (發送簽名邀請、查詢簽名狀態、重新發送邀請)

### Webhook Middleware

- [ ] T029 建立 Dropbox Sign Webhook 驗證 Middleware `Middleware/DropboxSignWebhookMiddleware.cs` (HMAC-SHA256 驗證、時間戳檢查、Event Hash 防重複)

**Checkpoint**: 基礎建設完成 - 使用者故事實作可以開始

---

## Phase 3: User Story 1 - 線下收購單建立 (Priority: P1) 🎯 MVP

**Goal**: 店員可在店內協助客戶建立收購單,包含客戶資料、商品資訊、身分證上傳、AI 辨識、線下簽名

**Independent Test**: 店員可以完整建立一筆收購單（包含客戶資料、商品項目、身分證上傳、線下簽名），並在系統中查詢到該筆記錄

### DTOs for User Story 1

- [ ] T030 [P] [US1] 建立 `CustomerDto` `Models/Dtos/CustomerDto.cs`
- [ ] T031 [P] [US1] 建立 `ServiceOrderDto` `Models/Dtos/ServiceOrderDto.cs`
- [ ] T032 [P] [US1] 建立 `ProductItemDto` `Models/Dtos/ProductItemDto.cs`
- [ ] T033 [P] [US1] 建立 `AttachmentDto` `Models/Dtos/AttachmentDto.cs`
- [ ] T034 [P] [US1] 建立 `SignatureRecordDto` `Models/Dtos/SignatureRecordDto.cs`
- [ ] T035 [P] [US1] 建立 `OcrResultDto` `Models/Dtos/OcrResultDto.cs`

### Request/Response Models for User Story 1

- [ ] T036 [P] [US1] 建立 `CreateBuybackOrderRequest` `Models/Requests/CreateBuybackOrderRequest.cs`
- [ ] T037 [P] [US1] 建立 `CreateCustomerRequest` `Models/Requests/CreateCustomerRequest.cs`
- [ ] T038 [P] [US1] 建立 `SearchCustomerRequest` `Models/Requests/SearchCustomerRequest.cs`
- [ ] T039 [P] [US1] 建立 `OcrIdCardRequest` `Models/Requests/OcrIdCardRequest.cs`
- [ ] T040 [P] [US1] 建立 `MergeSignatureRequest` `Models/Requests/MergeSignatureRequest.cs`
- [ ] T041 [P] [US1] 建立 `ServiceOrderResponse` `Models/Responses/ServiceOrderResponse.cs`
- [ ] T042 [P] [US1] 建立 `CustomerResponse` `Models/Responses/CustomerResponse.cs`
- [ ] T043 [P] [US1] 建立 `OcrResultResponse` `Models/Responses/OcrResultResponse.cs`

### Validators for User Story 1

- [ ] T044 [P] [US1] 建立 `CreateBuybackOrderRequestValidator` `Validators/CreateBuybackOrderRequestValidator.cs`
- [ ] T045 [P] [US1] 建立 `CreateCustomerRequestValidator` `Validators/CreateCustomerRequestValidator.cs`
- [ ] T046 [P] [US1] 建立 `SearchCustomerRequestValidator` `Validators/SearchCustomerRequestValidator.cs`
- [ ] T047 [P] [US1] 建立 `OcrIdCardRequestValidator` `Validators/OcrIdCardRequestValidator.cs`
- [ ] T048 [P] [US1] 建立 `MergeSignatureRequestValidator` `Validators/MergeSignatureRequestValidator.cs`

### Repository Interfaces for User Story 1

- [ ] T049 [P] [US1] 建立 `ICustomerRepository` 介面 `Repositories/Interfaces/ICustomerRepository.cs`
- [ ] T050 [P] [US1] 建立 `IServiceOrderRepository` 介面 `Repositories/Interfaces/IServiceOrderRepository.cs`
- [ ] T051 [P] [US1] 建立 `IProductItemRepository` 介面 `Repositories/Interfaces/IProductItemRepository.cs`
- [ ] T052 [P] [US1] 建立 `IAttachmentRepository` 介面 `Repositories/Interfaces/IAttachmentRepository.cs`
- [ ] T053 [P] [US1] 建立 `ISignatureRecordRepository` 介面 `Repositories/Interfaces/ISignatureRecordRepository.cs`

### Repository Implementations for User Story 1

- [ ] T054 [P] [US1] 實作 `CustomerRepository` `Repositories/CustomerRepository.cs` (SearchAsync、CreateAsync、GetByIdAsync、GetByIdNumberAsync)
- [ ] T055 [P] [US1] 實作 `ServiceOrderRepository` `Repositories/ServiceOrderRepository.cs` (CreateAsync、GetByIdAsync、GetByOrderNumberAsync、UpdateStatusAsync、樂觀鎖機制)
- [ ] T056 [P] [US1] 實作 `ProductItemRepository` `Repositories/ProductItemRepository.cs` (BatchCreateAsync、GetByServiceOrderIdAsync)
- [ ] T057 [P] [US1] 實作 `AttachmentRepository` `Repositories/AttachmentRepository.cs` (CreateAsync、GetByServiceOrderIdAsync、SoftDeleteAsync)
- [ ] T058 [P] [US1] 實作 `SignatureRecordRepository` `Repositories/SignatureRecordRepository.cs` (CreateAsync、GetByServiceOrderIdAsync)

### Service Interfaces for User Story 1

- [ ] T059 [P] [US1] 建立 `ICustomerService` 介面 `Services/Interfaces/ICustomerService.cs`
- [ ] T060 [P] [US1] 建立 `IServiceOrderService` 介面 `Services/Interfaces/IServiceOrderService.cs`

### Service Implementations for User Story 1

- [ ] T061 [US1] 實作 `CustomerService` `Services/CustomerService.cs` (SearchCustomersAsync、CreateCustomerAsync、GetByIdNumberAsync,整合 AuditLogService 記錄操作)
- [ ] T062 [US1] 實作 `ServiceOrderService` `Services/ServiceOrderService.cs` (CreateBuybackOrderAsync 方法,包含序號生成、附件儲存、簽名記錄、AuditLog 記錄、交易管理)

### Controller for User Story 1

- [ ] T063 [US1] 實作 `CustomerController` `Controllers/CustomerController.cs` (SearchCustomers、CreateCustomer API 端點)
- [ ] T064 [US1] 實作 `OcrController` `Controllers/OcrController.cs` (RecognizeIdCard API 端點,呼叫 IdCardOcrService)
- [ ] T065 [US1] 實作 `ServiceOrderController` `Controllers/ServiceOrderController.cs` (CreateBuybackOrder、PreviewBuybackContractPdf、MergeSignature、ConfirmOrder API 端點)

### Unit Tests for User Story 1

- [ ] T066 [P] [US1] 撰寫 `CreateBuybackOrderRequestValidatorTests` `Tests/Unit/Validators/CreateBuybackOrderRequestValidatorTests.cs`
- [ ] T067 [P] [US1] 撰寫 `CreateCustomerRequestValidatorTests` `Tests/Unit/Validators/CreateCustomerRequestValidatorTests.cs`
- [ ] T068 [P] [US1] 撰寫 `CustomerServiceTests` `Tests/Unit/Services/CustomerServiceTests.cs`
- [ ] T069 [P] [US1] 撰寫 `ServiceOrderServiceTests` `Tests/Unit/Services/ServiceOrderServiceTests.cs`
- [ ] T070 [P] [US1] 撰寫 `IdCardOcrServiceTests` `Tests/Unit/Services/IdCardOcrServiceTests.cs`

### Integration Tests for User Story 1

- [ ] T071 [P] [US1] 撰寫 `CustomerControllerTests` `Tests/Integration/Controllers/CustomerControllerTests.cs` (使用 Testcontainers PostgreSQL)
- [ ] T072 [P] [US1] 撰寫 `ServiceOrderControllerTests` `Tests/Integration/Controllers/ServiceOrderControllerTests.cs` (完整收購單建立流程測試)
- [ ] T073 [US1] 撰寫並發序號生成測試 `Tests/Integration/ServiceOrderConcurrencyTests.cs` (模擬 100 筆同時建立)

### Dependency Injection Registration for User Story 1

- [ ] T074 [US1] 在 `Program.cs` 註冊所有服務與 Repository (CustomerService, ServiceOrderService, CustomerRepository, ServiceOrderRepository, ProductItemRepository, AttachmentRepository, SignatureRecordRepository, BlobStorageService, IdCardOcrService, PdfGeneratorService)

**Checkpoint**: User Story 1 完成 - 可獨立測試收購單建立流程

---

## Phase 4: User Story 2 - 線下寄賣單建立 (Priority: P1)

**Goal**: 店員可協助客戶建立寄賣單,包含商品配件、瑕疵資訊、寄賣日期、續約設定

**Independent Test**: 店員可以完整建立一筆寄賣單（包含商品配件、瑕疵資訊、寄賣日期、續約設定、身分證上傳、線下簽名），並在系統中查詢到該筆記錄

### Request/Response Models for User Story 2

- [ ] T075 [P] [US2] 建立 `CreateConsignmentOrderRequest` `Models/Requests/CreateConsignmentOrderRequest.cs`
- [ ] T076 [P] [US2] 建立 `ConsignmentProductItemDto` `Models/Dtos/ConsignmentProductItemDto.cs` (包含 Accessories 與 Defects JSONB 欄位)

### Validators for User Story 2

- [ ] T077 [P] [US2] 建立 `CreateConsignmentOrderRequestValidator` `Validators/CreateConsignmentOrderRequestValidator.cs` (驗證寄賣日期、續約選項、配件與瑕疵多選)

### Service Methods for User Story 2

- [ ] T078 [US2] 在 `ServiceOrderService` 新增 `CreateConsignmentOrderAsync` 方法 (寄賣單建立邏輯,日期驗證、配件與瑕疵處理、AuditLog 記錄)

### Controller Endpoints for User Story 2

- [ ] T079 [US2] 在 `ServiceOrderController` 新增 `CreateConsignmentOrder`、`PreviewConsignmentContractPdf` API 端點

### Unit Tests for User Story 2

- [ ] T080 [P] [US2] 撰寫 `CreateConsignmentOrderRequestValidatorTests` `Tests/Unit/Validators/CreateConsignmentOrderRequestValidatorTests.cs`
- [ ] T081 [P] [US2] 撰寫寄賣單建立測試於 `ServiceOrderServiceTests`

### Integration Tests for User Story 2

- [ ] T082 [US2] 撰寫寄賣單建立完整流程測試於 `ServiceOrderControllerTests`

**Checkpoint**: User Story 1 與 User Story 2 均可獨立運作

---

## Phase 5: User Story 3 - 客戶搜尋與管理 (Priority: P2)

**Goal**: 店員可透過多種關鍵字搜尋既有客戶,或新增新客戶資料

**Independent Test**: 店員可以使用多種關鍵字（姓名、電話、Email、身分證字號）搜尋客戶,找到客戶後自動填入服務單表單;若找不到則新增新客戶,新增後可立即使用

### Enhanced Customer Search

- [ ] T083 [US3] 在 `CustomerRepository` 優化 `SearchAsync` 方法 (支援姓名模糊搜尋、電話精確搜尋、Email 模糊搜尋、身分證字號精確搜尋,建立索引)
- [ ] T084 [US3] 在 `CustomerService` 新增 `SearchCustomersAsync` 方法 (整合 AuditLog 記錄客戶搜尋操作)

### Unit Tests for User Story 3

- [ ] T085 [P] [US3] 撰寫客戶搜尋測試於 `CustomerServiceTests`

### Integration Tests for User Story 3

- [ ] T086 [US3] 撰寫客戶搜尋 API 測試於 `CustomerControllerTests`

**Checkpoint**: User Story 1, 2, 3 均可獨立運作

---

## Phase 6: User Story 5 - 服務單查詢與管理 (Priority: P2)

**Goal**: 店員可查詢、瀏覽已建立的服務單,包含篩選條件搜尋、查看詳細資訊、更新狀態、管理附件

**Independent Test**: 店員可以透過多種篩選條件（服務單類型、客戶名稱、日期範圍、狀態）搜尋服務單,查看詳細資訊、更新狀態,並查詢稽核日誌

### Request/Response Models for User Story 5

- [ ] T087 [P] [US5] 建立 `QueryServiceOrdersRequest` `Models/Requests/QueryServiceOrdersRequest.cs`
- [ ] T088 [P] [US5] 建立 `UpdateServiceOrderStatusRequest` `Models/Requests/UpdateServiceOrderStatusRequest.cs`
- [ ] T089 [P] [US5] 建立 `GenerateSasTokenRequest` `Models/Requests/GenerateSasTokenRequest.cs`
- [ ] T090 [P] [US5] 建立 `ServiceOrderListResponse` `Models/Responses/ServiceOrderListResponse.cs`
- [ ] T091 [P] [US5] 建立 `ServiceOrderDetailResponse` `Models/Responses/ServiceOrderDetailResponse.cs`

### Validators for User Story 5

- [ ] T092 [P] [US5] 建立 `QueryServiceOrdersRequestValidator` `Validators/QueryServiceOrdersRequestValidator.cs`
- [ ] T093 [P] [US5] 建立 `UpdateServiceOrderStatusRequestValidator` `Validators/UpdateServiceOrderStatusRequestValidator.cs`

### Repository Methods for User Story 5

- [ ] T094 [US5] 在 `ServiceOrderRepository` 新增 `QueryAsync` 方法 (支援多條件篩選、分頁查詢、排序)
- [ ] T095 [US5] 在 `ServiceOrderRepository` 新增 `UpdateStatusAsync` 方法 (狀態轉換驗證、樂觀鎖)
- [ ] T096 [US5] 在 `AttachmentRepository` 新增 `CreateViewLogAsync` 方法 (記錄附件查看日誌)

### Service Methods for User Story 5

- [ ] T097 [US5] 在 `ServiceOrderService` 新增 `QueryServiceOrdersAsync` 方法
- [ ] T098 [US5] 在 `ServiceOrderService` 新增 `GetServiceOrderDetailAsync` 方法
- [ ] T099 [US5] 在 `ServiceOrderService` 新增 `UpdateStatusAsync` 方法 (整合 AuditLog 記錄狀態變更)
- [ ] T100 [US5] 在 `BlobStorageService` 新增 `GenerateSasTokenAsync` 方法 (MemoryCache 快取機制)

### Controller Endpoints for User Story 5

- [ ] T101 [US5] 在 `ServiceOrderController` 新增 `QueryServiceOrders`、`GetServiceOrderDetail`、`UpdateStatus` API 端點
- [ ] T102 [US5] 建立 `AttachmentController` `Controllers/AttachmentController.cs` (GenerateSasToken API 端點,記錄附件查看日誌)

### Unit Tests for User Story 5

- [ ] T103 [P] [US5] 撰寫 `QueryServiceOrdersRequestValidatorTests` `Tests/Unit/Validators/QueryServiceOrdersRequestValidatorTests.cs`
- [ ] T104 [P] [US5] 撰寫服務單查詢測試於 `ServiceOrderServiceTests`
- [ ] T105 [P] [US5] 撰寫狀態更新測試於 `ServiceOrderServiceTests` (終態不可逆驗證)

### Integration Tests for User Story 5

- [ ] T106 [US5] 撰寫服務單查詢 API 測試於 `ServiceOrderControllerTests`
- [ ] T107 [US5] 撰寫附件查看日誌測試於 `AttachmentControllerTests`

**Checkpoint**: User Story 1, 2, 3, 5 均可獨立運作

---

## Phase 7: User Story 4 - 線上服務單建立 (Priority: P3)

**Goal**: 客戶可透過線上表單建立服務單,系統透過 Dropbox Sign API 將合約文件寄送至客戶 Email 供簽名

**Independent Test**: 客戶可透過線上表單建立服務單,完成表單送出後收到簽名邀請 Email,簽署完成後店員可在系統中查詢到該筆服務單與簽名記錄

### Request/Response Models for User Story 4

- [ ] T108 [P] [US4] 建立 `SendSignatureInvitationRequest` `Models/Requests/SendSignatureInvitationRequest.cs`
- [ ] T109 [P] [US4] 建立 `DropboxSignWebhookRequest` `Models/Requests/DropboxSignWebhookRequest.cs`
- [ ] T110 [P] [US4] 建立 `ResendSignatureInvitationRequest` `Models/Requests/ResendSignatureInvitationRequest.cs`

### Validators for User Story 4

- [ ] T111 [P] [US4] 建立 `SendSignatureInvitationRequestValidator` `Validators/SendSignatureInvitationRequestValidator.cs`
- [ ] T112 [P] [US4] 建立 `DropboxSignWebhookRequestValidator` `Validators/DropboxSignWebhookRequestValidator.cs`

### Repository for User Story 4

- [ ] T113 [P] [US4] 建立 `IDropboxSignWebhookEventRepository` 介面 `Repositories/Interfaces/IDropboxSignWebhookEventRepository.cs`
- [ ] T114 [US4] 實作 `DropboxSignWebhookEventRepository` `Repositories/DropboxSignWebhookEventRepository.cs` (IsEventProcessedAsync、MarkEventAsProcessedAsync)

### Service Methods for User Story 4

- [ ] T115 [US4] 在 `DropboxSignService` 新增 `SendSignatureInvitationAsync` 方法
- [ ] T116 [US4] 在 `DropboxSignService` 新增 `ResendSignatureInvitationAsync` 方法
- [ ] T117 [US4] 在 `ServiceOrderService` 新增處理 Webhook 簽名狀態更新的方法

### Controller Endpoints for User Story 4

- [ ] T118 [US4] 建立 `SignatureController` `Controllers/SignatureController.cs` (SendOnlineSignatureInvitation、ResendSignatureInvitation API 端點)
- [ ] T119 [US4] 建立 `WebhookController` `Controllers/WebhookController.cs` (DropboxSignWebhook API 端點,公開端點但需 API Key 驗證)

### Middleware for User Story 4

- [ ] T120 [US4] 在 `Program.cs` 註冊 `DropboxSignWebhookMiddleware`

### Unit Tests for User Story 4

- [ ] T121 [P] [US4] 撰寫 `DropboxSignServiceTests` `Tests/Unit/Services/DropboxSignServiceTests.cs`
- [ ] T122 [P] [US4] 撰寫 Webhook 驗證測試於 `DropboxSignWebhookMiddlewareTests`

### Integration Tests for User Story 4

- [ ] T123 [US4] 撰寫線上簽名完整流程測試於 `SignatureControllerTests` (使用 Dropbox Sign Sandbox 環境)
- [ ] T124 [US4] 撰寫 Webhook 端點測試於 `WebhookControllerTests`

### Dependency Injection Registration for User Story 4

- [ ] T125 [US4] 在 `Program.cs` 註冊 `DropboxSignService`、`DropboxSignWebhookEventRepository`

**Checkpoint**: 所有使用者故事均可獨立運作

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: 跨使用者故事的改進與優化

### Documentation

- [ ] T126 [P] 更新 API 契約文件 `specs/007-service-order-management/contracts/api-spec.yaml`
- [ ] T127 [P] 更新 `README.md` 新增服務單管理模組說明
- [ ] T128 [P] 建立權限設定文件 `specs/007-service-order-management/permissions.md`

### Performance Optimization

- [ ] T129 [P] 在 `service_orders` 表建立必要索引 (`idx_service_orders_customer_id`, `idx_service_orders_order_type`, `idx_service_orders_status`, `idx_service_orders_service_date`)
- [ ] T130 [P] 在 `product_items` 表建立 JSONB GIN 索引 (`idx_product_items_accessories`, `idx_product_items_defects`)
- [ ] T131 驗證 SAS Token 快取機制運作正常 (MemoryCache TTL 55 分鐘)

### Security Hardening

- [ ] T132 驗證所有 API 端點權限設定正確 (`serviceOrder.*.read`, `serviceOrder.*.create`, `serviceOrder.*.update`, `serviceOrder.attachment.viewSensitive`)
- [ ] T133 驗證軟刪除機制運作正常 (is_deleted 標記)
- [ ] T134 驗證樂觀鎖並發控制機制運作正常 (version 欄位)

### Integration & End-to-End Tests

- [ ] T135 執行完整的收購單建立流程測試 (客戶搜尋 → AI 辨識 → 建立服務單 → 線下簽名)
- [ ] T136 執行完整的寄賣單建立流程測試 (客戶搜尋 → AI 辨識 → 建立服務單 → 線下簽名)
- [ ] T137 執行服務單查詢與狀態管理流程測試
- [ ] T138 執行線上簽名完整流程測試 (發送邀請 → Webhook 更新狀態)

### Quickstart Validation

- [ ] T139 驗證 `quickstart.md` 所有流程可正常執行
- [ ] T140 執行並發測試 (100 位店員同時建立服務單)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無依賴 - 可立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - **阻擋所有使用者故事**
- **User Stories (Phase 3-7)**: 全部依賴 Foundational phase 完成
  - 使用者故事可平行進行 (若有足夠人力)
  - 或依優先順序循序執行 (P1 → P2 → P3)
- **Polish (Phase 8)**: 依賴所有期望的使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: 可在 Foundational (Phase 2) 後開始 - 無其他故事依賴
- **User Story 2 (P1)**: 可在 Foundational (Phase 2) 後開始 - 無其他故事依賴
- **User Story 3 (P2)**: 可在 Foundational (Phase 2) 後開始 - 增強 US1/US2 的客戶搜尋功能
- **User Story 5 (P2)**: 可在 Foundational (Phase 2) 後開始 - 查詢 US1/US2 建立的服務單
- **User Story 4 (P3)**: 可在 Foundational (Phase 2) 後開始 - 線上簽名為進階功能

### Within Each User Story

- DTOs 與 Request/Response Models 可平行建立
- Validators 可平行建立
- Repository 介面與實作可平行建立 (相同 Repository 除外)
- Service 依賴 Repository 完成
- Controller 依賴 Service 完成
- 單元測試可在對應類別完成後平行撰寫
- 整合測試依賴完整流程實作

### Parallel Opportunities

- Phase 1 所有任務可平行執行
- Phase 2 實體模型 (T014-T020) 可平行建立
- Phase 2 基礎服務介面 (T021-T024) 可平行建立
- 使用者故事完成 Foundational 後可平行開發 (若團隊人力充足)
- 每個使用者故事內的 DTOs、Validators、Repository 介面可平行建立
- 單元測試可平行撰寫

---

## Parallel Example: User Story 1

```bash
# 平行建立所有 DTOs:
Task T030: 建立 CustomerDto
Task T031: 建立 ServiceOrderDto
Task T032: 建立 ProductItemDto
Task T033: 建立 AttachmentDto
Task T034: 建立 SignatureRecordDto
Task T035: 建立 OcrResultDto

# 平行建立所有 Validators:
Task T044: 建立 CreateBuybackOrderRequestValidator
Task T045: 建立 CreateCustomerRequestValidator
Task T046: 建立 SearchCustomerRequestValidator
Task T047: 建立 OcrIdCardRequestValidator
Task T048: 建立 MergeSignatureRequestValidator

# 平行建立所有 Repository Interfaces:
Task T049: 建立 ICustomerRepository 介面
Task T050: 建立 IServiceOrderRepository 介面
Task T051: 建立 IProductItemRepository 介面
Task T052: 建立 IAttachmentRepository 介面
Task T053: 建立 ISignatureRecordRepository 介面
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational (CRITICAL - 阻擋所有故事)
3. 完成 Phase 3: User Story 1 (線下收購單建立)
4. 完成 Phase 4: User Story 2 (線下寄賣單建立)
5. **STOP and VALIDATE**: 獨立測試 User Story 1 與 User Story 2
6. 部署/展示 MVP

### Incremental Delivery

1. 完成 Setup + Foundational → 基礎就緒
2. 新增 User Story 1 → 獨立測試 → 部署/展示 (MVP!)
3. 新增 User Story 2 → 獨立測試 → 部署/展示
4. 新增 User Story 3 → 獨立測試 → 部署/展示
5. 新增 User Story 5 → 獨立測試 → 部署/展示
6. 新增 User Story 4 → 獨立測試 → 部署/展示
7. 每個故事新增價值且不破壞先前故事

### Parallel Team Strategy

若有多位開發者:

1. 團隊一起完成 Setup + Foundational
2. Foundational 完成後:
   - 開發者 A: User Story 1 (線下收購單)
   - 開發者 B: User Story 2 (線下寄賣單)
   - 開發者 C: User Story 3 (客戶搜尋)
3. 故事獨立完成並整合

---

## Summary

- **Total Tasks**: 140 個任務
- **Task Distribution**:
  - Setup: 9 tasks
  - Foundational: 20 tasks
  - User Story 1 (P1): 45 tasks
  - User Story 2 (P1): 8 tasks
  - User Story 3 (P2): 4 tasks
  - User Story 5 (P2): 21 tasks
  - User Story 4 (P3): 18 tasks
  - Polish: 15 tasks
- **Parallel Opportunities**: 約 60+ 任務可平行執行
- **Independent Test Criteria**: 每個使用者故事均有明確的獨立測試標準
- **MVP Scope**: User Story 1 + User Story 2 (線下收購單與寄賣單建立)

---

## Notes

- [P] 任務 = 不同檔案,無依賴關係,可平行執行
- [Story] 標籤將任務映射到特定使用者故事以便追蹤
- 每個使用者故事應可獨立完成與測試
- 在實作前驗證測試失敗
- 每個任務或邏輯群組完成後提交
- 在任何 Checkpoint 停止以獨立驗證故事
- 避免: 模糊任務、相同檔案衝突、破壞獨立性的跨故事依賴
