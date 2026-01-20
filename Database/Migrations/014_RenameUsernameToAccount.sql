-- 將 username 欄位重命名為 account
-- Migration: 014
-- Date: 2026-01-20
-- Description: Rename username field to account for better semantic clarity
-- Feature: 007-account-refactor

-- 使用 transaction 確保原子性
BEGIN;

-- =====================================================
-- Step 1: 重命名欄位
-- =====================================================
ALTER TABLE users RENAME COLUMN username TO account;

-- =====================================================
-- Step 2: 重命名約束條件
-- =====================================================
-- 刪除舊的 CHECK 約束並建立新的
ALTER TABLE users DROP CONSTRAINT IF EXISTS chk_username_length;
ALTER TABLE users ADD CONSTRAINT chk_account_length CHECK (LENGTH(account) BETWEEN 3 AND 20);

-- 刪除舊的 UNIQUE 約束並建立新的
ALTER TABLE users DROP CONSTRAINT IF EXISTS uq_users_username;
ALTER TABLE users ADD CONSTRAINT uq_users_account UNIQUE (account);

-- =====================================================
-- Step 3: 重命名索引
-- =====================================================
-- 刪除舊索引並建立新索引
DROP INDEX IF EXISTS idx_users_username_not_deleted;
CREATE INDEX IF NOT EXISTS idx_users_account_not_deleted 
    ON users(account) 
    WHERE is_deleted = false;

-- =====================================================
-- Step 4: 更新欄位註解
-- =====================================================
COMMENT ON COLUMN users.account IS '帳號名稱 (唯一,用於登入,3-20字元)';

-- =====================================================
-- Step 5: 資料完整性驗證
-- =====================================================
DO $$
DECLARE
    total_count INT;
    null_account_count INT;
    column_exists BOOLEAN;
BEGIN
    -- 驗證欄位是否已重命名
    SELECT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'users' 
          AND column_name = 'account'
    ) INTO column_exists;
    
    IF NOT column_exists THEN
        RAISE EXCEPTION 'Migration failed: account column does not exist';
    END IF;

    -- 驗證舊欄位是否已移除
    SELECT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'users' 
          AND column_name = 'username'
    ) INTO column_exists;
    
    IF column_exists THEN
        RAISE EXCEPTION 'Migration failed: username column still exists';
    END IF;

    -- 驗證資料完整性
    SELECT COUNT(*) INTO total_count FROM users;
    SELECT COUNT(*) INTO null_account_count FROM users WHERE account IS NULL OR account = '';
    
    IF null_account_count > 0 THEN
        RAISE EXCEPTION 'Data integrity check failed: % users have NULL or empty account', null_account_count;
    END IF;
    
    RAISE NOTICE '✓ Migration 014 successful: % users migrated, username renamed to account', total_count;
END $$;

COMMIT;
