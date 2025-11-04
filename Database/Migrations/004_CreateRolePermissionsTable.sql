-- 建立 role_permissions 資料表
-- 角色與權限的多對多關聯表

CREATE TABLE IF NOT EXISTS role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL,
    permission_id UUID NOT NULL,
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID,                                   -- 分配者 ID
    
    -- 外鍵約束
    CONSTRAINT fk_role_permissions_role_id 
        FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permissions_permission_id 
        FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE,
    
    -- 唯一約束（避免重複分配）
    CONSTRAINT uq_role_permission 
        UNIQUE (role_id, permission_id)
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_role_permissions_role_id 
    ON role_permissions(role_id);

CREATE INDEX IF NOT EXISTS idx_role_permissions_permission_id 
    ON role_permissions(permission_id);

CREATE INDEX IF NOT EXISTS idx_role_permissions_assigned_at 
    ON role_permissions(assigned_at DESC);

-- 表格註解
COMMENT ON TABLE role_permissions IS '角色權限關聯資料表';
COMMENT ON COLUMN role_permissions.role_id IS '角色 ID（外鍵關聯 roles.id）';
COMMENT ON COLUMN role_permissions.permission_id IS '權限 ID（外鍵關聯 permissions.id）';
COMMENT ON COLUMN role_permissions.assigned_by IS '分配者 ID（關聯 users.id）';
