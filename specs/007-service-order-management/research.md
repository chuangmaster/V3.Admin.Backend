# Research: 服務單管理模組技術決策

**Feature**: 服務單管理模組  
**Branch**: `007-service-order-management`  
**Date**: 2025-12-16  
**研究範圍**: 解決 Technical Context 中識別的 8 個關鍵技術決策問題

---

## 研究項目總覽

本研究針對服務單管理模組的核心技術挑戰進行深入分析,涵蓋以下領域:

1. **OCR 服務整合** - Azure Vision + Google Gemini 雙重辨識機制
2. **PDF 處理** - PDFsharp 繁體中文支援與簽名合併
3. **Webhook 安全** - Dropbox Sign API Webhook 驗證機制
4. **序號生成** - PostgreSQL Sequence 每日重置策略
5. **雲端儲存** - Azure Blob Storage SAS Token 管理
6. **資料庫設計** - 商品項目與服務單關聯設計
7. **變更追蹤** - 修改歷史記錄機制
8. **資料模型** - 簽名記錄統一設計

---

## 1. Azure Vision + Google Gemini OCR 整合模式

### Decision (決策)

採用「**兩階段 OCR 管線**」架構:
- **階段一**: Azure Computer Vision 4.0 進行文字擷取
- **階段二**: Google Gemini Pro Vision 進行結構化解析與驗證
- **降級策略**: Azure 失敗時直接使用 Gemini Vision,反之使用 Azure + 文字後處理
- **信心度評分**: 加權平均 (Azure 40% + Gemini 60%)

### Rationale (理由)

1. **互補優勢**: Azure Vision 對繁體中文 OCR 準確度高,Gemini 擅長語義理解與格式驗證
2. **成本效益**: Azure 按頁計費便宜,Gemini 僅用於結構化解析
3. **容錯性**: 雙服務設計提供更高可用性
4. **台灣身分證特性**: 需要同時處理 OCR 與邏輯驗證(如身分證字號檢查碼)

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| 單一使用 Gemini Vision | 成本較高,但實作簡單 | 準確度可能不如專門的 OCR 服務 | 成本考量 |
| 使用 Azure Form Recognizer | 專為表單設計 | 需要訓練自定義模型,維護成本高 | 增加複雜度 |
| 開源 Tesseract + 後處理 | 免費 | 準確度不足,需要大量調校 | 品質要求 |

### Implementation Guidance (實作指引)

**整合流程**:
1. 店員上傳身分證照片至後端 API (暫存於記憶體，不立即存入 Blob)
2. 後端呼叫 Azure Vision 擷取文字 (支援繁體中文)
3. 將 OCR 文字與原始圖片 Base64 傳送至 Gemini 進行結構化解析
4. Gemini 驗證身分證字號檢查碼、解析姓名、出生日期等欄位
5. 綜合兩個服務的信心度評分 (Azure 40% + Gemini 60%)
6. 若信心度 < 0.8，顯示警告訊息提示店員確認
7. 店員確認辨識結果與客戶資訊後送出表單
8. 後端將身分證照片儲存至 Azure Blob Storage 並產生服務單記錄

**關鍵技術點**:
- 使用 `Azure.AI.Vision.ImageAnalysis` SDK
- 使用 `Google.Cloud.AIPlatform.V1` SDK
- 實作 Retry 機制處理 API 限流 (429 錯誤)
- 記錄 OCR 辨識日誌供後續分析

