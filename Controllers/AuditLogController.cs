using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 稽核日誌 API 控制器
/// </summary>
/// <remarks>
/// 提供稽核日誌的查詢 API
/// 稽核日誌為 read-only，僅支援查詢操作
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogController : BaseApiController
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    /// <summary>
    /// 初始化稽核日誌控制器
    /// </summary>
    /// <param name="auditLogService">稽核日誌服務</param>
    /// <param name="logger">日誌記錄器</param>
    public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 查詢稽核日誌列表
    /// </summary>
    /// <param name="request">查詢請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>稽核日誌列表</returns>
    /// <remarks>
    /// 支援時間範圍、操作者、操作類型、目標類型等多條件篩選
    /// 結果按操作時間降序排列
    ///
    /// 範例請求：
    /// ```
    /// POST /api/auditlog/query
    /// {
    ///   "startTime": "2025-10-01T00:00:00Z",
    ///   "endTime": "2025-11-06T23:59:59Z",
    ///   "operationType": "create",
    ///   "targetType": "permission",
    ///   "pageNumber": 1,
    ///   "pageSize": 20
    /// }
    /// ```
    /// </remarks>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryAuditLogs(
        [FromBody] QueryAuditLogRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (request == null)
            {
                return ValidationError("查詢請求不能為 null");
            }

            var response = await _auditLogService.GetAuditLogsAsync(request, cancellationToken);

            _logger.LogInformation(
                "稽核日誌查詢成功: PageNumber={PageNumber}, PageSize={PageSize}, TotalCount={TotalCount}",
                request.PageNumber,
                request.PageSize,
                response.TotalCount
            );

            return Success(response, "查詢稽核日誌成功");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "稽核日誌查詢參數錯誤");
            return ValidationError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢稽核日誌時發生錯誤");
            return InternalError("查詢稽核日誌失敗");
        }
    }

    /// <summary>
    /// 根據 ID 查詢單筆稽核日誌
    /// </summary>
    /// <param name="id">稽核日誌 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>稽核日誌詳情</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuditLogById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return ValidationError("稽核日誌 ID 不能為空");
            }

            var auditLog = await _auditLogService.GetAuditLogByIdAsync(id, cancellationToken);
            if (auditLog == null)
            {
                _logger.LogWarning("稽核日誌不存在: {Id}", id);
                return NotFound($"稽核日誌 '{id}' 不存在");
            }

            _logger.LogInformation("稽核日誌已查詢: {Id}", id);
            return Success(auditLog, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢稽核日誌時發生錯誤");
            return InternalError("查詢稽核日誌失敗");
        }
    }

    /// <summary>
    /// 根據追蹤 ID 查詢相關稽核日誌
    /// </summary>
    /// <param name="traceId">追蹤 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>相關稽核日誌列表</returns>
    /// <remarks>
    /// 用於追蹤同一請求中執行的多項操作
    /// </remarks>
    [HttpGet("trace/{traceId}")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuditLogsByTraceId(
        string traceId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return ValidationError("追蹤 ID 不能為空");
            }

            var auditLogs = await _auditLogService.GetAuditLogsByTraceIdAsync(
                traceId,
                cancellationToken
            );

            _logger.LogInformation(
                "根據追蹤 ID 查詢稽核日誌成功: TraceId={TraceId}, Count={Count}",
                traceId,
                auditLogs.Count
            );

            return Success(auditLogs, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢稽核日誌時發生錯誤");
            return InternalError("查詢稽核日誌失敗");
        }
    }
}
