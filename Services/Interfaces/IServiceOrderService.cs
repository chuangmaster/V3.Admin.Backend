using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 服務單業務邏輯介面
/// </summary>
public interface IServiceOrderService
{
    /// <summary>
    /// 建立線下收購單 (US1)
    /// </summary>
    /// <param name="request">建立收購單請求</param>
    /// <param name="createdBy">建立者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的服務單完整資訊</returns>
    Task<ServiceOrderDetailDto> CreateBuybackOrderAsync(
        CreateBuybackOrderRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 產生收購合約預覽 PDF (US1)
    /// </summary>
    /// <remarks>
    /// 目前以輸入欄位直接填充預覽為主，後續可改為讀取模板 + 定位填值。
    /// </remarks>
    /// <param name="request">建立收購單請求（用於填值）</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>預覽 PDF 位元組</returns>
    Task<byte[]> PreviewBuybackContractPdfAsync(
        CreateBuybackOrderRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 合併 Base64 簽章至 PDF (US1)
    /// </summary>
    /// <param name="request">合併簽章請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合併後 PDF 位元組</returns>
    Task<byte[]> MergeSignatureAsync(
        MergeSignatureRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 確認服務單並儲存最終 PDF (US1)
    /// </summary>
    /// <remarks>
    /// 將最終 PDF 上傳至 Blob Storage，並建立附件與簽名記錄。
    /// </remarks>
    /// <param name="serviceOrderId">服務單 ID</param>
    /// <param name="request">確認請求</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>確認結果</returns>
    Task<ConfirmOrderResultDto> ConfirmOrderAsync(
        Guid serviceOrderId,
        ConfirmOrderRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    );
}
