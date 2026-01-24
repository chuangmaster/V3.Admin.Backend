-- ============================================================
-- Schema 遷移: TIMESTAMP → TIMESTAMPTZ (UTC0 時間標準化)
-- Migration: 020
-- Date: 2026-01-24
-- Description: 將所有 TIMESTAMP 欄位轉換為 TIMESTAMPTZ,確保時區資訊
-- Related Spec: specs/009-utc0-time-standard
-- ============================================================

-- 注意事項:
-- 1. 此腳本會修改所有時間欄位的類型
-- 2. PostgreSQL 在轉換時會假設原始資料為伺服器當前時區
-- 3. 建議在低流量時段執行
-- 4. 執行前請確保資料庫連線使用 Timezone=UTC

BEGIN;

-- ===== 1. users 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 users 資料表...';

    -- created_at
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'users'
        AND column_name = 'created_at'
        AND data_type = 'timestamp without time zone'
    ) THEN
        ALTER TABLE users
        ALTER COLUMN created_at TYPE TIMESTAMPTZ
        USING created_at AT TIME ZONE 'UTC';

        RAISE NOTICE '✓ users.created_at 已轉換為 TIMESTAMPTZ';
    END IF;

    -- updated_at
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'users'
        AND column_name = 'updated_at'
        AND data_type = 'timestamp without time zone'
    ) THEN
        ALTER TABLE users
        ALTER COLUMN updated_at TYPE TIMESTAMPTZ
        USING updated_at AT TIME ZONE 'UTC';

        RAISE NOTICE '✓ users.updated_at 已轉換為 TIMESTAMPTZ';
    END IF;

    -- deleted_at
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'users'
        AND column_name = 'deleted_at'
        AND data_type = 'timestamp without time zone'
    ) THEN
        ALTER TABLE users
        ALTER COLUMN deleted_at TYPE TIMESTAMPTZ
        USING deleted_at AT TIME ZONE 'UTC';

        RAISE NOTICE '✓ users.deleted_at 已轉換為 TIMESTAMPTZ';
    END IF;
END $$;

-- ===== 2. permissions 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 permissions 資料表...';

    ALTER TABLE permissions
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC',
    ALTER COLUMN updated_at TYPE TIMESTAMPTZ USING updated_at AT TIME ZONE 'UTC',
    ALTER COLUMN deleted_at TYPE TIMESTAMPTZ USING deleted_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ permissions 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 3. roles 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 roles 資料表...';

    ALTER TABLE roles
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC',
    ALTER COLUMN updated_at TYPE TIMESTAMPTZ USING updated_at AT TIME ZONE 'UTC',
    ALTER COLUMN deleted_at TYPE TIMESTAMPTZ USING deleted_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ roles 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 4. role_permissions 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 role_permissions 資料表...';

    ALTER TABLE role_permissions
    ALTER COLUMN assigned_at TYPE TIMESTAMPTZ USING assigned_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ role_permissions 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 5. user_roles 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 user_roles 資料表...';

    ALTER TABLE user_roles
    ALTER COLUMN assigned_at TYPE TIMESTAMPTZ USING assigned_at AT TIME ZONE 'UTC',
    ALTER COLUMN deleted_at TYPE TIMESTAMPTZ USING deleted_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ user_roles 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 6. audit_logs 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 audit_logs 資料表...';

    ALTER TABLE audit_logs
    ALTER COLUMN operation_time TYPE TIMESTAMPTZ USING operation_time AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ audit_logs 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 7. permission_failure_logs 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 permission_failure_logs 資料表...';

    ALTER TABLE permission_failure_logs
    ALTER COLUMN attempted_at TYPE TIMESTAMPTZ USING attempted_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ permission_failure_logs 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 8. customers 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 customers 資料表...';

    ALTER TABLE customers
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC',
    ALTER COLUMN updated_at TYPE TIMESTAMPTZ USING updated_at AT TIME ZONE 'UTC',
    ALTER COLUMN deleted_at TYPE TIMESTAMPTZ USING deleted_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ customers 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 9. service_orders 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 service_orders 資料表...';

    -- 一般時間戳記欄位
    ALTER TABLE service_orders
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC',
    ALTER COLUMN updated_at TYPE TIMESTAMPTZ USING updated_at AT TIME ZONE 'UTC',
    ALTER COLUMN deleted_at TYPE TIMESTAMPTZ USING deleted_at AT TIME ZONE 'UTC';

    -- 業務日期欄位 (DATE 類型保持不變,因為不需要時區資訊)
    -- service_date, consignment_start_date, consignment_end_date 保持為 DATE

    RAISE NOTICE '✓ service_orders 時間欄位已轉換為 TIMESTAMPTZ';
    RAISE NOTICE 'ℹ service_orders 的 DATE 類型欄位維持不變';
END $$;

