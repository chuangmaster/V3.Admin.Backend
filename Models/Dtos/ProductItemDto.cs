namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 商品項目 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層傳遞服務單的商品項目資訊
/// </remarks>
public class ProductItemDto
{
    /// <summary>
    /// 商品項目 ID
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
    /// 內碼
    /// </summary>
    public string? InternalCode { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
