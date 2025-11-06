-- 初始權限種子資料
-- 為管理員用戶初始化系統權限

-- 插入管理員專用的系統權限
-- 這些權限代表管理員可以執行的所有系統管理操作

-- 權限管理權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('permission.read', '查詢權限', '允許查詢和列出所有系統權限', 'function'),
    ('permission.create', '新增權限', '允許創建新的系統權限定義', 'function'),
    ('permission.update', '修改權限', '允許編輯現有權限的詳細資訊', 'function'),
    ('permission.delete', '刪除權限', '允許刪除不使用的權限', 'function'),
    ('permission.assign', '分配權限', '允許為角色分配權限', 'function'),
    ('permission.remove', '移除權限', '允許從角色移除權限', 'function')
ON CONFLICT (permission_code) DO NOTHING;

-- 角色管理權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('role.read', '查詢角色', '允許查詢和列出所有系統角色', 'function'),
    ('role.create', '新增角色', '允許創建新的系統角色', 'function'),
    ('role.update', '修改角色', '允許編輯現有角色的詳細資訊', 'function'),
    ('role.delete', '刪除角色', '允許刪除不使用的角色', 'function'),
    ('role.assign', '指派角色', '允許為用戶指派角色', 'function'),
    ('role.remove', '移除角色', '允許從用戶移除角色', 'function')
ON CONFLICT (permission_code) DO NOTHING;

-- 帳號管理權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('account.read', '查詢帳號', '允許查詢系統帳號信息', 'function'),
    ('account.create', '新增帳號', '允許創建新的系統帳號', 'function'),
    ('account.update', '修改帳號', '允許編輯帳號信息', 'function'),
    ('account.delete', '刪除帳號', '允許刪除帳號', 'function')
ON CONFLICT (permission_code) DO NOTHING;

-- 稽核日誌權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('audit_log.read', '查詢稽核日誌', '允許查詢系統稽核日誌和操作歷史', 'function')
ON CONFLICT (permission_code) DO NOTHING;

