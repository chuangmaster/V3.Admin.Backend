using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Middleware;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 服務單控制器
/// </summary>
/// <remarks>
/// 提供 US1 線下收購單建立、合約預覽、簽名合併與確認等端點。
/// </remarks>
[Route("api/service-orders")]
[Authorize]
public class ServiceOrderController : BaseApiController
{
    private readonly IServiceOrderService _serviceOrderService;
    private readonly ILogger<ServiceOrderController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="serviceOrderService">服務單服務</param>
    /// <param name="logger">日誌記錄器</param>
    public ServiceOrderController(
        IServiceOrderService serviceOrderService,
        ILogger<ServiceOrderController> logger
    )
    {
        _serviceOrderService = serviceOrderService;
        _logger = logger;
    }

    /// <summary>
    /// 建立線下收購單 (US1)
    /// </summary>
    /// <remarks>
    /// 建立服務單、商品項目、身分證附件與簽名記錄，並回傳完整資料。
    /// 需要權限: serviceOrder.buyback.create
    /// </remarks>
    /// <param name="request">建立收購單請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的服務單完整資訊</returns>
    [HttpPost]
    [RequirePermission("serviceOrder.buyback.create")]
    [ProducesResponseType(typeof(ApiResponseModel<ServiceOrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateBuybackOrder(
        [FromBody] CreateBuybackOrderRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            ServiceOrderDetailDto detail = await _serviceOrderService.CreateBuybackOrderAsync(
                request,
                userId.Value,
                cancellationToken
            );

            var response = MapToResponse(detail);
            return Created(response, "建立成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "建立收購單失敗: {Message} | TraceId: {TraceId}", ex.Message, TraceId);
            return BusinessError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立收購單時發生未預期的錯誤 | TraceId: {TraceId}", TraceId);
            return InternalError("建立收購單失敗");
        }
    }

    /// <summary>
    /// 產生收購合約預覽 PDF (US1)
    /// </summary>
    /// <remarks>
    /// 依輸入資料填充模板並回傳 PDF Base64，供前端預覽。
    /// 需要權限: serviceOrder.buyback.create
    /// </remarks>
    /// <param name="request">建立收購單請求（用於填值）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>PDF Base64</returns>
    [HttpPost("buyback/contract/preview")]
    [RequirePermission("serviceOrder.buyback.create")]
    [ProducesResponseType(typeof(ApiResponseModel<PdfBase64Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewBuybackContractPdf(
        [FromBody] CreateBuybackOrderRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            byte[] pdfBytes = await _serviceOrderService.PreviewBuybackContractPdfAsync(
                request,
                userId.Value,
                cancellationToken
            );

            var response = new PdfBase64Response
            {
                PdfBase64 = Convert.ToBase64String(pdfBytes),
                ContentType = "application/pdf",
            };

            return Success(response, "產生成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "產生合約預覽失敗: {Message} | TraceId: {TraceId}", ex.Message, TraceId);
            return BusinessError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "產生合約預覽時發生未預期的錯誤 | TraceId: {TraceId}", TraceId);
            return InternalError("產生合約預覽失敗");
        }
    }

    /// <summary>
    /// 合併簽章並回傳預覽文件 (US1)
    /// </summary>
    /// <remarks>
    /// 將線下簽章（Base64 PNG）合併到 PDF 指定位置，回傳合併後的 PDF Base64。
    /// 需要權限: serviceOrder.buyback.create
    /// </remarks>
    /// <param name="serviceOrderId">服務單 ID</param>
    /// <param name="request">合併簽章請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合併後的 PDF Base64</returns>
    [HttpPost("{serviceOrderId:guid}/signatures/merge-preview")]
    [RequirePermission("serviceOrder.buyback.create")]
    [ProducesResponseType(typeof(ApiResponseModel<PdfBase64Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MergeSignature(
        [FromRoute] Guid serviceOrderId,
        [FromBody] MergeSignatureRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (serviceOrderId == Guid.Empty)
            {
                return ValidationError("serviceOrderId 不可為空");
            }

            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            byte[] merged = await _serviceOrderService.MergeSignatureAsync(request, cancellationToken);

            var response = new PdfBase64Response
            {
                PdfBase64 = Convert.ToBase64String(merged),
                ContentType = "application/pdf",
            };

            return Success(response, "合併成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "合併簽章失敗: {Message} | TraceId: {TraceId}", ex.Message, TraceId);
            return BusinessError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合併簽章時發生未預期的錯誤 | TraceId: {TraceId}", TraceId);
            return InternalError("合併簽章失敗");
        }
    }

    /// <summary>
    /// 確認服務單並儲存最終文件 (US1)
    /// </summary>
    /// <remarks>
    /// 上傳最終 PDF 至 Blob Storage，並建立附件與簽名記錄。
    /// 需要權限: serviceOrder.buyback.create
    /// </remarks>
    /// <param name="serviceOrderId">服務單 ID</param>
    /// <param name="request">確認請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>確認結果</returns>
    [HttpPost("{serviceOrderId:guid}/signatures/offline")]
    [RequirePermission("serviceOrder.buyback.create")]
    [ProducesResponseType(typeof(ApiResponseModel<ConfirmOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmOrder(
        [FromRoute] Guid serviceOrderId,
        [FromBody] ConfirmOrderRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            ConfirmOrderResultDto result = await _serviceOrderService.ConfirmOrderAsync(
                serviceOrderId,
                request,
                userId.Value,
                cancellationToken
            );

            var response = new ConfirmOrderResponse
            {
                AttachmentId = result.AttachmentId,
                SignatureRecordId = result.SignatureRecordId,
                BlobPath = result.BlobPath,
                SasUrl = result.SasUri.ToString(),
                ExpiresAt = result.ExpiresAt,
            };

            return Success(response, "確認成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "確認服務單失敗: {Message} | TraceId: {TraceId}", ex.Message, TraceId);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "確認服務單失敗: {Message} | TraceId: {TraceId}", ex.Message, TraceId);
            return BusinessError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "確認服務單時發生未預期的錯誤 | TraceId: {TraceId}", TraceId);
            return InternalError("確認服務單失敗");
        }
    }

    private static ServiceOrderResponse MapToResponse(ServiceOrderDetailDto detail)
    {
        return new ServiceOrderResponse
        {
            Id = detail.ServiceOrder.Id,
            ServiceDate = detail.ServiceOrder.ServiceDate,
            OrderNumber = detail.ServiceOrder.OrderNumber,
            OrderType = detail.ServiceOrder.OrderType,
            OrderSource = detail.ServiceOrder.OrderSource,
            TotalAmount = detail.ServiceOrder.TotalAmount,
            Status = detail.ServiceOrder.Status,
            Customer = new CustomerResponse
            {
                Id = detail.Customer.Id,
                Name = detail.Customer.Name,
                PhoneNumber = detail.Customer.PhoneNumber,
                Email = detail.Customer.Email,
                IdNumber = detail.Customer.IdNumber,
                CreatedAt = detail.Customer.CreatedAt,
                UpdatedAt = detail.Customer.UpdatedAt,
                Version = detail.Customer.Version,
            },
            ProductItems = detail.ProductItems.Select(item => new ProductItemResponse
                {
                    Id = item.Id,
                    SequenceNumber = item.SequenceNumber,
                    BrandName = item.BrandName,
                    StyleName = item.StyleName,
                    InternalCode = item.InternalCode,
                })
                .ToList(),
            Attachments = detail.Attachments.Select(att => new AttachmentResponse
                {
                    Id = att.Id,
                    AttachmentType = att.AttachmentType,
                    FileName = att.FileName,
                    FileSize = att.FileSize,
                    ContentType = att.ContentType,
                    CreatedAt = att.CreatedAt,
                })
                .ToList(),
            SignatureRecords = detail.SignatureRecords.Select(sig => new SignatureRecordResponse
                {
                    Id = sig.Id,
                    DocumentType = sig.DocumentType,
                    SignatureType = sig.SignatureType,
                    SignerName = sig.SignerName,
                    SignedAt = sig.SignedAt,
                    DropboxSignStatus = sig.DropboxSignStatus,
                    DropboxSignUrl = sig.DropboxSignUrl,
                })
                .ToList(),
            CreatedAt = detail.ServiceOrder.CreatedAt,
            UpdatedAt = detail.ServiceOrder.UpdatedAt,
            Version = detail.ServiceOrder.Version,
        };
    }
}
