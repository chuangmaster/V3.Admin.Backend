-- 服務單管理模組測試資料 (僅供開發/測試環境)
-- Date: 2025-12-18

DO $$
DECLARE
    v_user_id UUID;
    v_customer_id UUID;
    v_order_id UUID;
BEGIN
    SELECT id INTO v_user_id
    FROM users
    WHERE is_deleted = false
    ORDER BY created_at
    LIMIT 1;

    IF v_user_id IS NULL THEN
        RAISE NOTICE 'ℹ 找不到可用的 users 資料，略過 service order seed';
        RETURN;
    END IF;

    -- 建立客戶 (若同 id_number 已存在則略過)
    INSERT INTO customers (name, phone_number, email, id_number, created_by)
    VALUES ('王小明', '0912345678', 'test@example.com', 'A123456789', v_user_id)
    ON CONFLICT (id_number) DO NOTHING;

    SELECT id INTO v_customer_id
    FROM customers
    WHERE id_number = 'A123456789'
      AND is_deleted = false
    LIMIT 1;

    IF v_customer_id IS NULL THEN
        RAISE NOTICE 'ℹ 建立/取得 customers 失敗，略過 service order seed';
        RETURN;
    END IF;

    -- 建立一筆收購單 (序號由 trigger 自動產生)
    INSERT INTO service_orders (
        order_type,
        order_source,
        customer_id,
        total_amount,
        status,
        created_by
    )
    VALUES (
        'BUYBACK',
        'OFFLINE',
        v_customer_id,
        120000,
        'PENDING',
        v_user_id
    )
    RETURNING id INTO v_order_id;

    -- 建立商品項目 (至少 1 件)
    INSERT INTO product_items (
        service_order_id,
        sequence_number,
        brand_name,
        style_name,
        internal_code
    )
    VALUES (
        v_order_id,
        1,
        'CHANEL',
        'Classic Flap',
        'INT-001'
    );

    RAISE NOTICE '✓ seed_service_order_test_data 完成 (order_id=%)', v_order_id;
END $$;
