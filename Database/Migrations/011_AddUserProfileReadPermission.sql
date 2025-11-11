-- 新增用戶個人資料查詢權限
-- Migration: 011
-- Date: 2025-11-12
-- Description: Add user.profile.read permission for user profile query feature
-- Feature: 003-user-profile

-- 新增 user.profile.read 權限
-- 此權限允許用戶查詢自己的個人資料，包括用戶名稱、顯示名稱和角色清單
INSERT INTO permissions (
    id,
    permission_code,
    name,
    description,
    permission_type,
    route_path,
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
    'user.profile.read',
    '查詢個人資料',
    '允許用戶查詢自己的個人資料，包括用戶名稱、顯示名稱和角色',
    'function',
    NULL,
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

-- 驗證權限建立並與角色建立關聯
DO $$
DECLARE
    permission_id UUID;
    admin_role_id UUID;
    role_permission_exists BOOLEAN;
    permission_count INT;
BEGIN
    -- 查詢權限 ID
    SELECT id INTO permission_id 
    FROM permissions 
    WHERE permission_code = 'user.profile.read' AND is_deleted = false
    LIMIT 1;
    
    -- 查詢 Admin 角色 ID
    SELECT id INTO admin_role_id 
    FROM roles 
    WHERE role_name = 'Admin' AND is_deleted = false
    LIMIT 1;
    
    IF permission_id IS NOT NULL THEN
        RAISE NOTICE '✓ 用戶個人資料查詢權限建立成功';
        RAISE NOTICE '  - Permission Code: user.profile.read';
        RAISE NOTICE '  - Permission Name: 查詢個人資料';
        RAISE NOTICE '  - Permission Type: function';
        RAISE NOTICE '  - Permission ID: %', permission_id;
        
        -- 如果 Admin 角色存在，將權限指派給 Admin 角色
        IF admin_role_id IS NOT NULL THEN
            -- 檢查關聯是否已存在
            SELECT EXISTS(
                SELECT 1 FROM role_permissions 
                WHERE role_id = admin_role_id 
                  AND permission_id = permission_id
            ) INTO role_permission_exists;
            
            IF NOT role_permission_exists THEN
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
                    permission_id,
                    CURRENT_TIMESTAMP,
                    NULL
                );
                
                RAISE NOTICE '✓ 權限已指派給 Admin 角色';
                RAISE NOTICE '  - Role: Admin';
                RAISE NOTICE '  - Permission: user.profile.read';
            ELSE
                RAISE NOTICE '⚠ Admin 角色已具有此權限';
            END IF;
        ELSE
            RAISE WARNING '⚠ Admin 角色不存在，跳過角色權限指派';
        END IF;
    ELSE
        RAISE WARNING '✗ 用戶個人資料查詢權限建立失敗或已存在';
    END IF;
END $$;

-- 驗證權限與角色的關聯
DO $$
DECLARE
    association_count INT;
BEGIN
    SELECT COUNT(*) INTO association_count
    FROM role_permissions rp
    JOIN permissions p ON rp.permission_id = p.id
    JOIN roles r ON rp.role_id = r.id
    WHERE p.permission_code = 'user.profile.read' 
      AND r.role_name = 'Admin'
      AND p.is_deleted = false
      AND r.is_deleted = false;
    
    IF association_count > 0 THEN
        RAISE NOTICE '✓ 用戶個人資料查詢權限與 Admin 角色的關聯驗證成功';
    ELSE
        RAISE WARNING '⚠ 權限與 Admin 角色關聯未建立';
    END IF;
END $$;

-- 權限對應的 API 端點：GET /api/account/me
-- 此端點允許已登入用戶查詢自己的個人資料
-- 此權限已自動指派給 Admin 角色
COMMENT ON COLUMN permissions.id IS 'Unique identifier for the permission record (added in migration 011)';
