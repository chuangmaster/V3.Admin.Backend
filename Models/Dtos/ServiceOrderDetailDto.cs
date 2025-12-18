namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 服務單完整資訊 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層傳遞服務單完整資訊 (含客戶、商品、附件、簽名記錄)
/// </remarks>
public class ServiceOrderDetailDto
{
    /// <summary>
    /// 服務單資訊
    /// </summary>
    public ServiceOrderDto ServiceOrder { get; set; } = new();

    /// <summary>
    /// 客戶資訊
    /// </summary>
    public CustomerDto Customer { get; set; } = new();

    /// <summary>
    /// 商品項目清單
    /// </summary>
    public List<ProductItemDto> ProductItems { get; set; } = new();

    /// <summary>
    /// 附件清單
    /// </summary>
    public List<AttachmentDto> Attachments { get; set; } = new();

    /// <summary>
    /// 簽名記錄清單
    /// </summary>
    public List<SignatureRecordDto> SignatureRecords { get; set; } = new();
}
