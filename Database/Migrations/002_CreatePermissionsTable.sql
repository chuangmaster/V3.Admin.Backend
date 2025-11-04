-- 建立 permissions 資料表
-- 權限資料表，包含路由權限和功能權限兩種類型

CREATE TABLE IF NOT EXISTS permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_code VARCHAR(100) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    permission_type VARCHAR(20) NOT NULL,              -- 'route' 或 'function'
    route_path VARCHAR(500),                           -- 僅路由權限使用（如 '/inventory', '/users/profile'）
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,                                   -- 建立者 ID（可關聯至 users.id）
    updated_by UUID,                                   -- 最後更新者 ID
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1,
    
    -- 約束條件
    CONSTRAINT chk_permission_type CHECK (permission_type IN ('route', 'function')),
    CONSTRAINT chk_permission_code_format CHECK (
        (permission_type = 'function' AND permission_code ~ '^[a-z0-9]+(\.[a-z0-9]+)+$') OR
        (permission_type = 'route')
    ),
    CONSTRAINT chk_route_path_required CHECK (
        (permission_type = 'route' AND route_path IS NOT NULL) OR
        (permission_type = 'function' AND route_path IS NULL)
    ),
    CONSTRAINT chk_version_positive CHECK (version >= 1)
);

-- 建立索引
CREATE UNIQUE INDEX IF NOT EXISTS idx_permissions_code 
    ON permissions(permission_code);

CREATE INDEX IF NOT EXISTS idx_permissions_code_not_deleted 
    ON permissions(permission_code) 
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_permissions_type 
    ON permissions(permission_type);

CREATE INDEX IF NOT EXISTS idx_permissions_isdeleted 
    ON permissions(is_deleted);

CREATE INDEX IF NOT EXISTS idx_permissions_createdat 
    ON permissions(created_at DESC);

-- 表格註解
COMMENT ON TABLE permissions IS '權限資料表';
COMMENT ON COLUMN permissions.permission_code IS '權限代碼（唯一，功能權限格式為 resource.action，如 inventory.create）';
COMMENT ON COLUMN permissions.permission_type IS '權限類型（route: 路由權限, function: 功能權限）';
COMMENT ON COLUMN permissions.route_path IS '路由路徑（僅路由權限使用，如 /inventory）';
COMMENT ON COLUMN permissions.is_deleted IS '軟刪除標記';
COMMENT ON COLUMN permissions.version IS '版本號，用於樂觀並發控制';
