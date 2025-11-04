-- 建立 permission_failure_logs 資料表
-- 權限驗證失敗記錄資料表，用於安全監控

CREATE TABLE IF NOT EXISTS permission_failure_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    username VARCHAR(100) NOT NULL,                    -- 用戶名稱（快照）
    attempted_resource VARCHAR(500) NOT NULL,          -- 嘗試訪問的資源（權限代碼或路由）
    failure_reason VARCHAR(200) NOT NULL,              -- 失敗原因
    attempted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address VARCHAR(45),                            -- 操作者 IP 位址
    user_agent TEXT,                                   -- 操作者 UserAgent
    trace_id VARCHAR(50)                               -- TraceId（分散式追蹤）
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_permission_failure_logs_user_id 
    ON permission_failure_logs(user_id);

CREATE INDEX IF NOT EXISTS idx_permission_failure_logs_attempted_at_desc 
    ON permission_failure_logs(attempted_at DESC);

CREATE INDEX IF NOT EXISTS idx_permission_failure_logs_user_time 
    ON permission_failure_logs(user_id, attempted_at DESC);

-- 表格註解
COMMENT ON TABLE permission_failure_logs IS '權限驗證失敗記錄資料表';
COMMENT ON COLUMN permission_failure_logs.attempted_resource IS '嘗試訪問的資源（權限代碼或路由路徑）';
COMMENT ON COLUMN permission_failure_logs.failure_reason IS '失敗原因（如 "權限不足", "用戶無任何角色"）';
