using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 權限資料存取實作
/// </summary>
public class PermissionRepository : IPermissionRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<PermissionRepository> _logger;

    public PermissionRepository(IDbConnection dbConnection, ILogger<PermissionRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<Permission> CreateAsync(Permission permission)
    {
        const string sql = @"
            INSERT INTO permissions (id, permission_code, name, description, permission_type, route_path, created_at, created_by, version)
            VALUES (@Id, @PermissionCode, @Name, @Description, @PermissionType, @RoutePath, @CreatedAt, @CreatedBy, 1)
            RETURNING *;
        ";

        try
        {
            var result = await _dbConnection.QuerySingleAsync<Permission>(sql, permission);
            _logger.LogInformation("權限已建立: {PermissionCode} ({Id})", permission.PermissionCode, permission.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立權限失敗: {PermissionCode}", permission.PermissionCode);
            throw;
        }
    }

    public async Task<Permission?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT * FROM permissions 
            WHERE id = @Id AND is_deleted = false;
        ";

        return await _dbConnection.QuerySingleOrDefaultAsync<Permission?>(sql, new { Id = id });
    }

    public async Task<(List<Permission> Items, int TotalCount)> GetAllAsync(
        int pageNumber, 
        int pageSize, 
        string? searchKeyword = null, 
        string? permissionType = null)
    {
        // 建立基礎查詢
        var whereClauses = new List<string> { "is_deleted = false" };
        var parameters = new DynamicParameters();
        parameters.Add("@Offset", (pageNumber - 1) * pageSize);
        parameters.Add("@PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            whereClauses.Add("(permission_code ILIKE @SearchKeyword OR name ILIKE @SearchKeyword)");
            parameters.Add("@SearchKeyword", $"%{searchKeyword}%");
        }

        if (!string.IsNullOrWhiteSpace(permissionType))
        {
            whereClauses.Add("permission_type = @PermissionType");
            parameters.Add("@PermissionType", permissionType);
        }

        var whereClause = string.Join(" AND ", whereClauses);

        // 取得總筆數
        var countSql = $"SELECT COUNT(*) FROM permissions WHERE {whereClause};";
        var totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);

        // 取得分頁資料
        var dataSql = $@"
            SELECT * FROM permissions 
            WHERE {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset;
        ";

        var items = (await _dbConnection.QueryAsync<Permission>(dataSql, parameters)).ToList();

        return (items, totalCount);
    }

    public async Task<bool> UpdateAsync(Permission permission)
    {
        const string sql = @"
            UPDATE permissions 
            SET name = @Name, 
                description = @Description, 
                route_path = @RoutePath, 
                updated_at = @UpdatedAt, 
                updated_by = @UpdatedBy, 
                version = version + 1
            WHERE id = @Id AND version = @Version AND is_deleted = false
            RETURNING *;
        ";

        try
        {
            var result = await _dbConnection.QuerySingleOrDefaultAsync<Permission?>(sql, permission);
            if (result == null)
            {
                _logger.LogWarning("權限更新失敗（版本不符或不存在）: {Id}", permission.Id);
                return false;
            }

            _logger.LogInformation("權限已更新: {Id}", permission.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新權限時發生錯誤: {Id}", permission.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
    {
        const string sql = @"
            UPDATE permissions 
            SET is_deleted = true, deleted_at = @DeletedAt, deleted_by = @DeletedBy
            WHERE id = @Id AND is_deleted = false;
        ";

        try
        {
            var result = await _dbConnection.ExecuteAsync(sql, new 
            { 
                Id = id, 
                DeletedAt = DateTime.UtcNow, 
                DeletedBy = deletedBy 
            });

            if (result > 0)
            {
                _logger.LogInformation("權限已刪除: {Id}", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除權限時發生錯誤: {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM permissions WHERE id = @Id AND is_deleted = false);";
        return await _dbConnection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    public async Task<bool> IsInUseAsync(Guid id)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM role_permissions 
                WHERE permission_id = @Id
            );
        ";
        return await _dbConnection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null)
    {
        string sql = "SELECT EXISTS(SELECT 1 FROM permissions WHERE permission_code = @Code AND is_deleted = false";
        if (excludeId.HasValue)
        {
            sql += " AND id != @ExcludeId";
        }
        sql += ");";

        var parameters = new DynamicParameters();
        parameters.Add("@Code", code);
        if (excludeId.HasValue)
        {
            parameters.Add("@ExcludeId", excludeId.Value);
        }

        var exists = await _dbConnection.ExecuteScalarAsync<bool>(sql, parameters);
        return !exists; // 返回 true 表示唯一
    }
}
