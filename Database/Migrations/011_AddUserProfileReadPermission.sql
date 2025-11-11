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

-- 驗證權限建立
DO $$
DECLARE
    permission_count INT;
BEGIN
    SELECT COUNT(*) INTO permission_count 
    FROM permissions 
    WHERE permission_code = 'user.profile.read' AND is_deleted = false;
    
    IF permission_count > 0 THEN
        RAISE NOTICE '✓ 用戶個人資料查詢權限建立成功';
        RAISE NOTICE '  - Permission Code: user.profile.read';
        RAISE NOTICE '  - Permission Name: 查詢個人資料';
        RAISE NOTICE '  - Permission Type: function';
    ELSE
        RAISE WARNING '✗ 用戶個人資料查詢權限建立失敗或已存在';
    END IF;
END $$;

-- 權限對應的 API 端點：GET /api/account/me
-- 此端點允許已登入用戶查詢自己的個人資料
COMMENT ON COLUMN permissions.id IS 'Unique identifier for the permission record (added in migration 011)';
