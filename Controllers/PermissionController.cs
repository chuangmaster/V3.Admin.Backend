using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// Permission Management API Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionController : BaseApiController
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionController> _logger;

    public PermissionController(IPermissionService permissionService, ILogger<PermissionController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of permissions with optional filtering
    /// </summary>
    /// <param name="pageNumber">Page number (starts at 1)</param>
    /// <param name="pageSize">Page size (max 100)</param>
    /// <param name="searchKeyword">Search keyword to filter by code or name</param>
    /// <param name="permissionType">Filter by permission type (route or function)</param>
    /// <returns>Paginated list of permissions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PermissionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchKeyword = null,
        [FromQuery] string? permissionType = null)
    {
        try
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1 || pageSize > 100)
            {
                pageSize = 20;
            }

            var (items, totalCount) = await _permissionService.GetPermissionsAsync(
                pageNumber, pageSize, searchKeyword, permissionType);

            var response = new PermissionListResponse
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Code = ResponseCodes.SUCCESS,
                Message = "Query successful",
                TraceId = TraceId,
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query permission list | TraceId: {TraceId}", TraceId);
            return InternalError("Failed to query permission list");
        }
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    /// <param name="request">Create permission request</param>
    /// <returns>Created permission details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return UnauthorizedResponse();
            }

            var permission = await _permissionService.CreatePermissionAsync(request, userIdGuid);
            return Created($"/api/permissions/{permission.Id}", permission);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create permission: {Message} | TraceId: {TraceId}",
                ex.Message, TraceId);
            return BusinessError(ex.Message, ResponseCodes.DUPLICATE_PERMISSION_CODE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission | TraceId: {TraceId}", TraceId);
            return InternalError();
        }
    }

    /// <summary>
    /// Get a specific permission by ID
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Permission details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermission([FromRoute] Guid id)
    {
        try
        {
            var permission = await _permissionService.GetPermissionByIdAsync(id);
            if (permission == null)
            {
                return NotFound("Permission not found", ResponseCodes.PERMISSION_NOT_FOUND);
            }

            return Success(permission, "Query successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query permission: {Id} | TraceId: {TraceId}", id, TraceId);
            return InternalError();
        }
    }

    /// <summary>
    /// Update an existing permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="request">Update permission request</param>
    /// <returns>Updated permission details</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePermission([FromRoute] Guid id, [FromBody] UpdatePermissionRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return UnauthorizedResponse();
            }

            var permission = await _permissionService.UpdatePermissionAsync(id, request, userIdGuid);
            return Success(permission, "Update successful");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Permission not found: {Id} | TraceId: {TraceId}", id, TraceId);
            return NotFound(ex.Message, ResponseCodes.PERMISSION_NOT_FOUND);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update permission: {Message} | TraceId: {TraceId}",
                ex.Message, TraceId);
            return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {Id} | TraceId: {TraceId}", id, TraceId);
            return InternalError();
        }
    }

    /// <summary>
    /// Delete (soft delete) a permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="request">Delete permission request with version</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeletePermission([FromRoute] Guid id, [FromBody] DeletePermissionRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return UnauthorizedResponse();
            }

            await _permissionService.DeletePermissionAsync(id, request, userIdGuid);
            return Success("Permission deleted successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Permission not found: {Id} | TraceId: {TraceId}", id, TraceId);
            return NotFound(ex.Message, ResponseCodes.PERMISSION_NOT_FOUND);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete permission: {Message} | TraceId: {TraceId}",
                ex.Message, TraceId);

            if (ex.Message.Contains("version mismatch", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
            }

            return BusinessError(ex.Message, ResponseCodes.PERMISSION_IN_USE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission: {Id} | TraceId: {TraceId}", id, TraceId);
            return InternalError();
        }
    }

    /// <summary>
    /// 驗證用戶是否擁有特定權限
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(PermissionValidationResponse), StatusCodes.Status200OK)]
    public IActionResult ValidatePermission([FromBody] ValidatePermissionRequest request)
    {
        try
        {
            string? userIdClaim = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return UnauthorizedResponse();
            }

            // 需注入 IPermissionValidationService
            // 此端點由於架構考量，在 PermissionController 無法直接訪問驗證服務
            // 應在專用端點 (如 AuthController) 或透過依賴注入實現
            // 暫時返回未實現狀態
            return Ok(new PermissionValidationResponse(false, "此端點需要專用實現", ResponseCodes.SUCCESS));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "驗證權限失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("驗證權限失敗");
        }
    }
}