**參考程式碼**: 詳見研究報告完整版 (包含 C# 服務實作範例)

---

## 2. PDFsharp 繁體中文支援與簽章流程

### Decision (決策)

- 使用 **PDFsharp 6.x** (支援 .NET 10)
- 繁體中文字體採用 **系統字體** (Windows: 微軟正黑體 `msjh.ttc`, Linux: NotoSansCJK)
- **PDF 模板填充**: 建立空白 PDF 模板,使用 `XGraphics.DrawString` 將欄位值定位填入
- **多階段簽章流程**:
  1. 後端填充 PDF 模板→傳回前端預覽
  2. 前端 HTML SignaturePad 簽章→傳回 Base64 PNG
  3. 後端合併簽章到 PDF→傳回前端確認
  4. 確認後儲存至 Azure Blob Storage
- Base64 PNG 簽章透過 `XGraphics.DrawImage` 定位到絕對座標
- 使用 `XFont` 搭配 `XPdfFontOptions` 確保字體嵌入

### Rationale (理由)

1. **字體嵌入**: PDFsharp 需明確指定字體才能正確顯示繁體中文
2. **模板填充**: 可重複使用同一版型設計,即時填充不同服務單資料
3. **多階段流程**: 提供預覽與確認機制,避免錯誤資料被永久儲存
4. **圖片格式**: PNG 支援透明背景,適合簽章圖片
5. **座標系統**: PDFsharp 使用左上角為原點,單位為點 (1 點 = 1/72 英寸)
6. **跨平台**: NotoSansCJK 確保在 Linux/Docker 環境也能正常運作

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| iTextSharp/iText7 | 功能強大 | 授權費用高,商業使用需付費 | 成本考量 |
| QuestPDF | 現代化 API,鏈式呼叫 | 較新但生態系不成熟 | 穩定性考量 |
| 圖片字體 | 實作簡單 | 檔案過大且無法搜尋 | 使用體驗差 |
| 前端 PDF.js 簽章 | 不需後端處理 | 無法控制 PDF 格式,安全性低 | 需後端驗證與儲存 |

### Implementation Guidance (實作指引)

**字體設定**:
- Windows: `C:\\Windows\\Fonts\\msjh.ttc` (微軟正黑體)
- Linux: `/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc`
- 使用 `CustomFontResolver` 載入字體檔

**多階段簽章流程**:

```csharp
// 階段 1: 填充 PDF 模板
public async Task<byte[]> FillPdfTemplateAsync(ServiceOrderDto order)
{
    // 載入空白模板 PDF
    using var templateStream = File.OpenRead("templates/buyback_contract_template.pdf");
    var document = PdfReader.Open(templateStream, PdfDocumentOpenMode.Modify);
    var page = document.Pages[0];
    var gfx = XGraphics.FromPdfPage(page);
    
    // 定位填入欄位值
    var font = new XFont("Microsoft JhengHei", 12);
    gfx.DrawString(order.CustomerName, font, XBrushes.Black, new XPoint(150, 100));
    gfx.DrawString(order.OrderNumber, font, XBrushes.Black, new XPoint(400, 100));
    gfx.DrawString(order.TotalAmount.ToString("C"), font, XBrushes.Black, new XPoint(150, 150));
    
    // 轉換為 byte[]
    using var ms = new MemoryStream();
    document.Save(ms);
    return ms.ToArray();
}

// 階段 3: 合併簽章到 PDF
public async Task<byte[]> MergeSignatureToPdfAsync(byte[] pdfBytes, string signatureBase64)
{
    // 載入已填充的 PDF
    using var pdfStream = new MemoryStream(pdfBytes);
    var document = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Modify);
    var page = document.Pages[0];
    var gfx = XGraphics.FromPdfPage(page);
    
    // 解析 Base64 簽章圖片
    var signatureBytes = Convert.FromBase64String(signatureBase64);
    using var signatureStream = new MemoryStream(signatureBytes);
    var signatureImage = XImage.FromStream(signatureStream);
    
    // 客戶簽章位置 (左下角)
    var signatureRect = new XRect(50, page.Height - 150, 150, 60);
    gfx.DrawImage(signatureImage, signatureRect);
    gfx.DrawRectangle(XPens.Gray, signatureRect); // 框線
    
    // 轉換為 byte[]
    using var ms = new MemoryStream();
    document.Save(ms);
    return ms.ToArray();
}
```

**API 端點設計**:
- `POST /api/service-orders/buyback/preview-pdf` - 填充模板,傳回 PDF 供預覽
- `POST /api/service-orders/buyback/merge-signature` - 合併簽章,傳回 PDF 供確認
- `POST /api/service-orders/buyback/confirm` - 確認後儲存至 Blob + 建立服務單記錄

**NuGet 套件**:
- `PDFsharp` 6.1.1+
- `System.Text.Encoding.CodePages` 8.0.0

---

## 3. Dropbox Sign API Webhook 驗證機制

### Decision (決策)

- 使用 **HMAC-SHA256** 驗證 Webhook 請求真實性
- 實作 **時間戳檢查** 防止重放攻擊 (容忍 5 分鐘時間差)
- 記錄 **Event Hash** 至資料庫防止重複處理
- 使用 ASP.NET Core **Middleware** 統一驗證

### Rationale (理由)

1. **安全性**: HMAC 確保請求來自 Dropbox Sign,未被中間人竄改
2. **防重放**: 時間戳與事件雜湊防止攻擊者重放舊請求
3. **效能**: Middleware 層級驗證,避免進入 Controller 邏輯
4. **可追溯**: 記錄所有 Webhook 事件便於審計

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| 僅使用 API Key | 實作簡單 | 容易洩漏,安全性不足 | 安全要求 |
| IP 白名單 | 簡單直接 | Dropbox Sign IP 可能變動,維護困難 | 可維護性 |
| JWT 驗證 | 標準化 | Dropbox Sign 未提供 JWT,需自行實作複雜度高 | 技術限制 |

### Implementation Guidance (實作指引)

**驗證流程**:
1. 讀取 Request Body
2. 取得 `X-HelloSign-Signature` 標頭
3. 使用 API Key 計算 HMAC-SHA256 簽名
4. 比對簽名是否一致
5. 驗證時間戳是否在容忍範圍內 (5 分鐘)
6. 檢查 Event Hash 是否已處理過
7. 處理事件並標記為已處理

**資料表設計**:
```sql
CREATE TABLE dropbox_sign_webhook_events (
    id SERIAL PRIMARY KEY,
    event_hash VARCHAR(64) UNIQUE NOT NULL,
    processed_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

**自動清理**: 建立排程任務清理 30 天前的事件記錄

---

## 4. PostgreSQL Sequence 每日重置策略

### Decision (決策)

- 使用 **複合主鍵** + **計算欄位**設計: `(date, sequence_number)`
- 建立自訂函數 `generate_daily_service_order_number()` 搭配觸發器
- 使用 `SELECT FOR UPDATE SKIP LOCKED` 確保並發安全
- 序號格式: `BS{YYYYMMDD}{001-999}` (收購單) 或 `CS{YYYYMMDD}{001-999}` (寄賣單)

### Rationale (理由)

1. **並發安全**: PostgreSQL Row-level Lock 確保同一天內序號唯一
2. **自動重置**: 觸發器自動檢測日期變更並重置序號
3. **查詢效率**: 複合索引 `(service_date, sequence_number)` 加速查詢
4. **可擴展性**: 支援每日最多 999 筆訂單,超過可調整

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| SEQUENCE + 定時任務重置 | 實作相對簡單 | 需要外部排程,複雜度高 | 依賴外部服務 |
| 應用層控制 | 邏輯集中 | 無法保證並發安全 | 安全性考量 |
| UUID | 全域唯一 | 不符合業務需求 (需要遞增編號) | 業務要求 |

### Implementation Guidance (實作指引)

**資料表設計**:
```sql
CREATE TABLE service_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_date DATE NOT NULL DEFAULT CURRENT_DATE,
    sequence_number INT NOT NULL,
    order_number VARCHAR(20) GENERATED ALWAYS AS (
        'BS' || TO_CHAR(service_date, 'YYYYMMDD') || LPAD(sequence_number::TEXT, 3, '0')
    ) STORED,
    -- 其他欄位...
    CONSTRAINT uk_service_orders_date_seq UNIQUE (service_date, sequence_number)
);
```

**序號生成函數**:
```sql
CREATE OR REPLACE FUNCTION generate_daily_service_order_number(p_service_date DATE)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_next_sequence INT;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('service_order_' || p_service_date::TEXT));
    
    SELECT COALESCE(MAX(sequence_number), 0) + 1
    INTO v_next_sequence
    FROM service_orders
    WHERE service_date = p_service_date
    FOR UPDATE SKIP LOCKED;
    
    IF v_next_sequence > 999 THEN
        RAISE EXCEPTION '當日服務單序號已達上限 (999)';
    END IF;
    
    RETURN v_next_sequence;
