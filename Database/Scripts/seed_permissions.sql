-- 初始權限種子資料
-- 為管理員用戶初始化系統權限

-- 插入管理員專用的系統權限
-- 這些權限代表管理員可以執行的所有系統管理操作

-- 權限管理權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('permissions.create', '新增權限', '允許創建新的系統權限定義', 'function'),
    ('permissions.view', '查詢權限', '允許查詢和列出所有系統權限', 'function'),
    ('permissions.update', '修改權限', '允許編輯現有權限的詳細資訊', 'function'),
    ('permissions.delete', '刪除權限', '允許刪除不使用的權限', 'function'),
    ('permissions.admin', '權限管理', '完整的權限管理權限', 'function')
ON CONFLICT (permission_code) DO NOTHING;

-- 角色管理權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('roles.create', '新增角色', '允許創建新的系統角色', 'function'),
    ('roles.view', '查詢角色', '允許查詢和列出所有系統角色', 'function'),
    ('roles.update', '修改角色', '允許編輯現有角色的詳細資訊', 'function'),
    ('roles.delete', '刪除角色', '允許刪除不使用的角色', 'function'),
    ('roles.assign_permissions', '分配角色權限', '允許為角色分配和移除權限', 'function'),
    ('roles.admin', '角色管理', '完整的角色管理權限', 'function')
ON CONFLICT (permission_code) DO NOTHING;

-- 用戶角色管理權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('user_roles.assign', '指派用戶角色', '允許為用戶指派角色', 'function'),
    ('user_roles.revoke', '移除用戶角色', '允許從用戶移除角色', 'function'),
    ('user_roles.view', '查詢用戶角色', '允許查詢用戶的角色和權限', 'function'),
    ('user_roles.admin', '用戶角色管理', '完整的用戶角色管理權限', 'function')
ON CONFLICT (permission_code) DO NOTHING;

-- 稽核日誌權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('audit_logs.view', '查詢稽核日誌', '允許查詢系統稽核日誌和操作歷史', 'function'),
    ('audit_logs.admin', '稽核日誌管理', '稽核日誌完整管理權限', 'function')
ON CONFLICT (permission_code) DO NOTHING;

-- 權限驗證和路由權限
INSERT INTO permissions (permission_code, name, description, permission_type, route_path) 
VALUES 
    ('permissions_management_access', '權限管理頁面訪問', '允許訪問權限管理頁面', 'route', '/admin/permissions'),
    ('roles_management_access', '角色管理頁面訪問', '允許訪問角色管理頁面', 'route', '/admin/roles'),
    ('user_roles_management_access', '用戶角色管理頁面訪問', '允許訪問用戶角色指派頁面', 'route', '/admin/user-roles'),
    ('audit_logs_access', '稽核日誌頁面訪問', '允許訪問稽核日誌查詢頁面', 'route', '/admin/audit-logs')
ON CONFLICT (permission_code) DO NOTHING;
