using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

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

        // 建立必要的表格：users / roles / permissions / user_roles / role_permissions
        // 注意：擴大 username 長度，並為權限/角色使用合理的欄位長度，避免測試資料被截斷
        var createTableSql = @"
            -- 必要的擴充套件（若尚未安裝）
            CREATE EXTENSION IF NOT EXISTS pgcrypto;

            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                username VARCHAR(100) NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                display_name VARCHAR(200) NOT NULL,
                version INTEGER NOT NULL DEFAULT 1,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                deleted_at TIMESTAMP WITH TIME ZONE,
                deleted_by UUID
            );

            CREATE INDEX IF NOT EXISTS idx_users_username ON users(username) WHERE is_deleted = FALSE;
            CREATE INDEX IF NOT EXISTS idx_users_is_deleted ON users(is_deleted);

            CREATE TABLE IF NOT EXISTS roles (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                role_name VARCHAR(200) NOT NULL,
                description TEXT,
                version INTEGER NOT NULL DEFAULT 1,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                deleted_at TIMESTAMP WITH TIME ZONE,
                created_by UUID,
                updated_by UUID,
                deleted_by UUID
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_roles_role_name ON roles(role_name) WHERE is_deleted = FALSE;

            CREATE TABLE IF NOT EXISTS permissions (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                permission_code VARCHAR(200) NOT NULL,
                name VARCHAR(200) NOT NULL,
                description TEXT,
                permission_type VARCHAR(50),
                version INTEGER NOT NULL DEFAULT 1,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                deleted_at TIMESTAMP WITH TIME ZONE,
                created_by UUID,
                updated_by UUID,
                deleted_by UUID
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_permissions_code ON permissions(permission_code) WHERE is_deleted = FALSE;

            CREATE TABLE IF NOT EXISTS role_permissions (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                role_id UUID NOT NULL,
                permission_id UUID NOT NULL,
                assigned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                assigned_by UUID,
                version INTEGER NOT NULL DEFAULT 1,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                CONSTRAINT fk_rp_role FOREIGN KEY(role_id) REFERENCES roles(id),
                CONSTRAINT fk_rp_permission FOREIGN KEY(permission_id) REFERENCES permissions(id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_role_permissions_role_permission ON role_permissions(role_id, permission_id) WHERE is_deleted = FALSE;

            CREATE TABLE IF NOT EXISTS user_roles (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id UUID NOT NULL,
                role_id UUID NOT NULL,
                assigned_by UUID,
                assigned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                version INTEGER NOT NULL DEFAULT 1,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                deleted_at TIMESTAMP WITH TIME ZONE,
                deleted_by UUID,
                CONSTRAINT fk_ur_user FOREIGN KEY(user_id) REFERENCES users(id),
                CONSTRAINT fk_ur_role FOREIGN KEY(role_id) REFERENCES roles(id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_user_roles_user_role ON user_roles(user_id, role_id) WHERE is_deleted = FALSE;

            CREATE TABLE IF NOT EXISTS permission_failure_logs (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id UUID,
                username VARCHAR(200),
                attempted_resource TEXT NOT NULL,
                failure_reason TEXT NOT NULL,
                attempted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                ip_address VARCHAR(100),
                user_agent TEXT,
                trace_id VARCHAR(200)
            );

            -- 可擴充：其他測試所需的表格（如 audit_logs、permissions 欄位索引等）
        ";

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();

        // Seed integration test permissions and admin role, and auto-assign role to test users
        var seedPermissionsSql = @"
            -- 預先建立整合測試需要的權限代碼
            INSERT INTO permissions (permission_code, name, description, permission_type, version, is_deleted, created_at, updated_at)
            SELECT v.permission_code, v.name, v.description, 'function', 1, false, NOW(), NOW()
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
                ('user.profile.read', 'User Profile Read', 'Read user profile')
            ) AS v(permission_code, name, description)
            WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.permission_code = v.permission_code AND p.is_deleted = FALSE);

            -- 建立一個整合測試專用的 role: integration-admin
            INSERT INTO roles (role_name, description, version, is_deleted, created_at, updated_at)
            SELECT 'integration-admin', 'Role with all permissions for integration tests', 1, false, NOW(), NOW()
            WHERE NOT EXISTS (SELECT 1 FROM roles r WHERE r.role_name = 'integration-admin' AND r.is_deleted = FALSE);

            -- 將所有已存在的 permission 指派給 integration-admin
            INSERT INTO role_permissions (role_id, permission_id, assigned_by)
            SELECT r.id, p.id, NULL
            FROM roles r, permissions p
            WHERE r.role_name = 'integration-admin' AND p.is_deleted = FALSE
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id AND rp.is_deleted = FALSE
                );

            -- 建立一個觸發器：當插入使用者且 username 結尾為 '_test_user' 時，自動指派 integration-admin 角色
            CREATE OR REPLACE FUNCTION assign_integration_admin_role()
            RETURNS trigger AS $func$
            DECLARE
                v_role_id UUID;
            BEGIN
                IF NEW.username LIKE '%_test_user' THEN
                    SELECT id INTO v_role_id FROM roles WHERE role_name = 'integration-admin' AND is_deleted = FALSE LIMIT 1;
                    IF v_role_id IS NOT NULL THEN
                        INSERT INTO user_roles (user_id, role_id, assigned_by, assigned_at)
                        SELECT NEW.id, v_role_id, NULL, NOW()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM user_roles ur WHERE ur.user_id = NEW.id AND ur.role_id = v_role_id AND ur.is_deleted = FALSE
                        );
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

        await using var seedCmd = new Npgsql.NpgsqlCommand(seedPermissionsSql, connection);
        await seedCmd.ExecuteNonQueryAsync();
    }
}