END;
$$;
```

**觸發器**:
```sql
CREATE TRIGGER trg_service_orders_sequence
BEFORE INSERT ON service_orders
FOR EACH ROW
EXECUTE FUNCTION trigger_generate_service_order_sequence();
```

---

## 5. Azure Blob Storage SAS Token 管理

### Decision (決策)

採用 **User Delegation SAS** 生成具有最小權限範圍的 SAS Token,有效期設為 **1 小時**,並實作 **Token 快取機制** (MemoryCache, TTL 55 分鐘) 以優化效能。

### Rationale (理由)

1. **安全性考量**:
   - User Delegation SAS 使用 Azure AD 憑證簽署,相比 Account SAS 更安全
   - 限定單一 Blob 的讀取權限,避免過度授權
   - 1 小時有效期符合規格要求,降低 Token 洩露風險
   
2. **可用性考量**:
   - 1 小時有效期足夠店員查看與下載附件
   - 實作 Token 產生 API 端點,過期後可重新請求
   - 支援多次下載同一附件(有效期內)

3. **效能考量**:
   - 快取同一檔案的 Token (使用 MemoryCache,TTL 55 分鐘)
   - 減少重複產生 SAS 的 API 呼叫

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| Account SAS | 實作簡單,不需 Azure AD | 使用 Storage Account Key,洩露風險高 | 安全性較低 |
| Stored Access Policy | 可集中管理與撤銷權限 | 需額外建立 Policy,複雜度高 | 單一檔案存取不需複雜策略 |
| 固定時效的公開 URL | 實作最簡單 | 無法控制存取權限,安全性最低 | 不符合個資保護需求 |
| 短時效 SAS (5-15 分鐘) | 安全性最高 | 使用體驗差,頻繁過期需重新取得 | 影響使用者體驗 |

### Implementation Guidance (實作指引)

**SAS 權限設定**:
```csharp
var sasBuilder = new BlobSasBuilder
{
    BlobContainerName = ContainerName,
    BlobName = blobName,
    Resource = "b", // b = Blob
    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // 提前 5 分鐘避免時鐘偏移
    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
};
sasBuilder.SetPermissions(BlobSasPermissions.Read); // 僅讀取權限
```

**快取策略**:
- Cache Key: `sas_{blobName}`
- TTL: 55 分鐘 (略短於 SAS 有效期,避免快取過期 Token)
- 檢查邏輯: 產生 SAS 前先檢查快取

**個資稽核**:
- 每次產生 SAS Token 時記錄至 `attachment_view_logs` 表
- 記錄查看者、查看時間、檔案名稱

---

## 6. 商品項目與服務單的資料庫設計

### Decision (決策)

採用 **1-N 關聯表設計**,寄賣單與收購單 **共用 product_items 表**,配件與瑕疵使用 **JSONB 欄位儲存多選資料**。

### Rationale (理由)

1. **資料正規化**:
   - 每個服務單可包含 1-4 件商品,典型的一對多關係
   - 共用商品表可避免重複欄位定義,維護成本低
   
2. **查詢效能**:
   - 使用 `service_order_id` 外鍵索引,JOIN 查詢效率高
   - JSONB 欄位支援 GIN 索引,可高效查詢特定配件或瑕疵
   
3. **彈性擴展**:
   - JSONB 欄位可彈性新增選項,無需 ALTER TABLE
   - 未來若需要更多商品屬性,可直接擴充 JSONB

4. **業務邏輯**:
   - 寄賣單與收購單的商品結構相似(品牌、款式、內碼)
   - 僅配件與瑕疵為寄賣單專屬欄位,使用 NULL 處理收購單

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| 寄賣單與收購單分開兩張商品表 | 欄位語意清晰,避免 NULL 欄位 | 重複欄位定義,維護成本高 | 違反 DRY 原則,擴展性差 |
| 配件與瑕疵使用關聯表 (多對多) | 資料最正規化,查詢彈性高 | 需建立 3-4 張額外關聯表,複雜度高 | 過度設計,查詢複雜 |
| 配件與瑕疵使用字串逗號分隔 | 實作最簡單 | 無法索引,查詢效率低,無法驗證資料完整性 | PostgreSQL JSONB 更適合 |

### Implementation Guidance (實作指引)

**資料表設計**:
```sql
CREATE TABLE product_items (
    id BIGSERIAL PRIMARY KEY,
    service_order_id BIGINT NOT NULL REFERENCES service_orders(id),
    sequence_number INT NOT NULL, -- 1-4,顯示順序
    brand_name VARCHAR(100) NOT NULL,
    style_name VARCHAR(100) NOT NULL,
    internal_code VARCHAR(50) NULL,
    
    -- 寄賣單專屬欄位 (收購單為 NULL)
    accessories JSONB NULL, -- ["BOX", "DUST_BAG", "RECEIPT"]
    defects JSONB NULL, -- ["HARDWARE_RUST", "LEATHER_SCRATCH"]
    
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NULL,
    
    CONSTRAINT chk_sequence_number CHECK (sequence_number BETWEEN 1 AND 4),
    CONSTRAINT uq_service_order_sequence UNIQUE (service_order_id, sequence_number)
);

