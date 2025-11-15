-- 建立使用者建立/更新者自我參考外鍵
-- Migration: 012
-- Date: 2025-11-15
-- Description: Add user creation and update tracking foreign keys

-- 1. users 表的 created_by 自我參考外鍵
-- 追蹤誰建立了該用戶
ALTER TABLE users 
ADD CONSTRAINT fk_users_created_by 
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL;

-- 2. users 表的 updated_by 自我參考外鍵
-- 追蹤誰最後更新了該用戶
ALTER TABLE users 
ADD CONSTRAINT fk_users_updated_by 
    FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL;

-- 3. role_permissions 表的 assigned_by 外鍵
-- 追蹤誰分配了該角色權限
ALTER TABLE role_permissions 
ADD CONSTRAINT fk_role_permissions_assigned_by 
    FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE SET NULL;

-- 4. user_roles 表的 assigned_by 外鍵
-- 追蹤誰分配了該用戶角色
ALTER TABLE user_roles 
ADD CONSTRAINT fk_user_roles_assigned_by 
    FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE SET NULL;

-- 建立索引（優化外鍵查詢）
CREATE INDEX IF NOT EXISTS idx_users_created_by_fk 
    ON users(created_by);

CREATE INDEX IF NOT EXISTS idx_users_updated_by_fk 
    ON users(updated_by);

CREATE INDEX IF NOT EXISTS idx_role_permissions_assigned_by_fk 
    ON role_permissions(assigned_by);

CREATE INDEX IF NOT EXISTS idx_user_roles_assigned_by_fk 
    ON user_roles(assigned_by);

-- 驗證外鍵建立
DO $$
BEGIN
    RAISE NOTICE '✓ 使用者追蹤外鍵約束建立成功';
    RAISE NOTICE '  - users.created_by → users.id (SET NULL)';
    RAISE NOTICE '  - users.updated_by → users.id (SET NULL)';
    RAISE NOTICE '  - role_permissions.assigned_by → users.id (SET NULL)';
    RAISE NOTICE '  - user_roles.assigned_by → users.id (SET NULL)';
END $$;
