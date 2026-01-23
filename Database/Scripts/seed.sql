-- 初始資料腳本
-- Description: Insert default admin and test user accounts
-- Date: 2025-10-27

-- 清理舊資料 (僅開發環境,生產環境請移除此段)
-- TRUNCATE TABLE users RESTART IDENTITY CASCADE;

-- 插入預設管理員帳號
-- 帳號: admin
-- 密碼: Admin@123
-- BCrypt Hash (work factor 12): $2a$12$XHvy84E7twXPVs0WwzqWBuxj1Sp2yb46QW2xsLN3lrHUlibMg.a1G
INSERT INTO users (id, account, password_hash, display_name, created_at, is_deleted, version)
VALUES (
    '00000000-0000-0000-0000-000000000001'::UUID,
    'admin',
    '$2a$12$XHvy84E7twXPVs0WwzqWBuxj1Sp2yb46QW2xsLN3lrHUlibMg.a1G',
    '系統管理員',
    CURRENT_TIMESTAMP,
    false,
    1
)
ON CONFLICT (id) DO NOTHING;

-- 插入測試帳號
-- 帳號: testuser
-- 密碼: Test@123
-- BCrypt Hash (work factor 12): $2a$12$v/27awEJ7xYNetSX0YshdOkkLS9EUwLpmj3rNLqp6JSPOG2R.r5k.
INSERT INTO users (id, account, password_hash, display_name, created_at, is_deleted, version)
VALUES (
    '00000000-0000-0000-0000-000000000002'::UUID,
    'testuser',
    '$2a$12$v/27awEJ7xYNetSX0YshdOkkLS9EUwLpmj3rNLqp6JSPOG2R.r5k.',
    '測試使用者',
    CURRENT_TIMESTAMP,
    false,
    1
)
ON CONFLICT (id) DO NOTHING;

-- 驗證資料插入
DO $$
DECLARE
    user_count INT;
BEGIN
    SELECT COUNT(*) INTO user_count FROM users WHERE is_deleted = false;

    IF user_count >= 2 THEN
        RAISE NOTICE '✓ 初始資料插入成功: % 個帳號', user_count;
    ELSE
        RAISE WARNING '⚠ 預期至少 2 個帳號,實際: %', user_count;
    END IF;
END $$;

-- 顯示插入的帳號資訊 (不含密碼雜湊)
SELECT
    id,
    account,
    display_name,
    created_at,
    is_deleted
FROM users
ORDER BY created_at;