-- JSONB GIN 索引
CREATE INDEX idx_product_items_accessories ON product_items USING GIN (accessories);
CREATE INDEX idx_product_items_defects ON product_items USING GIN (defects);
```

**配件選項** (JSONB 值):
- `BOX` - 盒子
- `DUST_BAG` - 防塵袋
- `RECEIPT` - 購證
- `SHOPPING_BAG` - 提袋
- `SHOULDER_STRAP` - 肩帶
- `FELT_PAD` - 羊毛氈
- `PILLOW` - 枕頭
- `WARRANTY_CARD` - 保卡
- `LOCK_KEY` - 鎖頭/鑰匙
- `RIBBON_FLOWER` - 緞帶/花
- `BRAND_CARD` - 品牌小卡
- `CERTIFICATE` - 保證書
- `NONE` - 無

**瑕疵選項** (JSONB 值):
- `HARDWARE_RUST` - 五金生鏽
- `HARDWARE_SCRATCH` - 五金刮痕
- `HARDWARE_MISSING` - 五金掉落
- `LEATHER_SCRATCH` - 皮質刮痕
- `LEATHER_WEAR` - 皮質磨損
- `LEATHER_DENT` - 皮質壓痕
- `LINING_DIRTY` - 內裡髒污
- `CORNER_WEAR` - 四角磨損

---

## 7. 稽核日誌整合 (AuditLogService)

### Decision (決策)

使用專案現有的 **AuditLogService** 記錄所有服務單相關操作,**不允許修改服務單內容**。服務單一經建立僅允許變更狀態,如需更正應建立新的服務單並註記關聯。

### Rationale (理由)

1. **契約性質**:
   - 寄賣單與收購單是具有法律效力的契約文件
   - 一經簽署不應允許任意修改,否則造成法律與稽核問題
   - 若需更正應建立補充單或更正單,保留完整稽核軌跡
   
2. **重用現有架構**:
   - 專案已有完整的 `AuditLogService` 實作 ([AuditLogService.cs](AuditLogService.cs))
   - 支援記錄操作者、操作時間、操作類型、目標實體
   - 支援 BeforeState/AfterState 記錄狀態變更
   - 避免重複開發與維護成本
   
3. **稽核需求**:
   - 記錄所有服務單操作:建立(CREATE)、查詢(READ)、狀態變更(STATUS_CHANGE)、刪除(DELETE)
   - 狀態變更記錄舊狀態與新狀態,滿足稽核要求
   - 支援 TraceId 關聯同一次請求的多個操作
   
4. **查詢便利性**:
   - 透過 `GetAuditLogsByTraceIdAsync` 查詢特定服務單的所有操作記錄
   - 透過 `GetAuditLogsAsync` 支援多條件篩選(時間範圍、操作者、操作類型)

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| 允許修改 + modification_history 表 | 彈性高,可追蹤欄位變更 | 違反契約不可變原則,增加資料表維護成本 | 法律風險高 |
| 僅允許草稿狀態修改 | 平衡彈性與安全性 | 增加狀態管理複雜度,仍需建立修改歷史表 | 增加實作複雜度 |
| 建立更正單機制 | 完整保留稽核軌跡 | 需額外開發更正單流程 | 可於後續 Phase 實作 |

### Implementation Guidance (實作指引)

**服務單建立時的稽核記錄**:
```csharp
// ServiceOrderService.CreateBuybackOrderAsync
await _auditLogService.LogOperationAsync(
    operatorId: currentUserId,
    operatorName: currentUserName,
    operationType: "CREATE_BUYBACK_ORDER",
    targetType: "SERVICE_ORDER",
    targetId: serviceOrder.Id,
    beforeState: null,
    afterState: JsonSerializer.Serialize(new { 
        OrderNumber = serviceOrder.OrderNumber,
        CustomerId = serviceOrder.CustomerId,
        TotalAmount = serviceOrder.TotalAmount,
        Status = serviceOrder.Status
    }),
    traceId: HttpContext.TraceIdentifier
);
```

**服務單狀態變更時的稽核記錄**:
```csharp
// ServiceOrderService.UpdateStatusAsync
var oldStatus = serviceOrder.Status;
serviceOrder.Status = newStatus;

