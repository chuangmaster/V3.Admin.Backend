-- Migration: 移除 RoutePath 欄位並更新權限類型支援
-- Date: 2025-11-16
-- Description: 完全移除 route_path 欄位，並更新權限類型支援 function 和 view

-- Step 1: 移除舊的約束條件
ALTER TABLE permissions
    DROP CONSTRAINT IF EXISTS chk_permission_type,
    DROP CONSTRAINT IF EXISTS chk_permission_code_format,
    DROP CONSTRAINT IF EXISTS chk_route_path_required;

-- Step 2: 移除 route_path 欄位
ALTER TABLE permissions
    DROP COLUMN IF EXISTS route_path;

-- Step 3: 更新權限類型約束（支援 'function' 和 'view'）
ALTER TABLE permissions
    ADD CONSTRAINT chk_permission_type CHECK (permission_type IN ('function', 'view'));

-- Step 4: 更新權限代碼格式約束（適用所有類型）
-- 允許字母、數字、點號、下劃線，長度 3-100 字元
-- 開頭和結尾不能是點號或下劃線
ALTER TABLE permissions
    ADD CONSTRAINT chk_permission_code_format CHECK (
        permission_code ~ '^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$' OR
        permission_code ~ '^[a-zA-Z0-9]$'
    );

-- Step 5: 更新表格註解
COMMENT ON TABLE permissions IS '權限資料表 - 支援 function（操作權限）和 view（UI 區塊瀏覽權限）';
COMMENT ON COLUMN permissions.permission_code IS '權限代碼（唯一，格式為 resource.action 或 resource.subresource.action）';
COMMENT ON COLUMN permissions.permission_type IS '權限類型（function: 功能操作權限, view: UI 區塊瀏覽權限）';

-- Migration completed successfully
