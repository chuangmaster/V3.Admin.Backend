using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Middleware;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// OCR 控制器
/// </summary>
/// <remarks>
/// 提供身分證 OCR 辨識端點，供服務單建立流程使用。
/// </remarks>
[Route("api/ocr")]
[Authorize]
public class OcrController : BaseApiController
{
    private const int _maxImageBytes = 10 * 1024 * 1024;

    private readonly IIdCardOcrService _idCardOcrService;
    private readonly ILogger<OcrController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="idCardOcrService">身分證 OCR 服務</param>
    /// <param name="logger">日誌記錄器</param>
    public OcrController(IIdCardOcrService idCardOcrService, ILogger<OcrController> logger)
    {
        _idCardOcrService = idCardOcrService;
        _logger = logger;
    }

    /// <summary>
    /// 身分證 OCR 辨識
    /// </summary>
    /// <remarks>
    /// 解析 Base64 圖片並呼叫 OCR 服務，回傳姓名/證號/信心度。
    /// 需要權限: customer.create（依據 api-contracts.md 契約）
    /// </remarks>
    /// <param name="request">OCR 請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OCR 辨識結果</returns>
    [HttpPost("id-card")]
    [RequirePermission("customer.create")]
    [ProducesResponseType(typeof(ApiResponseModel<OcrResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecognizeIdCard(
        [FromBody] OcrIdCardRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ImageBase64))
            {
                return ValidationError("ImageBase64 不可為空");
            }

            if (!IsAllowedImageContentType(request.ContentType))
            {
                return ValidationError("ContentType 僅允許 image/jpeg 或 image/png");
            }

            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(request.ImageBase64);
            }
            catch (FormatException)
            {
                return ValidationError("ImageBase64 格式不正確");
            }

            if (imageBytes.Length == 0)
            {
                return ValidationError("圖片不可為空");
            }

            if (imageBytes.Length > _maxImageBytes)
            {
                return ValidationError("圖片大小不可超過 10MB");
            }

            (string? name, string? idNumber, double confidence) =
                await _idCardOcrService.RecognizeAsync(imageBytes, cancellationToken);

            var response = new OcrResultResponse
            {
                Name = name,
                IdNumber = idNumber,
                Confidence = confidence,
            };

            return Success(response, response.IsLowConfidence ? "辨識完成（信心度偏低，請人工確認）" : "辨識完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "身分證 OCR 辨識失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("身分證 OCR 辨識失敗");
        }
    }

    /// <summary>
    /// 檢查是否為允許的圖片 MIME 類型
    /// </summary>
    private static bool IsAllowedImageContentType(string? contentType)
    {
        return contentType is "image/jpeg" or "image/png";
    }
}
