using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Middleware;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 權限管理 API 控制器
/// </summary>
/// <remarks>
/// 提供權限的建立、查詢、更新、刪除等操作的 REST API 端點。
/// 所有端點均須通過授權驗證，部分端點需要特定權限。
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionController : BaseApiController
{
    private readonly IPermissionService _permissionService;
    private readonly IPermissionValidationService _permissionValidationService;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<PermissionController> _logger;

    public PermissionController(
        IPermissionService permissionService,
        IPermissionValidationService permissionValidationService,
        IPermissionRepository permissionRepository,
        ILogger<PermissionController> logger
    )
    {
        _permissionService = permissionService;
        _permissionValidationService = permissionValidationService;
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 取得分頁權限清單
    /// </summary>
    /// <remarks>
    /// 查詢權限列表，支援分頁、關鍵字搜尋和類型篩選。
    /// 需要具有 "permission.read" 權限。
    /// </remarks>
    /// <param name="pageNumber">頁碼（從 1 開始）</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="searchKeyword">搜尋關鍵字（按權限代碼或名稱篩選）</param>
    /// <param name="permissionType">權限類型篩選（route 或 function）</param>
    /// <returns>分頁權限清單</returns>
    [HttpGet]
    [RequirePermission("permission.read")]
    [ProducesResponseType(
        typeof(PagedApiResponseModel<PermissionResponse>),
        StatusCodes.Status200OK
    )]
    public async Task<IActionResult> GetPermissions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchKeyword = null,
        [FromQuery] string? permissionType = null
    )
    {
        try
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            var paged = await _permissionService.GetPermissionsAsync(
                pageNumber,
                pageSize,
                searchKeyword,
                permissionType
            );

            var items =
                paged.Items != null
                    ? paged
                        .Items.Select(dto => new PermissionResponse
                        {
                            Id = dto.Id,
                            PermissionCode = dto.PermissionCode,
                            Name = dto.Name,
                            Description = dto.Description,
                            PermissionType = dto.PermissionType,
                            Version = dto.Version,
                            CreatedAt = dto.CreatedAt,
                        })
                        .ToList()
                    : new List<PermissionResponse>();

            return PagedSuccess(
                items,
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount,
                "Query successful"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query permission list | TraceId: {TraceId}", TraceId);
            return InternalError("Failed to query permission list");
        }
    }

    /// <summary>
    /// 建立新權限
    /// </summary>
    /// <remarks>
    /// 建立一個新的權限，需要具有 "permission.create" 權限。
    /// 權限代碼必須唯一。
    /// </remarks>
    /// <param name="request">建立權限請求</param>
    /// <returns>建立的權限詳細資訊</returns>
    [HttpPost]
    [RequirePermission("permission.create")]
    [ProducesResponseType(typeof(ApiResponseModel<PermissionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            var permissionDto = await _permissionService.CreatePermissionAsync(
                request,
                userId.Value
            );
            var responseData = new PermissionResponse
            {
                Id = permissionDto.Id,
                PermissionCode = permissionDto.PermissionCode,
                Name = permissionDto.Name,
                Description = permissionDto.Description,
                PermissionType = permissionDto.PermissionType,
                CreatedAt = permissionDto.CreatedAt,
                Version = permissionDto.Version
            };
            return Created(responseData, "建立成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to create permission: {Message} | TraceId: {TraceId}",
                ex.Message,
                TraceId
            );
            return BusinessError(ex.Message, ResponseCodes.DUPLICATE_PERMISSION_CODE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission | TraceId: {TraceId}", TraceId);
            return InternalError();
        }
    }

    /// <summary>
    /// 取得指定權限
    /// </summary>
    /// <remarks>
    /// 根據權限 ID 查詢單一權限的詳細資訊。
    /// </remarks>
    /// <param name="id">權限 ID</param>
    /// <returns>權限詳細資訊</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseModel<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermission([FromRoute] Guid id)
    {
        try
        {
            var permissionDto = await _permissionService.GetPermissionByIdAsync(id);
            if (permissionDto == null)
            {
                return NotFound("Permission not found", ResponseCodes.PERMISSION_NOT_FOUND);
            }

            var response = new PermissionResponse
            {
                Id = permissionDto.Id,
                PermissionCode = permissionDto.PermissionCode,
                Name = permissionDto.Name,
                Description = permissionDto.Description,
                PermissionType = permissionDto.PermissionType,
                CreatedAt = permissionDto.CreatedAt,
                Version = permissionDto.Version
            };
            return Success(response, "Query successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to query permission: {Id} | TraceId: {TraceId}",
                id,
                TraceId
            );
            return InternalError();
        }
    }

    /// <summary>
    /// 更新權限
    /// </summary>
    /// <remarks>
    /// 更新指定 ID 的權限資訊。需要具有 "permission.update" 權限。
    /// 支援樂觀併發控制，若發生版本衝突會傳回 409 Conflict。
    /// </remarks>
    /// <param name="id">權限 ID</param>
    /// <param name="request">更新權限請求</param>
    /// <returns>更新後的權限詳細資訊</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePermission(
        [FromRoute] Guid id,
        [FromBody] UpdatePermissionRequest request
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            var permissionDto = await _permissionService.UpdatePermissionAsync(
                id,
                request,
                userId.Value
            );
            var response = new PermissionResponse 
            {
                Id = permissionDto.Id,
                PermissionCode = permissionDto.PermissionCode,
                Name = permissionDto.Name,
                Description = permissionDto.Description,
                PermissionType = permissionDto.PermissionType,
                CreatedAt = permissionDto.CreatedAt,
                Version = permissionDto.Version
            };
            return Success(response, "Update successful");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Permission not found: {Id} | TraceId: {TraceId}", id, TraceId);
            return NotFound(ex.Message, ResponseCodes.PERMISSION_NOT_FOUND);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to update permission: {Message} | TraceId: {TraceId}",
                ex.Message,
                TraceId
            );
            return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating permission: {Id} | TraceId: {TraceId}",
                id,
                TraceId
            );
            return InternalError();
        }
    }

    /// <summary>
    /// 刪除權限（軟刪除）
    /// </summary>
    /// <remarks>
    /// 刪除指定 ID 的權限。需要具有 "permission.delete" 權限。
    /// 採用軟刪除方式，保留審計記錄。需要傳入版本號以進行樂觀併發控制。
    /// 若權限被角色使用則無法刪除。
    /// </remarks>
    /// <param name="id">權限 ID</param>
    /// <param name="request">刪除權限請求（含版本號）</param>
    /// <returns>刪除結果</returns>
    [HttpDelete("{id}")]
    [RequirePermission("permission.delete")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeletePermission(
        [FromRoute] Guid id,
        [FromBody] DeletePermissionRequest request
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            await _permissionService.DeletePermissionAsync(id, request, userId.Value);
            return Success("Permission deleted successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Permission not found: {Id} | TraceId: {TraceId}", id, TraceId);
            return NotFound(ex.Message, ResponseCodes.PERMISSION_NOT_FOUND);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete permission: {Message} | TraceId: {TraceId}",
                ex.Message,
                TraceId
            );

            if (ex.Message.Contains("version mismatch", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
            }

            return BusinessError(ex.Message, ResponseCodes.PERMISSION_IN_USE);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting permission: {Id} | TraceId: {TraceId}",
                id,
                TraceId
            );
            return InternalError();
        }
    }

    /// <summary>
    /// 檢查當前用戶是否擁有指定權限
    /// </summary>
    /// <remarks>
    /// 根據權限代碼檢查當前授權用戶是否擁有該權限。
    /// 用於前端查詢用戶權限以控制 UI 元件顯示/隱藏。
    /// </remarks>
    /// <param name="permissionCode">權限代碼</param>
    /// <returns>權限檢查結果</returns>
    [HttpGet("check/{permissionCode}")]
    [ProducesResponseType(typeof(CheckPermissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckPermission([FromRoute] string permissionCode)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            // 檢查用戶是否擁有該權限
            bool hasPermission = await _permissionValidationService.ValidatePermissionAsync(
                userId.Value,
                permissionCode
            );

            // 取得權限資訊（如果存在）
            var permission = await _permissionRepository.GetByCodeAsync(permissionCode);

            var response = new CheckPermissionResponse
            {
                PermissionCode = permissionCode,
                PermissionType = permission?.PermissionType,
                HasPermission = hasPermission,
            };

            return Success(response, "權限檢查完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "檢查權限失敗: {PermissionCode} | TraceId: {TraceId}",
                permissionCode,
                TraceId
            );
            return InternalError("檢查權限失敗");
        }
    }

    /// <summary>
    /// 驗證授權用戶的單一權限
    /// </summary>
    /// <remarks>
    /// 驗證目前授權用戶是否擁有指定的權限代碼。
    /// 此端點即時查詢用戶的所有角色及其關聯的權限，並檢查用戶是否擁有指定的權限。
    /// 支援功能權限（如 inventory.create）和路由權限驗證。
    /// </remarks>
    /// <param name="request">驗證權限請求，包含要驗證的權限代碼</param>
    /// <returns>
    /// 返回 PermissionValidationResponse，包含：
    /// - HasPermission: 用戶是否擁有該權限（true/false）
    /// - Message: 驗證結果描述
    /// </returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(PermissionValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidatePermission(
        [FromBody] ValidatePermissionRequest request
    )
    {
        try
        {
            // 取得當前授權用戶的 ID
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            // 驗證權限代碼不為空
            if (string.IsNullOrWhiteSpace(request.PermissionCode))
            {
                return BadRequest("權限代碼不能為空");
            }

            // 使用 PermissionValidationService 進行權限驗證
            bool hasPermission = await _permissionValidationService.ValidatePermissionAsync(
                userId.Value,
                request.PermissionCode.Trim()
            );

            // 組構驗證結果回應
            var response = new PermissionValidationResponse { Data = hasPermission };
            return Success(response, hasPermission ? "用戶擁有該權限" : "用戶不擁有該權限");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "驗證權限失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("驗證權限失敗");
        }
    }
}
