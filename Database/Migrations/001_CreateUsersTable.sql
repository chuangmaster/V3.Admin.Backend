-- 建立使用者資料表
-- Migration: 001
-- Date: 2025-10-27
-- Description: Create users table for account management system

-- 建立 users 資料表
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(20) NOT NULL,
    password_hash VARCHAR(60) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1,
    
    -- 約束
    CONSTRAINT chk_username_length CHECK (LENGTH(username) BETWEEN 3 AND 20),
    CONSTRAINT chk_displayname_length CHECK (LENGTH(display_name) <= 100),
    CONSTRAINT chk_version_positive CHECK (version >= 1)
);

-- 建立約束與索引
-- 帳號名稱唯一約束（支援 ON CONFLICT）
ALTER TABLE users
ADD CONSTRAINT uq_users_username UNIQUE (username);

-- 帳號名稱查詢索引 (排除已刪除，優化軟刪除查詢)
CREATE INDEX IF NOT EXISTS idx_users_username_not_deleted 
    ON users(username) 
    WHERE is_deleted = false;

-- 軟刪除查詢索引
CREATE INDEX IF NOT EXISTS idx_users_isdeleted 
    ON users(is_deleted);

-- 建立時間索引 (用於排序)
CREATE INDEX IF NOT EXISTS idx_users_createdat 
    ON users(created_at DESC);

-- 建立註解
COMMENT ON TABLE users IS '使用者資料表';
COMMENT ON COLUMN users.id IS '使用者唯一識別碼 (GUID)';
COMMENT ON COLUMN users.username IS '帳號名稱 (唯一,用於登入,3-20字元)';
COMMENT ON COLUMN users.password_hash IS '密碼雜湊 (BCrypt, 60字元)';
COMMENT ON COLUMN users.display_name IS '顯示名稱 (最大100字元)';
COMMENT ON COLUMN users.created_at IS '建立時間 (UTC)';
COMMENT ON COLUMN users.updated_at IS '最後更新時間 (UTC)';
COMMENT ON COLUMN users.created_by IS '建立者 ID (可關聯至 users.id)';
COMMENT ON COLUMN users.updated_by IS '最後更新者 ID (可關聯至 users.id)';
COMMENT ON COLUMN users.is_deleted IS '是否已刪除 (軟刪除標記)';
COMMENT ON COLUMN users.deleted_at IS '刪除時間 (UTC)';
COMMENT ON COLUMN users.deleted_by IS '刪除操作者 ID (可關聯至 users.id)';
COMMENT ON COLUMN users.version IS '版本號 (樂觀並發控制,初始值為1)';

-- 建立 FUNCTION: 自動更新 updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 建立 TRIGGER: 更新時自動設定 updated_at
CREATE TRIGGER update_users_updated_at 
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 驗證資料表建立
DO $$
BEGIN
    IF EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'users'
    ) THEN
        RAISE NOTICE '✓ users 資料表建立成功';
    ELSE
        RAISE EXCEPTION '✗ users 資料表建立失敗';
    END IF;
END $$;
