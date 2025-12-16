# API Contracts: 服務單管理

**Date**: 2025-12-14  
**Feature**: 服務單管理（寄賣單與收購單）  
**Version**: 1.0.0  
**Base URL**: `/api`

本文件定義服務單管理模組的前後端 API 契約，遵循 OpenAPI 3.0 規範與專案憲章原則 VII（Backend API Contract Compliance）。

---

## 通用規範

### 認證
所有 API 端點（除 `/api/auth/login` 外）必須在 HTTP Header 中包含 JWT Token：
```http
Authorization: Bearer {jwt_token}
```

### 統一回應格式
所有 API 回應遵循 `ApiResponse<T>` 格式：

```typescript
interface ApiResponse<T = any> {
  /** 操作是否成功 */
  success: boolean
  /** 業務邏輯代碼 */
  code: string
  /** 繁體中文訊息 */
  message: string
  /** 回應資料（可為 null） */
  data: T | null
  /** 回應時間戳記（ISO 8601, UTC） */
  timestamp: string
  /** 分散式追蹤 ID */
  traceId: string
}
```

### 分頁回應格式
```typescript
interface PagedResponse<T> {
  /** 資料項目列表 */
  items: T[]
  /** 當前頁碼（從 1 開始） */
  pageNumber: number
  /** 每頁筆數 */
  pageSize: number
  /** 總筆數 */
  totalRecords: number
  /** 總頁數 */
  totalPages: number
}
```

### 業務錯誤代碼
| 代碼 | 說明 | HTTP 狀態碼 |
|------|------|------------|
| `SUCCESS` | 操作成功 | 200 |
| `VALIDATION_ERROR` | 驗證錯誤 | 400 |
| `UNAUTHORIZED` | 未授權 | 401 |
| `FORBIDDEN` | 無權限 | 403 |
| `NOT_FOUND` | 資源不存在 | 404 |
| `CONCURRENT_UPDATE_CONFLICT` | 並發更新衝突 | 409 |
| `INTERNAL_SERVER_ERROR` | 伺服器錯誤 | 500 |

---

## API 端點總覽

### 服務單相關
| 方法 | 端點 | 說明 | 權限 |
|------|------|------|------|
| GET | `/service-orders` | 查詢服務單列表 | `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read` |
| GET | `/service-orders/{id}` | 查詢單一服務單 | `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read` |
| POST | `/service-orders` | 建立服務單 | `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create` |
| PUT | `/service-orders/{id}` | 更新服務單 | `serviceOrder.consignment.update` 或 `serviceOrder.buyback.update` |
| PATCH | `/service-orders/{id}/status` | 更新服務單狀態 | `serviceOrder.consignment.update` 或 `serviceOrder.buyback.update` |
| DELETE | `/service-orders/{id}` | 刪除服務單 | `serviceOrder.consignment.delete` 或 `serviceOrder.buyback.delete` |
| GET | `/service-orders/{id}/history` | 查詢修改歷史 | `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read` |

### 客戶相關
| 方法 | 端點 | 說明 | 權限 |
|------|------|------|------|
| GET | `/customers/search` | 搜尋客戶 | `customer.read` |
| POST | `/customers` | 新增客戶 | `customer.create` |
| GET | `/customers/{id}` | 查詢客戶詳細資訊 | `customer.read` |

### 附件相關
| 方法 | 端點 | 說明 | 權限 |
|------|------|------|------|
| POST | `/service-orders/{id}/attachments` | 上傳附件 | `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create` |
| GET | `/service-orders/{id}/attachments` | 取得附件列表 | `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read` |
| GET | `/attachments/{id}/download` | 下載附件 | `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read` |
| POST | `/attachments/{id}/view-log` | 記錄附件查看日誌 | `serviceOrder.attachment.viewSensitive` |

