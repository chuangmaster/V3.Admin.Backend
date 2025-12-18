namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 商品項目實體
/// </summary>
/// <remarks>
/// 對應資料庫 product_items 資料表,代表服務單內的單一商品項目 (1-4 件)
/// </remarks>
public class ProductItem
{
    /// <summary>
    /// 商品項目唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 所屬服務單 ID
    /// </summary>
    public Guid ServiceOrderId { get; set; }

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
    /// 內碼 (選填)
    /// </summary>
    public string? InternalCode { get; set; }

    /// <summary>
    /// 配件 (JSON 格式字串,寄賣單專屬)
    /// </summary>
    public string? Accessories { get; set; }

    /// <summary>
    /// 瑕疵處 (JSON 格式字串,寄賣單專屬)
    /// </summary>
    public string? Defects { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