-- ===== 10. product_items 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 product_items 資料表...';

    ALTER TABLE product_items
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC',
    ALTER COLUMN updated_at TYPE TIMESTAMPTZ USING updated_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ product_items 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 11. attachments 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 attachments 資料表...';

    ALTER TABLE attachments
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC',
    ALTER COLUMN updated_at TYPE TIMESTAMPTZ USING updated_at AT TIME ZONE 'UTC',
    ALTER COLUMN deleted_at TYPE TIMESTAMPTZ USING deleted_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ attachments 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 12. signature_records 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 signature_records 資料表...';

    ALTER TABLE signature_records
    ALTER COLUMN signed_at TYPE TIMESTAMPTZ USING signed_at AT TIME ZONE 'UTC',
    ALTER COLUMN sent_at TYPE TIMESTAMPTZ USING sent_at AT TIME ZONE 'UTC',
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC',
    ALTER COLUMN updated_at TYPE TIMESTAMPTZ USING updated_at AT TIME ZONE 'UTC';

    -- 檢查並轉換 expires_at (如果存在)
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'signature_records'
        AND column_name = 'expires_at'
    ) THEN
        ALTER TABLE signature_records
        ALTER COLUMN expires_at TYPE TIMESTAMPTZ USING expires_at AT TIME ZONE 'UTC';

        RAISE NOTICE '✓ signature_records.expires_at 已轉換為 TIMESTAMPTZ';
    END IF;

    RAISE NOTICE '✓ signature_records 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 13. attachment_view_logs 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 attachment_view_logs 資料表...';

    ALTER TABLE attachment_view_logs
    ALTER COLUMN viewed_at TYPE TIMESTAMPTZ USING viewed_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ attachment_view_logs 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 14. dropbox_sign_webhook_events 資料表 =====
DO $$
BEGIN
    RAISE NOTICE '開始遷移 dropbox_sign_webhook_events 資料表...';

    ALTER TABLE dropbox_sign_webhook_events
    ALTER COLUMN processed_at TYPE TIMESTAMPTZ USING processed_at AT TIME ZONE 'UTC',
    ALTER COLUMN created_at TYPE TIMESTAMPTZ USING created_at AT TIME ZONE 'UTC';

    RAISE NOTICE '✓ dropbox_sign_webhook_events 時間欄位已轉換為 TIMESTAMPTZ';
END $$;

-- ===== 15. 驗證遷移結果 =====
DO $$
DECLARE
    timestamp_count INT;
    timestamptz_count INT;
    rec RECORD;
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE '開始驗證遷移結果...';

    -- 檢查是否還有 TIMESTAMP (without time zone) 欄位
    SELECT COUNT(*) INTO timestamp_count
    FROM information_schema.columns
    WHERE table_schema = 'public'
    AND data_type = 'timestamp without time zone';

    -- 檢查 TIMESTAMPTZ 欄位數量
    SELECT COUNT(*) INTO timestamptz_count
    FROM information_schema.columns
    WHERE table_schema = 'public'
    AND data_type = 'timestamp with time zone';

    RAISE NOTICE 'TIMESTAMP (without time zone) 欄位剩餘: %', timestamp_count;
    RAISE NOTICE 'TIMESTAMPTZ (with time zone) 欄位數量: %', timestamptz_count;

    IF timestamp_count > 0 THEN
        RAISE WARNING '⚠ 仍有 % 個 TIMESTAMP 欄位未轉換', timestamp_count;

        -- 列出未轉換的欄位
        RAISE NOTICE '未轉換的欄位清單:';
        FOR rec IN
            SELECT table_name, column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
            AND data_type = 'timestamp without time zone'
            ORDER BY table_name, column_name
        LOOP
            RAISE NOTICE '  - %.%', rec.table_name, rec.column_name;
        END LOOP;
    ELSE
        RAISE NOTICE '✓ 所有 TIMESTAMP 欄位已成功轉換為 TIMESTAMPTZ';
    END IF;

    RAISE NOTICE '========================================';
END $$;

COMMIT;

-- ===== 執行後檢查清單 =====
-- 1. 確認所有時間欄位已轉換為 TIMESTAMPTZ
-- 2. 檢查應用程式日誌,確認無時區相關錯誤
-- 3. 執行整合測試驗證 API 端點
-- 4. 檢查 Dapper 查詢是否正常運作
-- 5. 驗證時間資料的顯示格式正確 (包含 Z 後綴)

-- ===== Rollback 腳本 (如需要) =====
-- 如果需要回滾,可以執行:
/*
ALTER TABLE users
ALTER COLUMN created_at TYPE TIMESTAMP USING created_at AT TIME ZONE 'UTC',
ALTER COLUMN updated_at TYPE TIMESTAMP USING updated_at AT TIME ZONE 'UTC',
ALTER COLUMN deleted_at TYPE TIMESTAMP USING deleted_at AT TIME ZONE 'UTC';

-- 對其他資料表重複相同操作...
*/
