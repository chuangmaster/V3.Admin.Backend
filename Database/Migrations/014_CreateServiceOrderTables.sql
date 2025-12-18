-- 建立服務單管理模組資料表
-- Migration: 014
-- Date: 2025-12-18
-- Description: Create customers, service_orders, product_items, attachments, signature_records, attachment_view_logs, dropbox_sign_webhook_events

-- 需要 gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ===== customers (客戶) =====
CREATE TABLE IF NOT EXISTS customers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20) NOT NULL,
    email VARCHAR(100),
    id_number VARCHAR(10) NOT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1,

    CONSTRAINT uq_customers_id_number UNIQUE (id_number),
    CONSTRAINT chk_customers_version_positive CHECK (version >= 1)
);

CREATE INDEX IF NOT EXISTS idx_customers_name_not_deleted
    ON customers(name)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_customers_phone_number_not_deleted
    ON customers(phone_number)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_customers_email_not_deleted
    ON customers(email)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_customers_id_number_not_deleted
    ON customers(id_number)
    WHERE is_deleted = false;

-- ===== service_orders (服務單) =====
CREATE TABLE IF NOT EXISTS service_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    service_date DATE NOT NULL DEFAULT CURRENT_DATE,
    sequence_number INT NOT NULL,
    order_type VARCHAR(20) NOT NULL,
    order_source VARCHAR(20) NOT NULL,

    customer_id UUID NOT NULL,
    total_amount NUMERIC(10, 2) NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',

    -- 寄賣專屬欄位
    consignment_start_date DATE,
    consignment_end_date DATE,
    renewal_option VARCHAR(50),

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    version INT NOT NULL DEFAULT 1,

    -- 注意：PostgreSQL 的 generated column 需使用 immutable expression。
    -- TO_CHAR(date, text) 在多數版本屬於 stable，因此改由觸發器於 INSERT 前生成。
    order_number VARCHAR(20) NOT NULL,

    CONSTRAINT fk_service_orders_customer_id FOREIGN KEY (customer_id) REFERENCES customers(id),
    CONSTRAINT uq_service_orders_order_number UNIQUE (order_number),
    CONSTRAINT uq_service_orders_date_type_seq UNIQUE (service_date, order_type, sequence_number),

    CONSTRAINT chk_service_orders_order_type CHECK (order_type IN ('BUYBACK', 'CONSIGNMENT')),
    CONSTRAINT chk_service_orders_order_source CHECK (order_source IN ('OFFLINE', 'ONLINE')),
    CONSTRAINT chk_service_orders_status CHECK (status IN ('PENDING', 'COMPLETED', 'TERMINATED')),
    CONSTRAINT chk_service_orders_version_positive CHECK (version >= 1),

    CONSTRAINT chk_service_orders_consignment_required CHECK (
        (order_type = 'CONSIGNMENT' AND consignment_start_date IS NOT NULL AND consignment_end_date IS NOT NULL)
        OR (order_type = 'BUYBACK' AND consignment_start_date IS NULL AND consignment_end_date IS NULL)
    ),
    CONSTRAINT chk_service_orders_consignment_date_range CHECK (
        consignment_end_date IS NULL OR consignment_end_date > consignment_start_date
    ),
    CONSTRAINT chk_service_orders_renewal_option CHECK (
        renewal_option IS NULL OR renewal_option IN ('AUTO_RETRIEVE', 'AUTO_DISCOUNT_10', 'DISCUSS')
    )
);

CREATE INDEX IF NOT EXISTS idx_service_orders_order_number_not_deleted
    ON service_orders(order_number)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_service_orders_customer_id_not_deleted
    ON service_orders(customer_id)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_service_orders_order_type_not_deleted
    ON service_orders(order_type)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_service_orders_status_not_deleted
    ON service_orders(status)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_service_orders_service_date_desc
    ON service_orders(service_date DESC);

CREATE INDEX IF NOT EXISTS idx_service_orders_created_at_desc
    ON service_orders(created_at DESC);

-- ===== product_items (商品項目) =====
CREATE TABLE IF NOT EXISTS product_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_order_id UUID NOT NULL,
    sequence_number INT NOT NULL,
    brand_name VARCHAR(100) NOT NULL,
    style_name VARCHAR(100) NOT NULL,
    internal_code VARCHAR(50),
    accessories JSONB,
    defects JSONB,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,

    CONSTRAINT fk_product_items_service_order_id FOREIGN KEY (service_order_id) REFERENCES service_orders(id),
    CONSTRAINT chk_product_items_sequence_number CHECK (sequence_number BETWEEN 1 AND 4),
    CONSTRAINT uq_product_items_order_sequence UNIQUE (service_order_id, sequence_number)
);

CREATE INDEX IF NOT EXISTS idx_product_items_service_order_id
    ON product_items(service_order_id);

-- ===== attachments (附件) =====
CREATE TABLE IF NOT EXISTS attachments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_order_id UUID NOT NULL,
    attachment_type VARCHAR(50) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    blob_path VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(100) NOT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by UUID,

    CONSTRAINT fk_attachments_service_order_id FOREIGN KEY (service_order_id) REFERENCES service_orders(id),
    CONSTRAINT chk_attachments_file_size CHECK (file_size > 0 AND file_size <= 10485760)
);

