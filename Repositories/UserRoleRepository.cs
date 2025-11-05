using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 用戶角色倉儲實現
/// 使用 Dapper 進行資料庫訪問
/// </summary>
public class UserRoleRepository : IUserRoleRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<UserRoleRepository> _logger;

    /// <summary>
    /// 初始化 UserRoleRepository
    /// </summary>
    public UserRoleRepository(IDbConnection dbConnection, ILogger<UserRoleRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    /// <summary>
    /// 為用戶指派角色（防止重複）
    /// </summary>
    public async Task<int> AssignRolesAsync(
        Guid userId,
        List<Guid> roleIds,
        Guid assignedBy,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            INSERT INTO user_roles (id, user_id, role_id, assigned_by, assigned_at, is_deleted)
            VALUES (@Id, @UserId, @RoleId, @AssignedBy, @AssignedAt, false)
            ON CONFLICT (user_id, role_id) WHERE is_deleted = false DO NOTHING;
        ";

        int count = 0;
        foreach (Guid roleId in roleIds)
        {
            try
            {
                int result = await _dbConnection.ExecuteAsync(
                    sql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        AssignedBy = assignedBy,
                        AssignedAt = DateTime.UtcNow,
                    }
                );

                if (result > 0)
                {
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "為用戶指派角色失敗: UserId={UserId}, RoleId={RoleId}",
                    userId,
                    roleId
                );
            }
        }

        _logger.LogInformation("為用戶指派了 {Count} 個角色: {UserId}", count, userId);
        return count;
    }

    /// <summary>
    /// 移除用戶的特定角色（軟刪除）
    /// </summary>
    public async Task<bool> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        Guid deletedBy,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            UPDATE user_roles 
            SET is_deleted = true, deleted_at = @DeletedAt, deleted_by = @DeletedBy
            WHERE user_id = @UserId AND role_id = @RoleId AND is_deleted = false
            RETURNING 1;
        ";

        try
        {
            int? result = await _dbConnection.QuerySingleOrDefaultAsync<int?>(
                sql,
                new
                {
                    UserId = userId,
                    RoleId = roleId,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = deletedBy,
                }
            );

            bool success = result.HasValue;
            if (success)
            {
                _logger.LogInformation(
                    "用戶角色已移除: UserId={UserId}, RoleId={RoleId}",
                    userId,
                    roleId
                );
            }
            else
            {
                _logger.LogWarning(
                    "用戶角色移除失敗（不存在或已刪除）: UserId={UserId}, RoleId={RoleId}",
                    userId,
                    roleId
                );
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "移除用戶角色失敗: UserId={UserId}, RoleId={RoleId}",
                userId,
                roleId
            );
            throw;
        }
    }

    /// <summary>
    /// 查詢用戶的所有角色（包含角色名稱）
    /// </summary>
    public async Task<List<UserRole>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            SELECT ur.id, ur.user_id, ur.role_id, ur.assigned_by, ur.assigned_at, 
                   ur.is_deleted, ur.deleted_at, ur.deleted_by
            FROM user_roles ur
            WHERE ur.user_id = @UserId AND ur.is_deleted = false
            ORDER BY ur.assigned_at DESC;
        ";

        try
        {
            var roles = (
                await _dbConnection.QueryAsync<UserRole>(sql, new { UserId = userId })
            ).ToList();

            _logger.LogInformation(
                "查詢用戶角色成功: UserId={UserId}, Count={Count}",
                userId,
                roles.Count
            );
            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢用戶角色失敗: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 檢查用戶是否擁有特定角色
    /// </summary>
    public async Task<bool> HasRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            SELECT COUNT(1) FROM user_roles
            WHERE user_id = @UserId AND role_id = @RoleId AND is_deleted = false;
        ";

        try
        {
            int count = await _dbConnection.QuerySingleAsync<int>(
                sql,
                new { UserId = userId, RoleId = roleId }
            );

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "檢查用戶角色失敗: UserId={UserId}, RoleId={RoleId}",
                userId,
                roleId
            );
            throw;
        }
    }

    /// <summary>
    /// 清除用戶的所有角色（軟刪除）
    /// </summary>
    public async Task<int> ClearUserRolesAsync(
        Guid userId,
        Guid deletedBy,
        CancellationToken cancellationToken = default
    )
    {
        const string sql =
            @"
            UPDATE user_roles 
            SET is_deleted = true, deleted_at = @DeletedAt, deleted_by = @DeletedBy
            WHERE user_id = @UserId AND is_deleted = false;
        ";

        try
        {
            int result = await _dbConnection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = deletedBy,
                }
            );

            if (result > 0)
            {
                _logger.LogInformation(
                    "清除用戶所有角色: UserId={UserId}, Count={Count}",
                    userId,
                    result
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除用戶角色失敗: UserId={UserId}", userId);
            throw;
        }
    }
}
