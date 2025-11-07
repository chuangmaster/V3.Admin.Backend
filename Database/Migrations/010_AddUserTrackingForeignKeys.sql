-- 建立用戶相關的自我參考和刪除追蹤外鍵
-- Migration: 010
-- Date: 2025-11-07
-- Description: Add user self-reference and deletion tracking foreign keys

-- 1. users 表的 deleted_by 自我參考外鍵
-- 追蹤誰刪除了該用戶
ALTER TABLE users 
ADD CONSTRAINT fk_users_deleted_by 
    FOREIGN KEY (deleted_by) REFERENCES users(id) ON DELETE SET NULL;

-- 2. user_roles 表的 deleted_by 外鍵
-- 追蹤誰刪除了該用戶角色指派
ALTER TABLE user_roles 
ADD CONSTRAINT fk_user_roles_deleted_by 
    FOREIGN KEY (deleted_by) REFERENCES users(id) ON DELETE SET NULL;

-- 建立索引（優化外鍵查詢）
CREATE INDEX IF NOT EXISTS idx_users_deleted_by_fk 
    ON users(deleted_by);

CREATE INDEX IF NOT EXISTS idx_user_roles_deleted_by_fk 
    ON user_roles(deleted_by);

-- 驗證外鍵建立
DO $$
BEGIN
    RAISE NOTICE '✓ 用戶追蹤外鍵約束建立成功';
    RAISE NOTICE '  - users.deleted_by → users.id (SET NULL)';
    RAISE NOTICE '  - user_roles.deleted_by → users.id (SET NULL)';
END $$;
