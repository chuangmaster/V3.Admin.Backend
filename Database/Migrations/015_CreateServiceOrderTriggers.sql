-- 建立服務單序號生成與觸發器
-- Migration: 015
-- Date: 2025-12-18
-- Description: Daily reset sequences + generate_daily_service_order_number() + trg_service_orders_sequence

-- 需要 gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ===== 序號狀態表 (每日重置用) =====
CREATE TABLE IF NOT EXISTS service_order_sequence_state (
    order_type VARCHAR(20) PRIMARY KEY,
    last_reset_date DATE NOT NULL
);

-- ===== PostgreSQL Sequences (依服務單類型分開) =====
CREATE SEQUENCE IF NOT EXISTS service_order_buyback_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 999;
CREATE SEQUENCE IF NOT EXISTS service_order_consignment_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 999;

-- ===== 取得當日序號 (每日重置) =====
CREATE OR REPLACE FUNCTION generate_daily_service_order_number(
    p_order_type VARCHAR(20),
    p_service_date DATE DEFAULT CURRENT_DATE
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_last_date DATE;
    v_next_seq INT;
    v_seq_name TEXT;
BEGIN
    IF p_order_type NOT IN ('BUYBACK', 'CONSIGNMENT') THEN
        RAISE EXCEPTION '不支援的 order_type: %', p_order_type;
    END IF;

    v_seq_name := CASE
        WHEN p_order_type = 'BUYBACK' THEN 'service_order_buyback_seq'
        ELSE 'service_order_consignment_seq'
    END;

    -- 以 order_type 做鎖，避免同一類型序號競態
    PERFORM pg_advisory_xact_lock(hashtext('service_order_seq_' || p_order_type));

    SELECT last_reset_date
    INTO v_last_date
    FROM service_order_sequence_state
    WHERE order_type = p_order_type;

    IF v_last_date IS NULL THEN
        INSERT INTO service_order_sequence_state(order_type, last_reset_date)
        VALUES (p_order_type, p_service_date)
        ON CONFLICT (order_type) DO NOTHING;

        -- 初次建立時，將 sequence 重置到 1
        EXECUTE format('SELECT setval(%L, 1, false)', v_seq_name);
    ELSIF v_last_date <> p_service_date THEN
        UPDATE service_order_sequence_state
        SET last_reset_date = p_service_date
        WHERE order_type = p_order_type;

        EXECUTE format('SELECT setval(%L, 1, false)', v_seq_name);
    END IF;

    EXECUTE format('SELECT nextval(%L)::INT', v_seq_name)
    INTO v_next_seq;

    IF v_next_seq > 999 THEN
        RAISE EXCEPTION '當日服務單序號已達上限 (999)';
    END IF;

    RETURN v_next_seq;
END;
$$;

-- ===== 觸發器：插入前自動填序號 =====
CREATE OR REPLACE FUNCTION trigger_set_service_order_sequence()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.service_date IS NULL THEN
        NEW.service_date := CURRENT_DATE;
    END IF;

    IF NEW.sequence_number IS NULL OR NEW.sequence_number = 0 THEN
        NEW.sequence_number := generate_daily_service_order_number(NEW.order_type, NEW.service_date);
    END IF;

    IF NEW.order_number IS NULL OR NEW.order_number = '' THEN
        NEW.order_number :=
            (CASE
                WHEN UPPER(NEW.order_type) = 'BUYBACK' THEN 'BS'
                WHEN UPPER(NEW.order_type) = 'CONSIGNMENT' THEN 'CS'
                ELSE 'SO'
            END)
            || TO_CHAR(NEW.service_date, 'YYYYMMDD')
            || LPAD(NEW.sequence_number::TEXT, 3, '0');
    END IF;

    RETURN NEW;
END;
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_trigger
        WHERE tgname = 'trg_service_orders_sequence'
    ) THEN
        CREATE TRIGGER trg_service_orders_sequence
            BEFORE INSERT ON service_orders
            FOR EACH ROW
            EXECUTE FUNCTION trigger_set_service_order_sequence();
    END IF;

    RAISE NOTICE '✓ trg_service_orders_sequence 建立成功';
END $$;
