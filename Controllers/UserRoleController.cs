using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Middleware;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 用戶角色管理 API 控制器
/// </summary>
[ApiController]
[Route("api/users/{userId}/roles")]
[Authorize]
public class UserRoleController : BaseApiController
{
    private readonly IUserRoleService _userRoleService;
    private readonly IPermissionValidationService _permissionValidationService;
    private readonly ILogger<UserRoleController> _logger;

    /// <summary>
    /// 初始化 UserRoleController
    /// </summary>
    public UserRoleController(
        IUserRoleService userRoleService,
        IPermissionValidationService permissionValidationService,
        ILogger<UserRoleController> logger
    )
    {
        _userRoleService = userRoleService;
        _permissionValidationService = permissionValidationService;
        _logger = logger;
    }

    /// <summary>
    /// 查詢用戶的所有角色
    /// </summary>
    [HttpGet]
    [RequirePermission("role.read")]
    [ProducesResponseType(typeof(UserRoleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        try
        {
            List<Models.Dtos.UserRoleDto> rolesDto = await _userRoleService.GetUserRolesAsync(
                userId
            );
            var response = new UserRoleResponse(rolesDto);
            return Success(response, "查詢成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("用戶不存在 | TraceId: {TraceId}", TraceId);
            return NotFound(ex.Message, ResponseCodes.USER_NOT_FOUND);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢用戶角色失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("查詢用戶角色失敗");
        }
    }

    /// <summary>
    /// 為用戶指派角色
    /// </summary>
    [HttpPost]
    [RequirePermission("role.assign")]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRoles(
        Guid userId,
        [FromBody] AssignUserRoleRequest request
    )
    {
        try
        {
            var operatorId = GetUserId();
            if (operatorId is null)
            {
                return UnauthorizedResponse();
            }

            int count = await _userRoleService.AssignRolesAsync(userId, request, operatorId.Value);
            return Success($"成功為用戶指派 {count} 個角色");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("角色分配驗證失敗 | TraceId: {TraceId}", TraceId);
            return ValidationError(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("用戶或角色不存在 | TraceId: {TraceId}", TraceId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "指派用戶角色失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("指派用戶角色失敗");
        }
    }

    /// <summary>
    /// 移除用戶的特定角色
    /// </summary>
    [HttpDelete("{roleId}")]
    [RequirePermission("role.remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
    {
        try
        {
            var operatorId = GetUserId();
            if (operatorId is null)
            {
                return UnauthorizedResponse();
            }

            RemoveUserRoleRequest request = new RemoveUserRoleRequest { RoleId = roleId };
            bool success = await _userRoleService.RemoveRoleAsync(
                userId,
                request,
                operatorId.Value
            );

            if (!success)
            {
                return NotFound("用戶或角色不存在");
            }

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("用戶或角色不存在 | TraceId: {TraceId}", TraceId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除用戶角色失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("移除用戶角色失敗");
        }
    }

    /// <summary>
    /// 查詢用戶的所有有效權限（多角色合併）
    /// </summary>
    [HttpGet("permissions")]
    [RequirePermission("permission.read")]
    [ProducesResponseType(typeof(UserEffectivePermissionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserEffectivePermissions(Guid userId)
    {
        try
        {
            // 查詢用戶有效權限，Service DTO 轉換為 Response DTO
            var effectivePermissionsDto =
                await _permissionValidationService.GetUserEffectivePermissionsAsync(userId);
            var responseDto = new Models.Responses.UserEffectivePermissionsResponse
            {
                UserId = effectivePermissionsDto.UserId,
                Permissions = effectivePermissionsDto
                    .Permissions.Select(p => new Models.Responses.PermissionResponse
                    {
                        Id = p.Id,
                        PermissionCode = p.PermissionCode,
                        Name = p.Name,
                        Description = p.Description,
                        PermissionType = p.PermissionType,
                    })
                    .ToList(),
            };
            return Success(responseDto, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢用戶有效權限失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("查詢用戶有效權限失敗");
        }
    }
}