### 簽名相關
| 方法 | 端點 | 說明 | 權限 |
|------|------|------|------|
| POST | `/service-orders/{id}/signatures/merge-preview` | 合併簽名與預覽文件 | `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create` |
| POST | `/service-orders/{id}/signatures/offline` | 儲存線下簽名 | `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create` |
| POST | `/service-orders/{id}/signatures/online` | 發送線上簽名邀請 | `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create` |
| POST | `/service-orders/{id}/signatures/resend` | 重新發送簽名邀請 | `serviceOrder.consignment.update` 或 `serviceOrder.buyback.update` |
| GET | `/service-orders/{id}/signatures` | 取得簽名記錄 | `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read` |

### OCR 相關
| 方法 | 端點 | 說明 | 權限 |
|------|------|------|------|
| POST | `/ocr/id-card` | 辨識身分證 | `customer.create` |

---

## 詳細 API 規格

### 1. 查詢服務單列表

**端點**: `GET /service-orders`

**權限**: `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read`

**查詢參數**:
```typescript
interface ServiceOrderListParams {
  /** 頁碼（從 1 開始） */
  pageNumber: number
  /** 每頁筆數（1-100） */
  pageSize: number
  /** 服務單類型（可選） */
  orderType?: "consignment" | "buyback"
  /** 客戶名稱（模糊搜尋，可選） */
  customerName?: string
  /** 起始日期（ISO 8601，可選） */
  startDate?: string
  /** 結束日期（ISO 8601，可選） */
  endDate?: string
  /** 服務單狀態（可選） */
  status?: "pending" | "completed" | "terminated"
}
```

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": {
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "orderNumber": "CS20251214001",
        "orderType": "consignment",
        "orderSource": "offline",
        "customerId": "660e8400-e29b-41d4-a716-446655440001",
        "customerName": "王小明",
        "brandName": "CHANEL",
        "style": "Classic Flap",
        "internalCode": "ABC123",
        "quantity": 1,
        "amount": 50000.00,
        "status": "pending",
        "createdAt": "2025-12-14T02:30:00Z",
        "createdBy": "user123",
        "version": 0
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalRecords": 150,
    "totalPages": 8
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400):
```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "pageSize 必須介於 1 到 100 之間",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 2. 查詢單一服務單

**端點**: `GET /service-orders/{id}`

**權限**: `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read`

**路徑參數**:
- `id` (UUID): 服務單 ID

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "orderNumber": "CS20251214001",
    "orderType": "consignment",
    "orderSource": "offline",
    "customerId": "660e8400-e29b-41d4-a716-446655440001",
    "customer": {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "name": "王小明",
      "phone": "0912345678",
      "email": "wang@example.com",
      "idCardNumber": "A123456789"
    },
    "productItems": [
      {
        "sequence": 1,
        "brandName": "CHANEL",
        "style": "Classic Flap",
        "internalCode": "ABC123",
        "accessories": ["box", "dustBag", "card"],
        "defects": ["cornerWear"]
      },
      {
        "sequence": 2,
        "brandName": "LV",
        "style": "Neverfull MM",
        "accessories": ["dustBag"],
        "defects": []
      }
    ],
    "totalAmount": 50000.00,
    "consignmentStartDate": "2025-12-14",
    "consignmentEndDate": "2026-03-14",
    "renewalOption": "auto_retrieve",
    "status": "pending",
    "createdAt": "2025-12-14T02:30:00Z",
    "createdBy": "user123",
    "updatedAt": null,
    "updatedBy": null,
    "version": 0
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (404):
```json
{
  "success": false,
  "code": "NOT_FOUND",
  "message": "服務單不存在",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 3. 建立服務單

**端點**: `POST /service-orders`

**權限**: `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create`

**重要**: 在呼叫此 API 前，前端必須確保已透過 `POST /service-orders/{id}/attachments` 上傳身分證明文件。後端會驗證該服務單是否存在類型為 `ID_CARD` 的附件，若未上傳則返回驗證錯誤。

**建議流程**:
1. 呼叫 `POST /service-orders` 建立服務單（取得服務單 ID）
2. 立即呼叫 `POST /service-orders/{id}/attachments` 上傳身分證明文件
3. 完成其他附件上傳與簽名流程

**請求 Body**:
```typescript
interface ProductItem {
  /** 品牌名稱 */
  brandName: string
  /** 款式 */
  style: string
  /** 內碼（可選） */
  internalCode?: string
  /** 商品序號(1-4) */
  sequence: number
  /** 商品配件（僅寄賣單） */
  accessories?: string[]
  /** 商品瑕疵處（僅寄賣單） */
  defects?: string[]
}

interface CreateServiceOrderRequest {
  /** 服務單類型 */
  orderType: "consignment" | "buyback"
  /** 服務單來源 */
  orderSource: "online" | "offline"
  /** 客戶 ID */
  customerId: string
  /** 商品項目列表(1-4件) */
  productItems: ProductItem[]
  /** 總金額 */
  totalAmount: number
  /** 寄賣起始日期（僅寄賣單，ISO 8601） */
  consignmentStartDate?: string
  /** 寄賣結束日期（僅寄賣單，ISO 8601） */
  consignmentEndDate?: string
  /** 續約設定（僅寄賣單） */
  renewalOption?: "auto_retrieve" | "auto_discount_10" | "discuss_later"
}
```

**請求範例**:
```json
{
  "orderType": "consignment",
  "orderSource": "offline",
  "customerId": "660e8400-e29b-41d4-a716-446655440001",
  "productItems": [
    {
      "sequence": 1,
      "brandName": "CHANEL",
      "style": "Classic Flap",
      "internalCode": "ABC123",
      "accessories": ["box", "dustBag", "card"],
      "defects": ["cornerWear"]
    },
    {
      "sequence": 2,
      "brandName": "LV",
      "style": "Neverfull MM",
      "accessories": ["dustBag"],
      "defects": []
    }
  ],
  "totalAmount": 50000.00,
  "consignmentStartDate": "2025-12-14",
  "consignmentEndDate": "2026-03-14",
  "renewalOption": "auto_retrieve"
}
```

**成功回應** (201):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "服務單建立成功",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "orderNumber": "CS20251214001",
    "orderType": "consignment",
    "orderSource": "offline",
    "customerId": "660e8400-e29b-41d4-a716-446655440001",
    "productItems": [
      {
        "sequence": 1,
        "brandName": "CHANEL",
        "style": "Classic Flap",
        "internalCode": "ABC123",
        "accessories": ["box", "dustBag", "card"],
        "defects": ["cornerWear"]
      },
      {
        "sequence": 2,
        "brandName": "LV",
        "style": "Neverfull MM",
        "accessories": ["dustBag"],
        "defects": []
      }
    ],
    "totalAmount": 50000.00,
    "consignmentStartDate": "2025-12-14",
    "consignmentEndDate": "2026-03-14",
    "renewalOption": "auto_retrieve",
    "status": "pending",
    "createdAt": "2025-12-14T02:30:00Z",
    "createdBy": "user123",
    "updatedAt": null,
    "updatedBy": null,
    "version": 0
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400):
```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "寄賣結束日期必須晚於起始日期",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400 - 未上傳身分證明):
```json
{
  "success": false,
  "code": "MISSING_ID_CARD_ATTACHMENT",
  "message": "身分證明文件為必要附件，請上傳或拍攝身分證照片",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 4. 更新服務單

**端點**: `PUT /service-orders/{id}`

**權限**: `serviceOrder.consignment.update` 或 `serviceOrder.buyback.update`

**路徑參數**:
- `id` (UUID): 服務單 ID

**請求 Body**:
```typescript
interface UpdateServiceOrderRequest {
  /** 商品項目列表(1-4件) */
  productItems: ProductItem[]
  /** 總金額 */
  totalAmount: number
  /** 寄賣起始日期（僅寄賣單，ISO 8601） */
  consignmentStartDate?: string
  /** 寄賣結束日期（僅寄賣單，ISO 8601） */
  consignmentEndDate?: string
  /** 續約設定（僅寄賣單） */
  renewalOption?: "auto_retrieve" | "auto_discount_10" | "discuss_later"
  /** 版本號（樂觀鎖） */
  version: number
}
```

**請求範例**:
```json
{
  "productItems": [
    {
      "sequence": 1,
      "brandName": "CHANEL",
      "style": "Classic Flap",
      "internalCode": "ABC123",
      "accessories": ["box", "dustBag", "card"],
      "defects": ["cornerWear"]
    },
    {
      "sequence": 2,
      "brandName": "LV",
      "style": "Neverfull MM",
      "accessories": ["dustBag"],
      "defects": []
    }
  ],
  "totalAmount": 55000.00,
  "consignmentStartDate": "2025-12-14",
  "consignmentEndDate": "2026-03-14",
  "renewalOption": "auto_retrieve",
  "version": 0
}
```

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "服務單更新成功",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "orderNumber": "CS20251214001",
    "totalAmount": 55000.00,
    "updatedAt": "2025-12-14T03:00:00Z",
    "updatedBy": "user123",
    "version": 1
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (409):
```json
{
  "success": false,
  "code": "CONCURRENT_UPDATE_CONFLICT",
  "message": "資料已被其他使用者修改，請重新載入",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 5. 更新服務單狀態

**端點**: `PATCH /service-orders/{id}/status`

**權限**: `serviceOrder.consignment.update` 或 `serviceOrder.buyback.update`

**路徑參數**:
- `id` (UUID): 服務單 ID

**請求 Body**:
```typescript
interface UpdateStatusRequest {
  /** 目標狀態 */
  status: "pending" | "completed" | "terminated"
  /** 版本號（樂觀鎖） */
  version: number
}
```

**請求範例**:
```json
{
  "status": "completed",
  "version": 0
}
```

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "狀態更新成功",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "status": "completed",
    "updatedAt": "2025-12-14T03:00:00Z",
    "updatedBy": "user123",
    "version": 1
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400):
```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "已終止的服務單無法變更狀態",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 6. 刪除服務單

**端點**: `DELETE /service-orders/{id}`

**權限**: `serviceOrder.consignment.delete` 或 `serviceOrder.buyback.delete`

**路徑參數**:
- `id` (UUID): 服務單 ID

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "服務單刪除成功",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (404):
```json
{
  "success": false,
  "code": "NOT_FOUND",
  "message": "服務單不存在",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 7. 查詢修改歷史

**端點**: `GET /service-orders/{id}/history`

**權限**: `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read`

**路徑參數**:
- `id` (UUID): 服務單 ID

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440002",
      "serviceOrderId": "550e8400-e29b-41d4-a716-446655440000",
      "fieldName": "totalAmount",
      "oldValue": "50000.00",
      "newValue": "55000.00",
      "modifiedAt": "2025-12-14T03:00:00Z",
      "modifiedBy": "user123",
      "modifiedByName": "張三"
    }
  ],
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 8. 搜尋客戶

**端點**: `GET /customers/search`

**權限**: `customer.read`

**說明**: 搜尋客戶資料,支援多種關鍵字搜尋。搜尋邏輯優先順序:身分證字號(精確比對) > 電話(精確比對) > 姓名(模糊搜尋) > Email(模糊搜尋)。若關鍵字符合身分證字號格式,則僅以身分證字號搜尋。

**支援的身分證字號格式**:
1. 台灣國民：1 英文字母 + 9 數字（例如：A123456789）
2. 外籍人士（無統一證號）：西元出生年月日 8 位數 + 英文姓名第一個字前兩個字母大寫（例如：19900115JO，總長度 10 位）

**查詢參數**:
```typescript
interface CustomerSearchParams {
  /** 搜尋關鍵字(姓名、電話、Email、身分證字號) */
  keyword: string
}
```

**請求範例**:
```
GET /customers/search?keyword=A123456789   // 台灣身分證字號搜尋(精確比對)
GET /customers/search?keyword=19900115JO   // 外籍人士身分證字號搜尋(精確比對)
GET /customers/search?keyword=0912345678   // 電話搜尋(精確比對)
GET /customers/search?keyword=王小明      // 姓名搜尋(模糊比對)
GET /customers/search?keyword=wang        // Email 搜尋(模糊比對)
```

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": [
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "name": "王小明",
      "phone": "0912345678",
      "email": "wang@example.com",
      "idCardNumber": "A123456789"
    }
  ],
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**成功回應** (200 - 無結果):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": [],
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 9. 新增客戶

**端點**: `POST /customers`

**權限**: `customer.create`

**請求 Body**:
```typescript
interface CreateCustomerRequest {
  /** 客戶姓名 */
  name: string
  /** 電話號碼 */
  phone: string
  /** Email（可選） */
  email?: string
  /** 身分證字號（支援台灣身分證與外籍人士格式） */
  idCardNumber: string
}
```

**身分證字號格式**:
1. 台灣國民：1 英文字母 + 9 數字（例如：A123456789）
2. 外籍人士（無統一證號）：西元出生年月日 8 位數 + 英文姓名第一個字前兩個字母大寫（例如：19900115JO，總長度 10 位）

**請求範例（台灣國民）**:
```json
{
  "name": "王小明",
  "phone": "0912345678",
  "email": "wang@example.com",
  "idCardNumber": "A123456789"
}
```

**請求範例（外籍人士）**:
```json
{
  "name": "John Doe",
  "phone": "0987654321",
  "email": "john@example.com",
  "idCardNumber": "19900115JO"
}
```

**成功回應** (201):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "客戶新增成功",
  "data": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "name": "王小明",
    "phone": "0912345678",
    "email": "wang@example.com",
    "idCardNumber": "A123456789",
    "createdAt": "2025-12-14T02:30:00Z",
    "updatedAt": null
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400):
```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "身分證字號已存在",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 10. 上傳附件

**端點**: `POST /service-orders/{id}/attachments`

**權限**: `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create`

**說明**: 上傳服務單相關附件。**身分證明文件（fileType = ID_CARD）為必要附件**，每筆服務單至少需要一個身分證明附件。建議在建立服務單後立即上傳身分證明文件。

**路徑參數**:
- `id` (UUID): 服務單 ID

**請求 Body** (multipart/form-data):
```typescript
interface UploadAttachmentRequest {
  /** 檔案（支援上傳或拍照） */
  file: File
  /** 檔案類型 */
  fileType: "ID_CARD" | "CONTRACT" | "OTHER"
}
```

**檔案限制**:
- 身分證明文件僅接受圖片格式：image/jpeg, image/png
- 檔案大小限制：最大 10MB
- 每筆服務單必須至少有一個 fileType = "ID_CARD" 的附件

**成功回應** (201):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "附件上傳成功",
  "data": {
    "id": "880e8400-e29b-41d4-a716-446655440003",
    "serviceOrderId": "550e8400-e29b-41d4-a716-446655440000",
    "fileName": "id_card.jpg",
    "fileType": "ID_CARD",
    "fileUrl": "https://storage.example.com/attachments/id_card_12345.jpg",
    "fileSize": 2048576,
    "uploadedAt": "2025-12-14T02:30:00Z",
    "uploadedBy": "user123"
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400 - 檔案大小超過限制):
```json
{
  "success": false,
  "code": "FILE_SIZE_EXCEEDED",
  "message": "檔案大小超過 10MB 限制，請壓縮後重新上傳",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400 - 檔案格式不支援):
```json
{
  "success": false,
  "code": "UNSUPPORTED_FILE_FORMAT",
  "message": "僅支援 JPG 或 PNG 格式的圖片檔案",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 11. 取得附件列表

**端點**: `GET /service-orders/{id}/attachments`

**權限**: `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read`

**路徑參數**:
- `id` (UUID): 服務單 ID

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": [
    {
      "id": "880e8400-e29b-41d4-a716-446655440003",
      "serviceOrderId": "550e8400-e29b-41d4-a716-446655440000",
      "fileName": "id_card.jpg",
      "fileType": "ID_CARD",
      "fileUrl": "https://storage.example.com/attachments/id_card_12345.jpg",
      "fileSize": 2048576,
      "uploadedAt": "2025-12-14T02:30:00Z",
      "uploadedBy": "user123"
    }
  ],
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 12. 下載附件

**端點**: `GET /attachments/{id}/download`

**權限**: `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read`

**路徑參數**:
- `id` (UUID): 附件 ID

**成功回應** (200):
返回檔案二進位資料，HTTP Header 包含：
```http
Content-Type: image/jpeg
Content-Disposition: attachment; filename="id_card.jpg"
Content-Length: 2048576
```

---

### 13. 儲存線下簽名

**端點**: `POST /service-orders/{id}/signatures/offline`

**權限**: `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create`

**路徑參數**:
- `id` (UUID): 服務單 ID

**請求 Body**:
```typescript
interface SaveOfflineSignatureRequest {
  /** 簽名文件類型 */
  documentType: "BUYBACK_CONTRACT" | "TRADE_APPLICATION" | "CONSIGNMENT_CONTRACT"
  /** 簽名資料（Base64 PNG） */
  signatureData: string
  /** 簽名者姓名 */
  signerName: string
}
```

**請求範例**:
```json
{
  "documentType": "CONSIGNMENT_CONTRACT",
  "signatureData": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "signerName": "王小明"
}
```

**成功回應** (201):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "簽名儲存成功",
  "data": {
    "id": "990e8400-e29b-41d4-a716-446655440004",
    "serviceOrderId": "550e8400-e29b-41d4-a716-446655440000",
    "documentType": "CONSIGNMENT_CONTRACT",
    "signatureMethod": "OFFLINE",
    "signerName": "王小明",
    "signedAt": "2025-12-14T02:30:00Z"
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 14. 發送線上簽名邀請

**端點**: `POST /service-orders/{id}/signatures/online`

**權限**: `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create`

**路徑參數**:
- `id` (UUID): 服務單 ID

**請求 Body**:
```typescript
interface SendOnlineSignatureRequest {
  /** 簽名文件類型 */
  documentType: "BUYBACK_CONTRACT" | "TRADE_APPLICATION" | "CONSIGNMENT_CONTRACT"
  /** 簽名者姓名 */
  signerName: string
  /** 簽名者 Email */
  signerEmail: string
}
```

**請求範例**:
```json
{
  "documentType": "CONSIGNMENT_CONTRACT",
  "signerName": "王小明",
  "signerEmail": "wang@example.com"
}
```

**成功回應** (201):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "簽名邀請已發送",
  "data": {
    "id": "990e8400-e29b-41d4-a716-446655440004",
    "serviceOrderId": "550e8400-e29b-41d4-a716-446655440000",
    "documentType": "CONSIGNMENT_CONTRACT",
    "signatureMethod": "ONLINE",
    "dropboxSignRequestId": "fa5c8a0b6f07",
    "signerName": "王小明",
    "signedAt": null
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 15. 重新發送簽名邀請

**端點**: `POST /service-orders/{id}/signatures/resend`

**權限**: `serviceOrder.consignment.update` 或 `serviceOrder.buyback.update`

**路徑參數**:
- `id` (UUID): 服務單 ID

**請求 Body**:
```typescript
interface ResendSignatureRequest {
  /** 簽名記錄 ID */
  signatureRecordId: string
}
```

**請求範例**:
```json
{
  "signatureRecordId": "990e8400-e29b-41d4-a716-446655440004"
}
```

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "簽名邀請已重新發送",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 16. 合併簽名與預覽文件

**端點**: `POST /service-orders/{id}/signatures/merge-preview`

**權限**: `serviceOrder.consignment.create` 或 `serviceOrder.buyback.create`

**說明**: 將客戶的觸控簽名（Base64）與 PDF 合約文件合併，產生預覽文件供店員與客戶確認。此 API 僅產生預覽，不會正式儲存簽名記錄。確認無誤後，前端應呼叫 `POST /service-orders/{id}/signatures/offline` 正式儲存簽名。

**路徑參數**:
- `id` (UUID): 服務單 ID

**請求 Body**:
```typescript
interface MergeSignaturePreviewRequest {
  /** 簽名文件類型 */
  documentType: "BUYBACK_CONTRACT" | "TRADE_APPLICATION" | "CONSIGNMENT_CONTRACT"
  /** 簽名資料（Base64 PNG） */
  signatureBase64: string
}
```

**請求範例**:
```json
{
  "documentType": "CONSIGNMENT_CONTRACT",
  "signatureBase64": "iVBORw0KGgoAAAANSUhEUgAA..."
}
```

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "預覽文件產生成功",
  "data": {
    "previewUrl": "https://storage.example.com/previews/contract_preview_12345.pdf",
    "expiresAt": "2025-12-14T12:30:00Z"
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400):
```json
{
  "success": false,
  "code": "SIGNATURE_MERGE_FAILED",
  "message": "簽名合併失敗，請確認簽名資料格式正確",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**前端整合流程**:
1. 客戶在簽名板上簽名完成
2. 前端匯出 Base64 簽名資料
3. 呼叫 `POST /service-orders/{id}/signatures/merge-preview` 取得預覽 URL
4. 顯示預覽 PDF 供店員與客戶確認
5. 確認無誤後呼叫 `POST /service-orders/{id}/signatures/offline` 正式儲存

---

### 17. 記錄附件查看日誌

**端點**: `POST /attachments/{id}/view-log`

**權限**: `serviceOrder.attachment.viewSensitive`

**說明**: 記錄敏感附件（如身分證明文件）的查看歷史，用於個資稽核。每次使用者查看或下載身分證明文件時，前端必須呼叫此 API 記錄日誌。

**路徑參數**:
- `id` (UUID): 附件 ID

**請求 Body**:
```typescript
interface LogAttachmentViewRequest {
  /** 操作類型 */
  action: "VIEW" | "DOWNLOAD"
}
```

**請求範例**:
```json
{
  "action": "VIEW"
}
```

**成功回應** (201):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查看記錄已儲存",
  "data": {
    "logId": "aa0e8400-e29b-41d4-a716-446655440005",
    "attachmentId": "880e8400-e29b-41d4-a716-446655440003",
    "userId": "user123",
    "userName": "張三",
    "action": "VIEW",
    "viewedAt": "2025-12-14T10:30:00Z"
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**前端整合流程**:
1. 使用者點擊「查看」或「下載」身分證明文件
2. 前端先呼叫 `POST /attachments/{id}/view-log` 記錄日誌
3. 日誌記錄成功後才顯示圖片或觸發下載

---

### 18. 取得簽名記錄

**端點**: `GET /service-orders/{id}/signatures`

**權限**: `serviceOrder.consignment.read` 或 `serviceOrder.buyback.read`

**路徑參數**:
- `id` (UUID): 服務單 ID

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": [
    {
      "id": "990e8400-e29b-41d4-a716-446655440004",
      "serviceOrderId": "550e8400-e29b-41d4-a716-446655440000",
      "documentType": "CONSIGNMENT_CONTRACT",
      "signatureMethod": "OFFLINE",
      "signerName": "王小明",
      "signedAt": "2025-12-14T02:30:00Z"
    }
  ],
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

---

### 17. 辨識身分證

**端點**: `POST /ocr/id-card`

**權限**: `customer.create`

**說明**: 使用 AI OCR 技術辨識身分證圖片,提取姓名與身分證字號。前端應在辨識成功後,自動使用身分證字號呼叫客戶搜尋 API (`GET /customers/search?keyword={idCardNumber}`),實現自動搜尋既有客戶的功能。

**請求 Body** (multipart/form-data):
```typescript
interface OCRIDCardRequest {
  /** 身分證圖片檔案 */
  file: File
}
```

**成功回應** (200):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "辨識成功",
  "data": {
    "name": "王小明",
    "idCardNumber": "A123456789",
    "confidence": 0.92
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**成功回應範例（外籍人士）**:
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "辨識成功",
  "data": {
    "name": "John Doe",
    "idCardNumber": "19900115JO",
    "confidence": 0.89
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**說明**:
- 對於台灣身分證，辨識結果為標準格式（1 英文字母 + 9 數字）
- 對於外籍人士居留證，若無統一證號欄項，辨識結果為西元出生年月日 8 位數 + 英文姓名前兩字母大寫（總長度 10 位）
- confidence 為辨識信心度（0-1），建議低於 0.8 時提示用戶確認

**錯誤回應** (400 - 辨識失敗):
```json
{
  "success": false,
  "code": "OCR_RECOGNITION_FAILED",
  "message": "辨識失敗，請重新拍攝或手動輸入客戶資料",
  "data": null,
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**錯誤回應** (400 - 身分證字號格式錯誤):
```json
{
  "success": false,
  "code": "INVALID_ID_CARD_FORMAT",
  "message": "辨識的身分證字號格式不正確，請確認照片清晰度或手動輸入",
  "data": {
    "recognizedText": "A12345678X"
  },
  "timestamp": "2025-12-14T10:30:00Z",
  "traceId": "abc123xyz"
}
```

**前端整合流程**:
1. 用戶上傳/拍攝身分證照片
2. 點擊 ⭐ 星星符號觸發 OCR 辨識
3. 呼叫 `POST /ocr/id-card` 取得辨識結果
4. 若辨識成功,自動呼叫 `GET /customers/search?keyword={idCardNumber}` 搜尋客戶
5. 若找到客戶,自動選擇並填入表單;若找不到,填入新增客戶表單

---

## Webhook（Dropbox Sign 回呼）

**端點**: `POST /webhooks/dropbox-sign`

**用途**: 接收 Dropbox Sign 的簽名狀態更新通知

**請求 Body** (由 Dropbox Sign 發送):
```json
{
  "event": {
    "event_type": "signature_request_signed",
    "event_time": "2025-12-14T10:30:00Z"
  },
  "signature_request": {
    "signature_request_id": "fa5c8a0b6f07",
    "is_complete": true
  }
}
```

**回應** (200):
```json
{
  "success": true
}
```

---

## 權限定義

### 寄賣單權限
- `serviceOrder.consignment.read`: 查詢與匯出寄賣單
- `serviceOrder.consignment.create`: 建立寄賣單
- `serviceOrder.consignment.update`: 修改寄賣單
- `serviceOrder.consignment.delete`: 刪除寄賣單

### 收購單權限
- `serviceOrder.buyback.read`: 查詢與匯出收購單
- `serviceOrder.buyback.create`: 建立收購單
- `serviceOrder.buyback.update`: 修改收購單
- `serviceOrder.buyback.delete`: 刪除收購單

### 客戶權限
- `customer.read`: 查詢客戶資料
- `customer.create`: 新增客戶資料

---

## 錯誤處理範例（前端）

```typescript
async function fetchServiceOrders(params: ServiceOrderListParams) {
  try {
    const response = await request<PagedResponse<ServiceOrder>>({
      url: "/service-orders",
      method: "GET",
      params
    })
    
    if (response.success) {
      return response.data
    } else {
      // 處理業務錯誤
      ElMessage.error(response.message)
      return null
    }
  } catch (error) {
    // 處理網路錯誤或系統錯誤
    ElMessage.error("系統錯誤，請稍後再試")
    return null
  }
}
```

---

## 版本歷史

| 版本 | 日期 | 變更內容 |
|------|------|---------|
| 1.0.0 | 2025-12-14 | 初始版本，定義服務單管理所有 API 端點 |

---

## 總結

本 API 契約定義了服務單管理模組的所有前後端互動介面：

✅ **19 個 API 端點**：涵蓋服務單、客戶、附件、簽名、OCR 功能（包含簽名合併預覽與附件查看日誌）  
✅ **統一回應格式**：遵循 `ApiResponse<T>` 規範  
✅ **完整錯誤處理**：定義所有業務錯誤代碼  
✅ **權限控制**：明確定義各端點所需權限  
✅ **分頁支援**：標準分頁參數與回應格式  
✅ **並發控制**：使用樂觀鎖（version 欄位）  
✅ **憲章合規**：遵循 Backend API Contract Compliance 原則
