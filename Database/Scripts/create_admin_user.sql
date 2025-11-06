-- ====================================
-- 建立系統管理員腳本
-- ====================================
-- 此腳本建立一個系統管理員使用者並分配所有權限

-- ====================================
-- 步驟 1: 建立系統管理員使用者
-- ====================================
-- 使用者名稱: admin
-- 密碼: Admin@12345 (密碼雜湊已由 BCrypt 生成)
-- 
-- 注意: 此密碼雜湊是使用 BCrypt (cost=12) 生成的
-- 原始密碼: Admin@12345
-- 若要修改密碼，請使用以下命令生成新的雜湊:
-- dotnet user-secrets set "DefaultPassword" "YourNewPassword"
-- 或使用 C# 代碼: BCrypt.Net.BCrypt.HashPassword("password", 12)

INSERT INTO users (username, password_hash, display_name, created_by)
VALUES (
    'admin',
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5pf1kc6Tpm/Q6',  -- BCrypt hash of 'Admin@12345'
    '系統管理員',
    NULL  -- 初始建立者為 NULL
)
ON CONFLICT (username) DO NOTHING
RETURNING id, username, display_name, created_at;

-- ====================================
-- 步驟 2: 建立「系統管理員」角色
-- ====================================
-- 只在不存在時建立角色
INSERT INTO roles (role_name, description, created_by)
SELECT 
    '系統管理員',
    '具有系統所有權限的管理員角色，可以管理權限、角色、使用者和稽核日誌',
    (SELECT id FROM users WHERE username = 'admin' AND is_deleted = false LIMIT 1)
WHERE NOT EXISTS (
    SELECT 1 FROM roles 
    WHERE role_name = '系統管理員' AND is_deleted = false
)
RETURNING id, role_name, created_at;

-- ====================================
-- 步驟 3: 將所有系統權限分配給「系統管理員」角色
-- ====================================
-- 先刪除該角色已有的權限分配，然後重新分配（確保權限集合最新）
DELETE FROM role_permissions 
WHERE role_id = (SELECT id FROM roles WHERE role_name = '系統管理員' AND is_deleted = false LIMIT 1);

-- 然後將所有有效的系統權限分配給該角色
INSERT INTO role_permissions (role_id, permission_id, assigned_by)
SELECT 
    r.id,
    p.id,
    (SELECT id FROM users WHERE username = 'admin' AND is_deleted = false LIMIT 1)
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = '系統管理員'
  AND r.is_deleted = false
  AND p.is_deleted = false
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ====================================
-- 步驟 4: 將「系統管理員」角色指派給 admin 使用者
-- ====================================
-- 首先恢復已刪除的記錄（如果存在）
UPDATE user_roles
SET is_deleted = false,
    deleted_at = NULL,
    deleted_by = NULL
WHERE user_id = (SELECT id FROM users WHERE username = 'admin' AND is_deleted = false LIMIT 1)
  AND role_id = (SELECT id FROM roles WHERE role_name = '系統管理員' AND is_deleted = false LIMIT 1)
  AND is_deleted = true;

-- 然後插入新記錄（如果不存在）
INSERT INTO user_roles (user_id, role_id, assigned_by)
SELECT 
    u.id,
    r.id,
    u.id  -- 自己指派給自己
FROM users u
CROSS JOIN roles r
WHERE u.username = 'admin'
  AND u.is_deleted = false
  AND r.role_name = '系統管理員'
  AND r.is_deleted = false
  AND NOT EXISTS (
    SELECT 1 FROM user_roles ur
    WHERE ur.user_id = u.id
      AND ur.role_id = r.id
  )
RETURNING user_id, role_id, assigned_at;

-- ====================================
-- 驗證: 查詢系統管理員的完整資訊
-- ====================================
SELECT 
    u.id as user_id,
    u.username,
    u.display_name,
    u.created_at,
    r.role_name,
    COUNT(p.id) as total_permissions
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id AND ur.is_deleted = false
LEFT JOIN roles r ON ur.role_id = r.id AND r.is_deleted = false
LEFT JOIN role_permissions rp ON r.id = rp.role_id
LEFT JOIN permissions p ON rp.permission_id = p.id AND p.is_deleted = false
WHERE u.username = 'admin'
  AND u.is_deleted = false
GROUP BY u.id, u.username, u.display_name, u.created_at, r.role_name;

-- ====================================
-- 查詢系統管理員的所有權限
-- ====================================
SELECT DISTINCT
    p.permission_code,
    p.name,
    p.description,
    p.permission_type,
    p.route_path
FROM users u
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_deleted = false
INNER JOIN roles r ON ur.role_id = r.id AND r.is_deleted = false
INNER JOIN role_permissions rp ON r.id = rp.role_id
INNER JOIN permissions p ON rp.permission_id = p.id AND p.is_deleted = false
WHERE u.username = 'admin'
  AND u.is_deleted = false
ORDER BY p.permission_code;

-- ====================================
-- 重要資訊
-- ====================================
-- 
-- 系統管理員帳號資訊:
-- ├─ 用戶名稱: admin
-- ├─ 密碼: Admin@12345
-- ├─ 顯示名稱: 系統管理員
-- ├─ 角色: 系統管理員
-- └─ 權限: 系統中所有權限
--
-- 登入方式:
-- POST /api/auth/login
-- {
--   "username": "admin",
--   "password": "Admin@12345"
-- }
--
-- 安全建議:
-- 1. 首次登入後立即修改密碼
-- 2. 不要在代碼中硬編碼密碼
-- 3. 定期審查管理員操作日誌
-- 4. 啟用多因素驗證 (MFA) 如果可能
-- 5. 限制管理員帳號的訪問範圍
--
-- 密碼修改:
-- 1. 登入系統
-- 2. 進入帳號設定
-- 3. 使用舊密碼驗證
-- 4. 輸入新密碼並確認
--
-- 若要重置密碼，使用以下 SQL:
-- UPDATE users
-- SET password_hash = '$2a$12$...',  -- 新的 BCrypt 雜湊
--     version = version + 1
-- WHERE username = 'admin'
--   AND is_deleted = false;

