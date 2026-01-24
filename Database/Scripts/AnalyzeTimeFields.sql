-- ============================================================
-- 資料庫時間欄位分析腳本
-- 目的: 識別所有包含時間資料的欄位及其當前格式
-- 執行環境: PostgreSQL 15+
-- 執行時機: Phase 5 - US3 資料遷移前的準備工作
-- ============================================================

-- 查詢所有資料表的時間類型欄位
-- 包含: timestamp, timestamptz, date 等時間相關類型
SELECT 
    t.table_schema AS schema_name,
    t.table_name,
    c.column_name,
    c.data_type,
    c.is_nullable,
    c.column_default,
    -- 統計該欄位有多少筆資料
    (SELECT COUNT(*) 
     FROM information_schema.tables AS it 
     WHERE it.table_schema = t.table_schema 
       AND it.table_name = t.table_name
       AND it.table_type = 'BASE TABLE'
    ) AS estimated_rows
FROM information_schema.tables AS t
JOIN information_schema.columns AS c 
    ON t.table_schema = c.table_schema 
   AND t.table_name = c.table_name
WHERE t.table_schema NOT IN ('pg_catalog', 'information_schema')
  AND t.table_type = 'BASE TABLE'
  AND c.data_type IN ('timestamp without time zone', 'timestamp with time zone', 'date', 'time without time zone', 'time with time zone')
ORDER BY t.table_schema, t.table_name, c.column_name;

-- 查詢系統常見時間欄位的命名模式
-- 幫助識別可能需要遷移的業務時間欄位
SELECT DISTINCT
    t.table_schema,
    t.table_name,
    c.column_name,
    c.data_type,
    CASE 
        WHEN c.column_name ILIKE '%created%' THEN '建立時間'
        WHEN c.column_name ILIKE '%updated%' THEN '更新時間'
        WHEN c.column_name ILIKE '%deleted%' THEN '刪除時間'
        WHEN c.column_name ILIKE '%date%' THEN '業務日期'
        WHEN c.column_name ILIKE '%time%' THEN '業務時間'
        WHEN c.column_name ILIKE '%at' THEN '時間戳記'
        ELSE '其他'
    END AS field_category
FROM information_schema.tables AS t
JOIN information_schema.columns AS c 
    ON t.table_schema = c.table_schema 
   AND t.table_name = c.table_name
WHERE t.table_schema NOT IN ('pg_catalog', 'information_schema')
  AND t.table_type = 'BASE TABLE'
  AND c.data_type IN ('timestamp without time zone', 'timestamp with time zone', 'date')
ORDER BY field_category, t.table_schema, t.table_name;

-- 檢查當前資料庫的時區設定
SHOW timezone;

-- 檢查當前連線的時區設定
SELECT current_setting('TIMEZONE') AS current_timezone;

-- 範例: 檢查特定資料表的時間資料範例
-- (需要根據實際資料表名稱調整)
-- 這可以幫助我們了解現有資料的時間格式和範圍

-- 注意: 以下查詢範例,實際執行時需要根據分析結果調整
/*
-- 範例 1: 檢查 users 資料表的時間欄位
SELECT 
    id,
    created_at,
    updated_at,
    deleted_at,
    TO_CHAR(created_at AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.MS"Z"') AS created_at_utc0,
    EXTRACT(TIMEZONE FROM created_at) AS created_at_timezone
FROM users
ORDER BY created_at DESC
LIMIT 10;

-- 範例 2: 檢查 service_orders 資料表的業務時間欄位
SELECT 
    id,
    service_date,
    created_at,
    TO_CHAR(service_date AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.MS"Z"') AS service_date_utc0
FROM service_orders
ORDER BY service_date DESC
LIMIT 10;
*/

-- ============================================================
-- 執行結果記錄指引:
-- 1. 將查詢結果匯出為 CSV 或 JSON
-- 2. 記錄所有包含時間欄位的資料表
-- 3. 識別需要遷移的欄位清單
-- 4. 評估資料量以規劃遷移策略
-- ============================================================
