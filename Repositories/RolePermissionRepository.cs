using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 角色權限關聯資料存取實作
/// </summary>
public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<RolePermissionRepository> _logger;

    /// <summary>
    /// 初始化 RolePermissionRepository
    /// </summary>
    /// <param name="dbConnection">資料庫連接</param>
    /// <param name="logger">日誌記錄器</param>
    public RolePermissionRepository(IDbConnection dbConnection, ILogger<RolePermissionRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<int> AssignPermissionsAsync(Guid roleId, List<Guid> permissionIds, Guid? assignedBy = null, CancellationToken cancellationToken = default)
    {
        if (!permissionIds.Any())
        {
            return 0;
        }

        const string sql = @"
            INSERT INTO role_permissions (id, role_id, permission_id, assigned_at, assigned_by)
            VALUES (@Id, @RoleId, @PermissionId, @AssignedAt, @AssignedBy)
            ON CONFLICT (role_id, permission_id) DO NOTHING;
        ";

        try
        {
            var count = 0;
            foreach (var permissionId in permissionIds)
            {
                var result = await _dbConnection.ExecuteAsync(sql, new
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = permissionId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = assignedBy
                });

                count += result;
            }

            _logger.LogInformation("為角色分配了 {Count} 個權限: {RoleId}", count, roleId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配角色權限失敗: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM role_permissions 
            WHERE role_id = @RoleId AND permission_id = @PermissionId;
        ";

        try
        {
            var result = await _dbConnection.ExecuteAsync(sql, new
            {
                RoleId = roleId,
                PermissionId = permissionId
            });

            if (result > 0)
            {
                _logger.LogInformation("已移除角色權限: {RoleId} - {PermissionId}", roleId, permissionId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除角色權限失敗: {RoleId} - {PermissionId}", roleId, permissionId);
            throw;
        }
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT p.* FROM permissions p
            INNER JOIN role_permissions rp ON p.id = rp.permission_id
            WHERE rp.role_id = @RoleId AND p.is_deleted = false
            ORDER BY p.permission_type, p.permission_code;
        ";

        try
        {
            var permissions = (await _dbConnection.QueryAsync<Permission>(sql, new { RoleId = roleId })).ToList();
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得角色權限失敗: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<int> ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM role_permissions 
            WHERE role_id = @RoleId;
        ";

        try
        {
            var result = await _dbConnection.ExecuteAsync(sql, new { RoleId = roleId });
            _logger.LogInformation("已清除角色的所有權限: {RoleId} (刪除 {Count} 筆)", roleId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除角色權限失敗: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM role_permissions 
                WHERE role_id = @RoleId AND permission_id = @PermissionId
            );
        ";

        return await _dbConnection.QuerySingleAsync<bool>(sql, new
        {
            RoleId = roleId,
            PermissionId = permissionId
        });
    }
}
