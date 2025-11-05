using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 角色資料存取實作
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<RoleRepository> _logger;

    /// <summary>
    /// 初始化 RoleRepository
    /// </summary>
    /// <param name="dbConnection">資料庫連接</param>
    /// <param name="logger">日誌記錄器</param>
    public RoleRepository(IDbConnection dbConnection, ILogger<RoleRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO roles (id, role_name, description, created_at, created_by, version)
            VALUES (@Id, @RoleName, @Description, @CreatedAt, @CreatedBy, 1)
            RETURNING *;
        ";

        try
        {
            var result = await _dbConnection.QuerySingleAsync<Role>(sql, role);
            _logger.LogInformation("角色已建立: {RoleName} ({Id})", role.RoleName, role.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立角色失敗: {RoleName}", role.RoleName);
            throw;
        }
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM roles 
            WHERE id = @Id AND is_deleted = false;
        ";

        return await _dbConnection.QuerySingleOrDefaultAsync<Role?>(sql, new { Id = id });
    }

    public async Task<(List<Role> roles, int totalCount)> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        const string countSql = @"
            SELECT COUNT(*) FROM roles 
            WHERE is_deleted = false;
        ";

        const string sql = @"
            SELECT * FROM roles 
            WHERE is_deleted = false
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset;
        ";

        try
        {
            var totalCount = await _dbConnection.QuerySingleAsync<int>(countSql);
            var offset = (pageNumber - 1) * pageSize;
            var roles = (await _dbConnection.QueryAsync<Role>(sql, new { PageSize = pageSize, Offset = offset })).ToList();

            return (roles, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得角色列表失敗");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE roles 
            SET role_name = @RoleName, description = @Description, updated_at = @UpdatedAt, updated_by = @UpdatedBy, version = version + 1
            WHERE id = @Id AND version = @Version AND is_deleted = false
            RETURNING 1;
        ";

        try
        {
            role.UpdatedAt = DateTime.UtcNow;
            var result = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sql, role);

            if (result != null)
            {
                _logger.LogInformation("角色已更新: {RoleName} ({Id})", role.RoleName, role.Id);
                return true;
            }

            _logger.LogWarning("角色更新失敗（版本衝突）: {Id}", role.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新角色失敗: {RoleName}", role.RoleName);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy, int version, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE roles 
            SET is_deleted = true, deleted_at = @DeletedAt, deleted_by = @DeletedBy
            WHERE id = @Id AND version = @Version AND is_deleted = false
            RETURNING 1;
        ";

        try
        {
            var result = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sql, new
            {
                Id = id,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = deletedBy,
                Version = version
            });

            if (result != null)
            {
                _logger.LogInformation("角色已刪除: {Id}", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除角色失敗: {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT EXISTS(SELECT 1 FROM roles WHERE id = @Id AND is_deleted = false);
        ";

        return await _dbConnection.QuerySingleAsync<bool>(sql, new { Id = id });
    }

    public async Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM user_roles 
                WHERE role_id = @Id AND is_deleted = false
            );
        ";

        return await _dbConnection.QuerySingleAsync<bool>(sql, new { Id = id });
    }

    public async Task<bool> RoleNameExistsAsync(string roleName, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT EXISTS(
                SELECT 1 FROM roles 
                WHERE role_name = @RoleName AND is_deleted = false";

        if (excludeId.HasValue)
        {
            sql += " AND id != @ExcludeId";
        }

        sql += ");";

        return await _dbConnection.QuerySingleAsync<bool>(sql, new
        {
            RoleName = roleName,
            ExcludeId = excludeId
        });
    }
}
