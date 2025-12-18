using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;
using System.Text.RegularExpressions;

namespace V3.Admin.Backend.Tests.Helpers;

/// <summary>
/// 資料庫測試基礎設施，使用 Testcontainers 提供 PostgreSQL 測試環境
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // 執行資料庫遷移腳本
        await ExecuteMigrationScripts();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    private async Task ExecuteMigrationScripts()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        foreach (var migration in EnumerateMigrationFiles())
        {
            string sql = await File.ReadAllTextAsync(migration.FullPath);

            try
            {
                await using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"執行 migration 失敗: {migration.FileName}",
                    ex
                );
            }
        }

        // Seed integration test permissions and admin role, and auto-assign role to test users
        var seedPermissionsSql = @"
            -- 預先建立整合測試需要的權限代碼 (符合 migration 013 後的 permission_type 約束: function/view)
            INSERT INTO permissions (permission_code, name, description, permission_type, created_at, updated_at, is_deleted, version)
            SELECT v.permission_code, v.name, v.description, 'function', NOW(), NOW(), false, 1
            FROM (VALUES
                ('permission.read', 'Permission Read', 'Read permissions'),
                ('permission.create', 'Permission Create', 'Create permissions'),
                ('permission.update', 'Permission Update', 'Update permissions'),
                ('permission.delete', 'Permission Delete', 'Delete permissions'),
                ('permission.assign', 'Permission Assign', 'Assign permissions to roles'),
                ('permission.remove', 'Permission Remove', 'Remove permissions from roles'),

                ('role.read', 'Role Read', 'Read roles'),
                ('role.create', 'Role Create', 'Create roles'),
                ('role.update', 'Role Update', 'Update roles'),
                ('role.delete', 'Role Delete', 'Delete roles'),
                ('role.assign', 'Role Assign', 'Assign roles to users'),
                ('role.remove', 'Role Remove', 'Remove roles from users'),

                ('user.profile.read', 'User Profile Read', 'Read user profile'),

                ('customer.read', 'Customer Read', 'Read customers'),
                ('customer.create', 'Customer Create', 'Create customers'),

                ('serviceOrder.buyback.create', 'Service Order Buyback Create', 'Create buyback service order')
            ) AS v(permission_code, name, description)
            WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.permission_code = v.permission_code AND p.is_deleted = FALSE);

            -- 建立一個整合測試專用的 role: integration-admin
            INSERT INTO roles (role_name, description, created_at, is_deleted, version)
            SELECT 'integration-admin', 'Role with all permissions for integration tests', NOW(), false, 1
            WHERE NOT EXISTS (SELECT 1 FROM roles r WHERE r.role_name = 'integration-admin' AND r.is_deleted = FALSE);

            -- 將所有已存在的 permission 指派給 integration-admin
            INSERT INTO role_permissions (role_id, permission_id, assigned_by)
            SELECT r.id, p.id, NULL
            FROM roles r, permissions p
            WHERE r.role_name = 'integration-admin' AND p.is_deleted = FALSE
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
                );

            -- 當插入使用者且 username 結尾為 '_test_user' 時，自動指派 integration-admin 角色
            CREATE OR REPLACE FUNCTION assign_integration_admin_role()
            RETURNS trigger AS $func$
            DECLARE
                v_role_id UUID;
            BEGIN
                IF NEW.username LIKE '%_test_user' THEN
                    SELECT id INTO v_role_id FROM roles WHERE role_name = 'integration-admin' AND is_deleted = FALSE LIMIT 1;
                    IF v_role_id IS NOT NULL THEN
                        INSERT INTO user_roles (user_id, role_id, assigned_by, assigned_at)
                        VALUES (NEW.id, v_role_id, NULL, NOW())
                        ON CONFLICT (user_id, role_id) DO NOTHING;
                    END IF;
                END IF;
                RETURN NEW;
            END;
            $func$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS assign_integration_admin_role_trigger ON users;
            CREATE TRIGGER assign_integration_admin_role_trigger
            AFTER INSERT ON users
            FOR EACH ROW
            EXECUTE FUNCTION assign_integration_admin_role();
        ";

        await using var seedCmd = new NpgsqlCommand(seedPermissionsSql, connection);
        await seedCmd.ExecuteNonQueryAsync();
    }

    private static IEnumerable<(string FileName, string FullPath)> EnumerateMigrationFiles()
    {
        string migrationsDir = FindMigrationsDirectory();
        var regex = new Regex("^\\d{3}_.*\\.sql$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 既有 migration 009 已涵蓋 role_permissions/user_roles 的 assigned_by 外鍵；
        // migration 012 會重複新增同名 constraint，導致全新資料庫套用時失敗。
        // 整合測試目前不依賴 012 的額外外鍵，因此在 fixture 中略過。
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "012_AddUserTrackingForeignKeys.sql",
        };

        return Directory
            .EnumerateFiles(migrationsDir, "*.sql", SearchOption.TopDirectoryOnly)
            .Select(path => new { Path = path, Name = Path.GetFileName(path) })
            .Where(x => x.Name != null && regex.IsMatch(x.Name))
                .Where(x => x.Name != null && !excluded.Contains(x.Name))
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => (x.Name!, x.Path));
    }

    private static string FindMigrationsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Database", "Migrations");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "找不到 Database/Migrations 目錄，請確認測試執行目錄位於 repo 內或調整 FindMigrationsDirectory() 搜尋邏輯。"
        );
    }
}
