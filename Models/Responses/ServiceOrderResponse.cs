namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 服務單回應模型
/// </summary>
/// <remarks>
/// 用於回傳服務單完整資訊 (含客戶、商品、附件、簽名記錄)
/// </remarks>
public class ServiceOrderResponse
{
    /// <summary>
    /// 服務單 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 服務日期
    /// </summary>
    public DateTime ServiceDate { get; set; }

    /// <summary>
    /// 服務單編號 (BS/CS + YYYYMMDD + 001)
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// 服務單類型 (BUYBACK/CONSIGNMENT)
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// 服務單來源 (OFFLINE/ONLINE)
    /// </summary>
    public string OrderSource { get; set; } = string.Empty;

    /// <summary>
    /// 總金額
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 服務單狀態 (PENDING/COMPLETED/TERMINATED)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 客戶資訊
    /// </summary>
    public CustomerResponse? Customer { get; set; }

    /// <summary>
    /// 商品項目清單
    /// </summary>
    public List<ProductItemResponse> ProductItems { get; set; } = new();

    /// <summary>
    /// 附件清單
    /// </summary>
    public List<AttachmentResponse> Attachments { get; set; } = new();

    /// <summary>
    /// 簽名記錄清單
    /// </summary>
    public List<SignatureRecordResponse> SignatureRecords { get; set; } = new();

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 資料版本號
    /// </summary>
    public int Version { get; set; }
}

/// <summary>
/// 商品項目回應模型
/// </summary>
public class ProductItemResponse
{
    /// <summary>
    /// 商品項目 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 商品序號 (1-4)
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// 品牌名稱
    /// </summary>
    public string BrandName { get; set; } = string.Empty;

    /// <summary>
    /// 款式
    /// </summary>
    public string StyleName { get; set; } = string.Empty;

    /// <summary>
    /// 內碼
    /// </summary>
    public string? InternalCode { get; set; }
}

/// <summary>
/// 附件回應模型
/// </summary>
public class AttachmentResponse
{
    /// <summary>
    /// 附件 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 附件類型
    /// </summary>
    public string AttachmentType { get; set; } = string.Empty;

    /// <summary>
    /// 原始檔名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 檔案大小 (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME 類型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 上傳時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 簽名記錄回應模型
/// </summary>
public class SignatureRecordResponse
{
    /// <summary>
    /// 簽名記錄 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 文件類型
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// 簽名方式 (OFFLINE/ONLINE)
    /// </summary>
    public string SignatureType { get; set; } = string.Empty;

    /// <summary>
    /// 簽名者姓名
    /// </summary>
    public string? SignerName { get; set; }

    /// <summary>
    /// 簽名時間 (UTC)
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Dropbox Sign 狀態
    /// </summary>
    public string? DropboxSignStatus { get; set; }

    /// <summary>
    /// Dropbox Sign 簽名連結
    /// </summary>
    public string? DropboxSignUrl { get; set; }
}
