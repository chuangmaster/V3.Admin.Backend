-- 建立 roles 資料表
-- 角色資料表，代表權限的集合

CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1,
    
    -- 約束條件
    CONSTRAINT chk_role_name_length CHECK (LENGTH(role_name) BETWEEN 1 AND 100),
    CONSTRAINT chk_role_version_positive CHECK (version >= 1)
);

-- 建立索引
CREATE UNIQUE INDEX IF NOT EXISTS idx_roles_name 
    ON roles(role_name) 
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_roles_isdeleted 
    ON roles(is_deleted);

CREATE INDEX IF NOT EXISTS idx_roles_createdat 
    ON roles(created_at DESC);

-- 表格註解
COMMENT ON TABLE roles IS '角色資料表';
COMMENT ON COLUMN roles.role_name IS '角色名稱（唯一，1-100 字元）';
COMMENT ON COLUMN roles.is_deleted IS '軟刪除標記';
COMMENT ON COLUMN roles.version IS '版本號，用於樂觀並發控制';