await _auditLogService.LogOperationAsync(
    operatorId: currentUserId,
    operatorName: currentUserName,
    operationType: "STATUS_CHANGE",
    targetType: "SERVICE_ORDER",
    targetId: serviceOrder.Id,
    beforeState: JsonSerializer.Serialize(new { Status = oldStatus }),
    afterState: JsonSerializer.Serialize(new { Status = newStatus }),
    traceId: HttpContext.TraceIdentifier
);
```

**查詢服務單稽核日誌**:
```csharp
// 查詢特定服務單的所有操作記錄
var auditLogs = await _auditLogService.GetAuditLogsAsync(new QueryAuditLogRequest
{
    TargetType = "SERVICE_ORDER",
    TargetId = serviceOrderId,
    StartTime = DateTime.UtcNow.AddMonths(-3),
    EndTime = DateTime.UtcNow,
    PageNumber = 1,
    PageSize = 50
});
```

**操作類型定義**:
- `CREATE_BUYBACK_ORDER` - 建立收購單
- `CREATE_CONSIGNMENT_ORDER` - 建立寄賣單
- `STATUS_CHANGE` - 狀態變更 (PENDING → COMPLETED → TERMINATED)
- `SOFT_DELETE` - 軟刪除服務單
- `VIEW_SENSITIVE_ATTACHMENT` - 查看敏感附件(身分證明文件)

**整合要點**:
1. 在 `ServiceOrderService` 注入 `IAuditLogService`
2. 所有 CUD 操作後呼叫 `LogOperationAsync`
3. 狀態變更必須記錄 BeforeState 與 AfterState
4. 使用 HttpContext.TraceIdentifier 作為 TraceId
5. 敏感附件查看記錄至 `attachment_view_logs` 表 + AuditLog 雙重記錄

---

## 8. 簽名記錄資料模型統一設計

### Decision (決策)

採用 **單一表格設計**,使用 **signature_type 欄位區分線下與線上簽名**,線下簽名儲存 **Base64 PNG**,線上簽名儲存 **Dropbox Sign Request ID**。

### Rationale (理由)

1. **統一管理**:
   - 線下與線上簽名本質上都是「簽名記錄」,屬於同一業務實體
   - 單一表格可簡化查詢與管理,避免 UNION 查詢
   
2. **欄位區分**:
   - `signature_type` 欄位清楚區分簽名方式
   - `signature_data` 欄位根據類型儲存不同內容(多型欄位)
   - `dropbox_sign_request_id` 欄位專屬於線上簽名,線下為 NULL
   
3. **擴展性**:
   - 未來若新增其他簽名方式(如電子憑證),僅需擴充 `signature_type` 值
   - 不需額外建立表格或複雜的繼承結構
   
4. **查詢便利性**:
   - 查詢某筆服務單的所有簽名記錄僅需單一 SELECT
   - 統計簽名狀態(已簽名/未簽名)更簡單

### Alternatives Considered (替代方案)

| 方案 | 優點 | 缺點 | 為何未採用 |
|------|------|------|------------|
| 線下與線上分開兩張表 | 欄位語意最清晰,避免 NULL 欄位 | 查詢需 UNION,維護成本高 | 違反 DRY 原則 |
| 繼承表 (Table Per Type) | 符合物件導向設計 | PostgreSQL 無原生繼承支援,需手動實作 | 增加複雜度 |
| 所有簽名資料存 JSONB | 彈性最高 | 失去型別安全,查詢效率低 | 不利於資料完整性驗證 |

### Implementation Guidance (實作指引)

**資料表設計**:
```sql
CREATE TABLE signature_records (
    id BIGSERIAL PRIMARY KEY,
    service_order_id BIGINT NOT NULL REFERENCES service_orders(id),
    
    document_type VARCHAR(50) NOT NULL, -- 'BUYBACK_CONTRACT', 'ONE_TIME_TRADE', 'CONSIGNMENT_CONTRACT'
    signature_type VARCHAR(20) NOT NULL, -- 'OFFLINE', 'ONLINE'
    
    -- 簽名資料 (多型欄位)
    signature_data TEXT NULL, -- 線下: Base64 PNG; 線上: NULL
    
    -- 線上簽名專屬欄位
    dropbox_sign_request_id VARCHAR(100) NULL,
    dropbox_sign_status VARCHAR(20) NULL, -- 'PENDING', 'SIGNED', 'DECLINED', 'EXPIRED'
    dropbox_sign_url TEXT NULL,
    
    signer_name VARCHAR(100) NOT NULL,
    signed_at TIMESTAMP NULL,
    
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT NOT NULL REFERENCES users(id),
    updated_at TIMESTAMP NULL,
    
    CONSTRAINT chk_document_type CHECK (document_type IN ('BUYBACK_CONTRACT', 'ONE_TIME_TRADE', 'CONSIGNMENT_CONTRACT')),
    CONSTRAINT chk_signature_type CHECK (signature_type IN ('OFFLINE', 'ONLINE')),
    CONSTRAINT chk_dropbox_status CHECK (dropbox_sign_status IS NULL OR dropbox_sign_status IN ('PENDING', 'SIGNED', 'DECLINED', 'EXPIRED'))
);
```

**文件類型**:
- `BUYBACK_CONTRACT` - 收購合約
- `ONE_TIME_TRADE` - 一時貿易申請書 (收購單專屬)
- `CONSIGNMENT_CONTRACT` - 寄賣合約書

**簽名流程**:
1. **線下簽名**: 店員引導客戶在網頁上簽名 → 前端將 Base64 PNG 傳送至後端 → 儲存至 `signature_data` 欄位 → `signed_at` 立即填入
2. **線上簽名**: 後端呼叫 Dropbox Sign API 寄送簽名邀請 → 儲存 Request ID → 狀態設為 `PENDING` → Webhook 回調更新狀態為 `SIGNED` → 填入 `signed_at`

---

## 總結與建議

### 技術架構總覽

```
┌─────────────────────────────────────────────────────────────┐
│                     ASP.NET Core 10 API                      │
├─────────────────────────────────────────────────────────────┤
│  Controllers (ServiceOrderController, SignatureController)   │
├─────────────────────────────────────────────────────────────┤
│  Services                                                     │
│  ├─ IdCardOcrService (Azure Vision + Gemini)                │
│  ├─ BlobStorageService (Azure Blob + SAS Token)             │
│  ├─ PdfGeneratorService (PDFsharp)                          │
│  ├─ DropboxSignService (Dropbox Sign API)                   │
│  ├─ ServiceOrderService                                      │
│  ├─ ModificationHistoryService                              │
│  └─ SignatureRecordService                                   │
├─────────────────────────────────────────────────────────────┤
│  Repositories (Dapper)                                        │
│  ├─ ServiceOrderRepository                                   │
│  ├─ ProductItemRepository                                    │
│  ├─ SignatureRecordRepository                                │
│  └─ ModificationHistoryRepository                            │
├─────────────────────────────────────────────────────────────┤
│  Database (PostgreSQL 15+)                                   │
│  ├─ service_orders (含每日重置序號)                         │
│  ├─ product_items (JSONB 配件/瑕疵)                         │
│  ├─ signature_records (統一線下/線上簽名)                   │
│  ├─ modification_history (JSONB 變更記錄)                   │
│  └─ dropbox_sign_webhook_events (防重複處理)                │
├─────────────────────────────────────────────────────────────┤
│  External Services                                            │
│  ├─ Azure Computer Vision 4.0 (OCR 文字擷取)                │
│  ├─ Google Gemini Pro Vision (結構化解析)                   │
│  ├─ Azure Blob Storage (附件儲存 + SAS Token)               │
│  └─ Dropbox Sign API (線上簽名)                              │
└─────────────────────────────────────────────────────────────┘
```

### 開發優先順序建議

**Phase 1 - 核心功能 (P1)**:
1. 服務單資料模型與資料庫 Schema
2. 客戶管理 (搜尋、新增)
3. 商品項目管理 (1-4 件)
4. 線下收購單建立 (含線下簽名)
5. 線下寄賣單建立 (含線下簽名)

**Phase 2 - 輔助功能 (P2)**:
6. Azure Blob Storage 附件管理
7. 身分證 OCR 辨識 (Azure Vision + Gemini)
8. PDF 合約產生與簽名合併 (PDFsharp)
9. 服務單查詢與修改歷史

**Phase 3 - 進階功能 (P3)**:
10. Dropbox Sign API 整合 (線上簽名)
11. Webhook 端點與狀態同步
12. 個資稽核日誌

### 效能優化建議

1. **資料庫索引**: 確保所有外鍵、查詢條件欄位都有索引
2. **JSONB GIN 索引**: 配件與瑕疵欄位建立 GIN 索引
3. **SAS Token 快取**: 使用 MemoryCache 減少 Azure API 呼叫
4. **分頁查詢**: 所有列表查詢必須實作分頁 (預設 20 筆,最大 100 筆)
5. **Connection Pooling**: 確保 Dapper 使用連線池

### 安全性檢查清單

- [x] JWT Bearer Token 認證
- [x] FluentValidation 輸入驗證
- [x] Dapper 參數化查詢 (防 SQL Injection)
- [x] Azure Blob SAS Token 時效性控制
- [x] Dropbox Sign Webhook HMAC 驗證
- [x] 敏感附件查看記錄 (個資稽核)
- [x] 樂觀鎖並發控制
- [x] 軟刪除機制

### 測試策略

1. **單元測試**: 所有 Service 與 Validator
2. **整合測試**: 所有 API 端點 (使用 Testcontainers PostgreSQL)
3. **OCR 辨識測試**: 準備 30-50 張真實身分證照片樣本
4. **並發測試**: 模擬 100 位店員同時建立服務單
5. **Webhook 測試**: 使用 Dropbox Sign Sandbox 環境

---

**下一步**: 進入 Phase 1 資料模型設計,產生 `data-model.md` 與資料庫遷移腳本。
