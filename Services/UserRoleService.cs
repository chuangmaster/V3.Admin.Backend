using FluentValidation;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;
using V3.Admin.Backend.Validators;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 用戶角色服務實現
/// </summary>
public class UserRoleService : IUserRoleService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserRoleService> _logger;
    private readonly AssignUserRoleRequestValidator _assignValidator;

    /// <summary>
    /// 初始化 UserRoleService
    /// </summary>
    public UserRoleService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        ILogger<UserRoleService> logger,
        AssignUserRoleRequestValidator assignValidator
    )
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _logger = logger;
        _assignValidator = assignValidator;
    }

    /// <summary>
    /// 為用戶指派角色
    /// </summary>
    public async Task<int> AssignRolesAsync(
        Guid userId,
        AssignUserRoleRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        // 驗證請求
        var validationResult = await _assignValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 檢查用戶是否存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"用戶 {userId} 不存在");
        }

        // 檢查所有角色是否存在
        foreach (var roleId in request.RoleIds)
        {
            var roleExists = await _roleRepository.ExistsAsync(roleId, cancellationToken);
            if (!roleExists)
            {
                throw new KeyNotFoundException($"角色 {roleId} 不存在");
            }
        }

        // 指派角色
        int count = await _userRoleRepository.AssignRolesAsync(
            userId,
            request.RoleIds,
            operatorId,
            cancellationToken
        );
        _logger.LogInformation("為用戶指派了 {Count} 個角色: {UserId}", count, userId);

        // TODO: 記錄稽核日誌（T097 中實作）

        return count;
    }

    /// <summary>
    /// 移除用戶的特定角色
    /// </summary>
    public async Task<bool> RemoveRoleAsync(
        Guid userId,
        RemoveUserRoleRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        // 檢查用戶是否存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"用戶 {userId} 不存在");
        }

        // 檢查角色是否存在
        var roleExists = await _roleRepository.ExistsAsync(request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new KeyNotFoundException($"角色 {request.RoleId} 不存在");
        }

        // 移除角色
        bool success = await _userRoleRepository.RemoveRoleAsync(
            userId,
            request.RoleId,
            operatorId,
            cancellationToken
        );

        // TODO: 記錄稽核日誌（T097 中實作）

        return success;
    }

    /// <summary>
    /// 查詢用戶的所有角色
    /// </summary>
    public async Task<List<UserRoleDto>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        // 檢查用戶是否存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"用戶 {userId} 不存在");
        }

        // 查詢用戶角色
        var userRoles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);

        // 轉換為 DTO
        var roleDtos = new List<UserRoleDto>();
        foreach (var userRole in userRoles)
        {
            var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
            if (role != null)
            {
                roleDtos.Add(
                    new UserRoleDto
                    {
                        Id = userRole.Id,
                        UserId = userRole.UserId,
                        RoleId = userRole.RoleId,
                        RoleName = role.RoleName,
                        AssignedAt = userRole.AssignedAt,
                    }
                );
            }
        }

        return roleDtos;
    }
}