CREATE INDEX IF NOT EXISTS idx_attachments_service_order_id_not_deleted
    ON attachments(service_order_id)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_attachments_attachment_type_not_deleted
    ON attachments(attachment_type)
    WHERE is_deleted = false;

-- ===== signature_records (簽名記錄) =====
CREATE TABLE IF NOT EXISTS signature_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_order_id UUID NOT NULL,
    document_type VARCHAR(50) NOT NULL,
    signature_type VARCHAR(20) NOT NULL,

    signature_data TEXT,

    dropbox_sign_request_id VARCHAR(100),
    dropbox_sign_status VARCHAR(20),
    dropbox_sign_url TEXT,

    signer_name VARCHAR(100) NOT NULL,
    signed_at TIMESTAMP,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT fk_signature_records_service_order_id FOREIGN KEY (service_order_id) REFERENCES service_orders(id),
    CONSTRAINT chk_signature_records_document_type CHECK (document_type IN ('BUYBACK_CONTRACT', 'ONE_TIME_TRADE', 'CONSIGNMENT_CONTRACT')),
    CONSTRAINT chk_signature_records_signature_type CHECK (signature_type IN ('OFFLINE', 'ONLINE')),
    CONSTRAINT chk_signature_records_dropbox_status CHECK (
        dropbox_sign_status IS NULL OR dropbox_sign_status IN ('PENDING', 'SIGNED', 'DECLINED', 'EXPIRED')
    )
);

CREATE INDEX IF NOT EXISTS idx_signature_records_service_order_id
    ON signature_records(service_order_id);

CREATE INDEX IF NOT EXISTS idx_signature_records_dropbox_sign_request_id
    ON signature_records(dropbox_sign_request_id)
    WHERE dropbox_sign_request_id IS NOT NULL;

-- ===== attachment_view_logs (附件查看日誌) =====
CREATE TABLE IF NOT EXISTS attachment_view_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    attachment_id UUID NOT NULL,
    service_order_id UUID NOT NULL,
    viewed_by UUID NOT NULL,
    viewed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    operation_type VARCHAR(20) NOT NULL,
    ip_address VARCHAR(45),

    CONSTRAINT fk_attachment_view_logs_attachment_id FOREIGN KEY (attachment_id) REFERENCES attachments(id),
    CONSTRAINT fk_attachment_view_logs_service_order_id FOREIGN KEY (service_order_id) REFERENCES service_orders(id),
    CONSTRAINT chk_attachment_view_logs_operation_type CHECK (operation_type IN ('VIEW', 'DOWNLOAD'))
);

CREATE INDEX IF NOT EXISTS idx_attachment_view_logs_attachment_id
    ON attachment_view_logs(attachment_id);

CREATE INDEX IF NOT EXISTS idx_attachment_view_logs_service_order_id
    ON attachment_view_logs(service_order_id);

CREATE INDEX IF NOT EXISTS idx_attachment_view_logs_viewed_by
    ON attachment_view_logs(viewed_by);

CREATE INDEX IF NOT EXISTS idx_attachment_view_logs_viewed_at_desc
    ON attachment_view_logs(viewed_at DESC);

-- ===== dropbox_sign_webhook_events (Webhook 防重複) =====
CREATE TABLE IF NOT EXISTS dropbox_sign_webhook_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_hash VARCHAR(64) NOT NULL,
    processed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_dropbox_sign_webhook_events_event_hash UNIQUE (event_hash)
);

CREATE INDEX IF NOT EXISTS idx_dropbox_sign_webhook_events_created_at_desc
    ON dropbox_sign_webhook_events(created_at DESC);

-- ===== updated_at triggers =====
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_proc WHERE proname = 'update_updated_at_column') THEN
        -- customers
        IF NOT EXISTS (
            SELECT 1
            FROM pg_trigger
            WHERE tgname = 'update_customers_updated_at'
        ) THEN
            CREATE TRIGGER update_customers_updated_at
                BEFORE UPDATE ON customers
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();
        END IF;

        -- service_orders
        IF NOT EXISTS (
            SELECT 1
            FROM pg_trigger
            WHERE tgname = 'update_service_orders_updated_at'
        ) THEN
            CREATE TRIGGER update_service_orders_updated_at
                BEFORE UPDATE ON service_orders
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();
        END IF;

        -- product_items
        IF NOT EXISTS (
            SELECT 1
            FROM pg_trigger
            WHERE tgname = 'update_product_items_updated_at'
        ) THEN
            CREATE TRIGGER update_product_items_updated_at
                BEFORE UPDATE ON product_items
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();
        END IF;

        -- attachments
        IF NOT EXISTS (
            SELECT 1
            FROM pg_trigger
            WHERE tgname = 'update_attachments_updated_at'
        ) THEN
            CREATE TRIGGER update_attachments_updated_at
                BEFORE UPDATE ON attachments
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();
        END IF;

        -- signature_records
        IF NOT EXISTS (
            SELECT 1
            FROM pg_trigger
            WHERE tgname = 'update_signature_records_updated_at'
        ) THEN
            CREATE TRIGGER update_signature_records_updated_at
                BEFORE UPDATE ON signature_records
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();
        END IF;

        RAISE NOTICE '✓ updated_at triggers 建立成功';
    ELSE
        RAISE NOTICE 'ℹ 找不到 update_updated_at_column()，略過 updated_at triggers 建立';
    END IF;
END $$;
