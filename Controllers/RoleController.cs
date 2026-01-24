using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Middleware;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 角色管理 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoleController : BaseApiController
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleController> _logger;

    /// <summary>
    /// 初始化 RoleController
    /// </summary>
    public RoleController(IRoleService roleService, ILogger<RoleController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// 取得角色列表（分頁）
    /// </summary>
    [HttpGet]
    [RequirePermission("role.read")]
    [ProducesResponseType(typeof(PagedApiResponseModel<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20
    )
    {
        try
        {
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 20;

            (List<Models.Dtos.RoleDto> roles, int totalCount) = await _roleService.GetRolesAsync(
                pageNumber,
                pageSize
            );

            var items =
                roles != null
                    ? roles
                        .Select(r => new RoleResponse
                        {
                            Id = r.Id,
                            RoleName = r.RoleName,
                            Description = r.Description,
                            CreatedAt = r.CreatedAt,
                            Version = r.Version,
                        })
                        .ToList()
                    : new List<RoleResponse>();

            return PagedSuccess(items, pageNumber, pageSize, totalCount, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢角色列表失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("查詢角色列表失敗");
        }
    }

    /// <summary>
    /// 建立新角色
    /// </summary>
    [HttpPost]
    [RequirePermission("role.create")]
    [ProducesResponseType(typeof(ApiResponseModel<RoleResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
                return UnauthorizedResponse();

            Models.Dtos.RoleDto roleDto = await _roleService.CreateRoleAsync(request, userId.Value);
            var response = new RoleResponse
            {
                Id = roleDto.Id,
                RoleName = roleDto.RoleName,
                Description = roleDto.Description,
                CreatedAt = roleDto.CreatedAt,
                Version = roleDto.Version,
            };
            return Created(response, "角色建立成功");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("角色建立驗證失敗 | TraceId: {TraceId}", TraceId);
            return ValidationError(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("角色建立失敗：{Message} | TraceId: {TraceId}", ex.Message, TraceId);
            return BusinessError(ex.Message, ResponseCodes.DUPLICATE_ROLE_NAME);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立角色失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("建立角色失敗");
        }
    }

    /// <summary>
    /// 根據 ID 取得角色
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseModel<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleById(Guid id)
    {
        try
        {
            Models.Dtos.RoleDto? roleDto = await _roleService.GetRoleByIdAsync(id);
            if (roleDto == null)
                return NotFound("角色不存在", ResponseCodes.ROLE_NOT_FOUND);

            var response = new RoleResponse
            {
                Id = roleDto.Id,
                RoleName = roleDto.RoleName,
                Description = roleDto.Description,
                CreatedAt = roleDto.CreatedAt,
                Version = roleDto.Version,
            };
            return Success(response, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得角色失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("取得角色失敗");
        }
    }

    /// <summary>
    /// 取得角色詳細資訊（包含權限）
    /// </summary>
    [HttpGet("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponseModel<RoleDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleDetail(Guid id)
    {
        try
        {
            Models.Dtos.RoleDetailDto? roleDetailDto = await _roleService.GetRoleDetailAsync(id);
            if (roleDetailDto == null)
                return NotFound("角色不存在", ResponseCodes.ROLE_NOT_FOUND);

            var responseData = new RoleDetailResponse
            {
                Id = roleDetailDto.Id,
                RoleName = roleDetailDto.RoleName,
                Description = roleDetailDto.Description,
                CreatedAt = roleDetailDto.CreatedAt,
                Version = roleDetailDto.Version,
                Permissions =
                    roleDetailDto
                        .Permissions?.Select(p => new PermissionResponse
                        {
                            Id = p.Id,
                            PermissionCode = p.PermissionCode,
                            Name = p.Name,
                            Description = p.Description,
                            PermissionType = p.PermissionType,
                            CreatedAt = p.CreatedAt,
                            Version = p.Version,
                        })
                        .ToList() ?? new List<PermissionResponse>(),
            };

            return Success(responseData, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得角色詳細資訊失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("取得角色詳細資訊失敗");
        }
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("role.update")]
    [ProducesResponseType(typeof(ApiResponseModel<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
                return UnauthorizedResponse();

            Models.Dtos.RoleDto roleDto = await _roleService.UpdateRoleAsync(
                id,
                request,
                userId.Value
            );

            var response = new RoleResponse
            {
                Id = roleDto.Id,
                RoleName = roleDto.RoleName,
                Description = roleDto.Description,
                CreatedAt = roleDto.CreatedAt,
                Version = roleDto.Version,
            };

            return Success(response, "角色更新成功");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("角色更新驗證失敗 | TraceId: {TraceId}", TraceId);
            return ValidationError(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("角色不存在 | TraceId: {TraceId}", TraceId);
            return NotFound(ex.Message, ResponseCodes.ROLE_NOT_FOUND);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("角色更新失敗：{Message} | TraceId: {TraceId}", ex.Message, TraceId);
            if (ex.Message.Contains("版本衝突"))
                return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
            return BusinessError(ex.Message, ResponseCodes.DUPLICATE_ROLE_NAME);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新角色失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("更新角色失敗");
        }
    }

    /// <summary>
    /// 刪除角色
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("role.delete")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRole(Guid id, [FromBody] DeleteRoleRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
                return UnauthorizedResponse();

            bool success = await _roleService.DeleteRoleAsync(id, request, userId.Value);
            if (!success)
                return NotFound("角色不存在", ResponseCodes.ROLE_NOT_FOUND);

            return Success("角色刪除成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("角色不存在 | TraceId: {TraceId}", TraceId);
            return NotFound(ex.Message, ResponseCodes.ROLE_NOT_FOUND);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("角色刪除失敗：{Message} | TraceId: {TraceId}", ex.Message, TraceId);
            if (ex.Message.Contains("版本衝突"))
                return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
            return BusinessError(ex.Message, ResponseCodes.ROLE_IN_USE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除角色失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("刪除角色失敗");
        }
    }

    /// <summary>
    /// 為角色分配權限
    /// </summary>
    [HttpPost("{roleId}/permissions")]
    [RequirePermission("permission.assign")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignPermissions(
        Guid roleId,
        [FromBody] AssignRolePermissionsRequest request
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
                return UnauthorizedResponse();

            bool success = await _roleService.AssignPermissionsAsync(roleId, request, userId.Value);
            if (!success)
                return NotFound("角色或權限不存在");

            ApiResponseModel response = ApiResponseModel.CreateSuccess(
                "角色權限分配成功",
                ResponseCodes.SUCCESS
            );
            response.TraceId = TraceId;

            return Ok(response);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("權限分配驗證失敗 | TraceId: {TraceId}", TraceId);
            return ValidationError(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("角色或權限不存在 | TraceId: {TraceId}", TraceId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配角色權限失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("分配角色權限失敗");
        }
    }

    /// <summary>
    /// 移除角色的特定權限
    /// </summary>
    [HttpDelete("{roleId}/permissions/{permissionId}")]
    [RequirePermission("permission.remove")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
                return UnauthorizedResponse();

            bool success = await _roleService.RemovePermissionAsync(
                roleId,
                permissionId,
                userId.Value
            );
            if (!success)
                return NotFound("角色或權限不存在");

            return Success("角色權限移除成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("角色或權限不存在 | TraceId: {TraceId}", TraceId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除角色權限失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("移除角色權限失敗");
        }
    }
}
