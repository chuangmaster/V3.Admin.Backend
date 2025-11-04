-- 建立 user_roles 資料表
-- 用戶與角色的多對多關聯表

CREATE TABLE IF NOT EXISTS user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID,                                   -- 指派者 ID
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    
    -- 外鍵約束
    CONSTRAINT fk_user_roles_user_id 
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role_id 
        FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    
    -- 唯一約束（避免重複指派，排除已刪除）
    CONSTRAINT uq_user_role 
        UNIQUE (user_id, role_id)
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id_isdeleted 
    ON user_roles(user_id, is_deleted);

CREATE INDEX IF NOT EXISTS idx_user_roles_role_id 
    ON user_roles(role_id);

CREATE INDEX IF NOT EXISTS idx_user_roles_assigned_at 
    ON user_roles(assigned_at DESC);

-- 表格註解
COMMENT ON TABLE user_roles IS '用戶角色關聯資料表';
COMMENT ON COLUMN user_roles.user_id IS '用戶 ID（外鍵關聯 users.id）';
COMMENT ON COLUMN user_roles.role_id IS '角色 ID（外鍵關聯 roles.id）';
COMMENT ON COLUMN user_roles.assigned_by IS '指派者 ID（關聯 users.id）';
COMMENT ON COLUMN user_roles.is_deleted IS '軟刪除標記';
