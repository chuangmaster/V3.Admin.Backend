-- 建立審計外鍵約束
-- Migration: 008
-- Date: 2025-11-07
-- Description: Add audit-related foreign keys to ensure audit trail integrity

-- 1. audit_logs 表的 operator_id 外鍵
-- 使用 RESTRICT：防止刪除有審計記錄的用戶，確保審計追蹤完整性
ALTER TABLE audit_logs 
ADD CONSTRAINT fk_audit_logs_operator_id 
    FOREIGN KEY (operator_id) REFERENCES users(id) ON DELETE RESTRICT;

-- 2. permission_failure_logs 表的 user_id 外鍵
-- 使用 CASCADE：如果用戶被刪除，相關的權限失敗記錄也應該刪除
ALTER TABLE permission_failure_logs 
ADD CONSTRAINT fk_permission_failure_logs_user_id 
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE;

-- 建立索引（優化外鍵查詢）
CREATE INDEX IF NOT EXISTS idx_audit_logs_operator_id_fk 
    ON audit_logs(operator_id);

CREATE INDEX IF NOT EXISTS idx_permission_failure_logs_user_id_fk 
    ON permission_failure_logs(user_id);

-- 驗證外鍵建立
DO $$
BEGIN
    RAISE NOTICE '✓ 審計日誌外鍵約束建立成功';
    RAISE NOTICE '  - audit_logs.operator_id → users.id (RESTRICT)';
    RAISE NOTICE '  - permission_failure_logs.user_id → users.id (CASCADE)';
END $$;
