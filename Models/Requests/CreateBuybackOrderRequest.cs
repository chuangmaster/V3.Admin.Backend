namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 建立線下收購單請求模型
/// </summary>
/// <remarks>
/// MVP (US1) 以「收購單 + 線下」為主:
/// - 客戶可選擇既有客戶 (CustomerId) 或新增客戶 (NewCustomer)
/// - 至少 1 件、最多 4 件商品項目
/// - 需提供至少 1 份身分證明圖片 (Base64)
/// </remarks>
public class CreateBuybackOrderRequest
{
    /// <summary>
    /// 服務單類型
    /// </summary>
    /// <remarks>
    /// US1 固定為 BUYBACK
    /// </remarks>
    public string OrderType { get; set; } = "BUYBACK";

    /// <summary>
    /// 服務單來源
    /// </summary>
    /// <remarks>
    /// US1 固定為 OFFLINE
    /// </remarks>
    public string OrderSource { get; set; } = "OFFLINE";

    /// <summary>
    /// 既有客戶 ID (擇一)
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 新增客戶資料 (擇一)
    /// </summary>
    public CreateCustomerRequest? NewCustomer { get; set; }

    /// <summary>
    /// 商品項目清單 (1-4 件)
    /// </summary>
    public List<CreateBuybackProductItemRequest> ProductItems { get; set; } = new();

    /// <summary>
    /// 總金額
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 身分證明圖片 Base64 (不含 data: 前綴)
    /// </summary>
    public string IdCardImageBase64 { get; set; } = string.Empty;

    /// <summary>
    /// 身分證明圖片 MIME 類型
    /// </summary>
    /// <remarks>
    /// 允許: image/jpeg, image/png
    /// </remarks>
    public string IdCardImageContentType { get; set; } = string.Empty;

    /// <summary>
    /// 身分證明圖片原始檔名
    /// </summary>
    public string IdCardImageFileName { get; set; } = string.Empty;
}

/// <summary>
/// 建立收購單商品項目請求模型
/// </summary>
public class CreateBuybackProductItemRequest
{
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
