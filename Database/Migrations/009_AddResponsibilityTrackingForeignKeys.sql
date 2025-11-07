-- 建立責任追蹤外鍵約束
-- Migration: 009
-- Date: 2025-11-07
-- Description: Add responsibility tracking foreign keys for role and permission management

-- 1. role_permissions 表的 assigned_by 外鍵
-- 追蹤誰分配了權限到角色
ALTER TABLE role_permissions 
ADD CONSTRAINT fk_role_permissions_assigned_by 
    FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE SET NULL;

-- 2. user_roles 表的 assigned_by 外鍵
-- 追蹤誰指派了角色給用戶
ALTER TABLE user_roles 
ADD CONSTRAINT fk_user_roles_assigned_by 
    FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE SET NULL;

-- 3. permissions 表的 created_by 外鍵
-- 追蹤誰建立了權限
ALTER TABLE permissions 
ADD CONSTRAINT fk_permissions_created_by 
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL;

-- 4. permissions 表的 updated_by 外鍵
-- 追蹤誰最後修改了權限
ALTER TABLE permissions 
ADD CONSTRAINT fk_permissions_updated_by 
    FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL;

-- 5. permissions 表的 deleted_by 外鍵
-- 追蹤誰刪除了權限
ALTER TABLE permissions 
ADD CONSTRAINT fk_permissions_deleted_by 
    FOREIGN KEY (deleted_by) REFERENCES users(id) ON DELETE SET NULL;

-- 6. roles 表的 created_by 外鍵
-- 追蹤誰建立了角色
ALTER TABLE roles 
ADD CONSTRAINT fk_roles_created_by 
    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL;

-- 7. roles 表的 updated_by 外鍵
-- 追蹤誰最後修改了角色
ALTER TABLE roles 
ADD CONSTRAINT fk_roles_updated_by 
    FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL;

-- 8. roles 表的 deleted_by 外鍵
-- 追蹤誰刪除了角色
ALTER TABLE roles 
ADD CONSTRAINT fk_roles_deleted_by 
    FOREIGN KEY (deleted_by) REFERENCES users(id) ON DELETE SET NULL;

-- 建立索引（優化外鍵查詢）
CREATE INDEX IF NOT EXISTS idx_role_permissions_assigned_by_fk 
    ON role_permissions(assigned_by);

CREATE INDEX IF NOT EXISTS idx_user_roles_assigned_by_fk 
    ON user_roles(assigned_by);

CREATE INDEX IF NOT EXISTS idx_permissions_created_by_fk 
    ON permissions(created_by);

CREATE INDEX IF NOT EXISTS idx_permissions_updated_by_fk 
    ON permissions(updated_by);

CREATE INDEX IF NOT EXISTS idx_permissions_deleted_by_fk 
    ON permissions(deleted_by);

CREATE INDEX IF NOT EXISTS idx_roles_created_by_fk 
    ON roles(created_by);

CREATE INDEX IF NOT EXISTS idx_roles_updated_by_fk 
    ON roles(updated_by);

CREATE INDEX IF NOT EXISTS idx_roles_deleted_by_fk 
    ON roles(deleted_by);

-- 驗證外鍵建立
DO $$
BEGIN
    RAISE NOTICE '✓ 責任追蹤外鍵約束建立成功';
    RAISE NOTICE '  - role_permissions.assigned_by → users.id (SET NULL)';
    RAISE NOTICE '  - user_roles.assigned_by → users.id (SET NULL)';
    RAISE NOTICE '  - permissions.created_by → users.id (SET NULL)';
    RAISE NOTICE '  - permissions.updated_by → users.id (SET NULL)';
    RAISE NOTICE '  - permissions.deleted_by → users.id (SET NULL)';
    RAISE NOTICE '  - roles.created_by → users.id (SET NULL)';
    RAISE NOTICE '  - roles.updated_by → users.id (SET NULL)';
    RAISE NOTICE '  - roles.deleted_by → users.id (SET NULL)';
END $$;
