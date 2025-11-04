-- 建立 audit_logs 資料表
-- 稽核日誌資料表，記錄權限管理相關操作
-- 僅可新增和查詢，不可修改或刪除

CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id UUID NOT NULL,                         -- 操作者 ID
    operator_name VARCHAR(100) NOT NULL,               -- 操作者名稱（快照，避免 JOIN）
    operation_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    operation_type VARCHAR(50) NOT NULL,               -- 如 '新增權限', '修改角色', '指派角色'
    target_type VARCHAR(50) NOT NULL,                  -- 如 'Permission', 'Role', 'UserRole'
    target_id UUID NOT NULL,                           -- 目標對象 ID
    before_state TEXT,                                 -- 變更前狀態（JSON 格式）
    after_state TEXT,                                  -- 變更後狀態（JSON 格式）
    ip_address VARCHAR(45),                            -- 操作者 IP 位址（支援 IPv6）
    user_agent TEXT,                                   -- 操作者 UserAgent
    trace_id VARCHAR(50)                               -- TraceId（關聯分散式追蹤）
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_audit_logs_operation_time_desc 
    ON audit_logs(operation_time DESC);

CREATE INDEX IF NOT EXISTS idx_audit_logs_operator_id 
    ON audit_logs(operator_id);

CREATE INDEX IF NOT EXISTS idx_audit_logs_operation_type 
    ON audit_logs(operation_type);

CREATE INDEX IF NOT EXISTS idx_audit_logs_target_type 
    ON audit_logs(target_type);

CREATE INDEX IF NOT EXISTS idx_audit_logs_operator_time 
    ON audit_logs(operator_id, operation_time DESC);

-- 表格註解
COMMENT ON TABLE audit_logs IS '稽核日誌資料表（僅可新增和查詢，不可修改或刪除）';
COMMENT ON COLUMN audit_logs.before_state IS '變更前狀態（JSON 格式，新增操作為 null）';
COMMENT ON COLUMN audit_logs.after_state IS '變更後狀態（JSON 格式，刪除操作後為 null）';
COMMENT ON COLUMN audit_logs.trace_id IS '分散式追蹤 ID';
