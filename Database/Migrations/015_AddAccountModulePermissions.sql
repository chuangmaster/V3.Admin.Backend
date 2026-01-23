-- 新增帳號模組相關權限
-- Migration: 015
-- Date: 2026-01-24
-- Description: Add account module permissions for account management and password operations
-- Feature: 007-account-refactor

-- 新增 user.profile.update 權限
-- 此權限允許用戶修改自己的密碼
INSERT INTO permissions (
    id,
    permission_code,
    name,
    description,
    permission_type,
    created_at,
    updated_at,
    created_by,
    updated_by,
    is_deleted,
    deleted_at,
    deleted_by,
    version
) VALUES (
    gen_random_uuid(),
    'user.profile.update',
    '修改個人資料',
    '允許用戶修改自己的個人資料，包括密碼變更',
    'function',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    false,
    NULL,
    NULL,
    1
)
ON CONFLICT (permission_code) DO NOTHING;

-- 新增 account.read 權限
-- 此權限允許查詢帳號列表和單一帳號詳情
INSERT INTO permissions (
    id,
    permission_code,
    name,
    description,
    permission_type,
    created_at,
    updated_at,
    created_by,
    updated_by,
    is_deleted,
    deleted_at,
    deleted_by,
    version
) VALUES (
    gen_random_uuid(),
    'account.read',
    '查詢帳號',
    '允許查詢帳號列表和單一帳號詳情',
    'function',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    false,
    NULL,
    NULL,
    1
)
ON CONFLICT (permission_code) DO NOTHING;

-- 新增 account.create 權限
-- 此權限允許建立新帳號
INSERT INTO permissions (
    id,
    permission_code,
    name,
    description,
    permission_type,
    created_at,
    updated_at,
    created_by,
    updated_by,
    is_deleted,
    deleted_at,
    deleted_by,
    version
) VALUES (
    gen_random_uuid(),
    'account.create',
    '新增帳號',
    '允許建立新的使用者帳號',
    'function',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    false,
    NULL,
    NULL,
    1
)
ON CONFLICT (permission_code) DO NOTHING;

-- 新增 account.update 權限
-- 此權限允許更新帳號資訊和重設密碼
INSERT INTO permissions (
    id,
    permission_code,
    name,
    description,
    permission_type,
    created_at,
    updated_at,
    created_by,
    updated_by,
    is_deleted,
    deleted_at,
    deleted_by,
    version
) VALUES (
    gen_random_uuid(),
    'account.update',
    '更新帳號',
    '允許更新帳號資訊和重設使用者密碼',
    'function',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    false,
    NULL,
    NULL,
    1
)
ON CONFLICT (permission_code) DO NOTHING;

-- 新增 account.delete 權限
-- 此權限允許刪除帳號
INSERT INTO permissions (
    id,
    permission_code,
    name,
    description,
    permission_type,
    created_at,
    updated_at,
    created_by,
    updated_by,
    is_deleted,
    deleted_at,
    deleted_by,
    version
) VALUES (
    gen_random_uuid(),
    'account.delete',
    '刪除帳號',
    '允許刪除使用者帳號（軟刪除）',
    'function',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    NULL,
    NULL,
    false,
    NULL,
    NULL,
    1
)
ON CONFLICT (permission_code) DO NOTHING;

-- 驗證權限建立並與 Admin 角色建立關聯
DO $$
DECLARE
    admin_role_id UUID;
    permission_record RECORD;
    permission_count INT := 0;
    assigned_count INT := 0;
BEGIN
    -- 查詢 Admin 角色 ID
    SELECT id INTO admin_role_id
    FROM roles
    WHERE role_name = 'Admin' AND is_deleted = false
    LIMIT 1;

    IF admin_role_id IS NULL THEN
        RAISE WARNING '⚠ Admin 角色不存在，跳過權限指派';
        RETURN;
    END IF;

    -- 處理每個新增的權限
    FOR permission_record IN
        SELECT id, permission_code, name
        FROM permissions
        WHERE permission_code IN (
            'user.profile.update',
            'account.read',
            'account.create',
            'account.update',
            'account.delete'
        )
        AND is_deleted = false
    LOOP
        permission_count := permission_count + 1;

        -- 檢查權限是否已指派給 Admin 角色
        IF NOT EXISTS (
            SELECT 1 FROM role_permissions
            WHERE role_id = admin_role_id
              AND permission_id = permission_record.id
        ) THEN
            -- 插入角色-權限關聯
            INSERT INTO role_permissions (
                id,
                role_id,
                permission_id,
                assigned_at,
                assigned_by
            ) VALUES (
                gen_random_uuid(),
                admin_role_id,
                permission_record.id,
                CURRENT_TIMESTAMP,
                NULL
            );

            assigned_count := assigned_count + 1;
            RAISE NOTICE '✓ 權限已指派: % (%)', permission_record.name, permission_record.permission_code;
        END IF;
    END LOOP;

    RAISE NOTICE '========================================';
    RAISE NOTICE '✓ 帳號模組權限建立完成';
    RAISE NOTICE '  - 建立權限數: %', permission_count;
    RAISE NOTICE '  - 指派給 Admin: %', assigned_count;
    RAISE NOTICE '========================================';
END $$;

-- 顯示所有帳號相關權限
SELECT
    permission_code,
    name,
    description,
    permission_type,
    created_at
FROM permissions
WHERE permission_code IN (
    'user.profile.read',
    'user.profile.update',
    'account.read',
    'account.create',
    'account.update',
    'account.delete'
)
AND is_deleted = false
ORDER BY permission_code;

-- 權限對應的 API 端點：
-- - user.profile.read:   GET    /api/account/me
-- - user.profile.update: PUT    /api/account/me/password
-- - account.read:        GET    /api/account, GET /api/account/{id}
-- - account.create:      POST   /api/account
-- - account.update:      PUT    /api/account/{id}, PUT /api/account/{id}/reset-password
-- - account.delete:      DELETE /api/account/{id}
