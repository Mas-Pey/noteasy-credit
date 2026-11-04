-- =============================================================================
-- Inisialisasi Database PostgreSQL untuk EasyCredit
-- Sesuai dengan konfigurasi dan flow nyata di Docker WSL
-- =============================================================================

-- 1. Buat Role easycredit_user (jika belum ada)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'easycredit_user') THEN
        CREATE ROLE easycredit_user LOGIN PASSWORD 'easycredit_pass';
    ELSE
        ALTER ROLE easycredit_user WITH LOGIN PASSWORD 'easycredit_pass';
    END IF;
END
$$;

-- 2. Buat Database easycredit jika belum ada
SELECT 'CREATE DATABASE easycredit OWNER easycredit_user'
WHERE NOT EXISTS (
    SELECT 1 FROM pg_database WHERE datname = 'easycredit'
)\gexec

-- 3. Berikan hak akses database ke easycredit_user
GRANT ALL PRIVILEGES ON DATABASE easycredit TO easycredit_user;

-- 4. Pindah ke database easycredit
\connect easycredit

-- -----------------------------------------------------------------------------
-- 5. Tabel mst_user (Skema Awal)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mst_user (
    user_id       SERIAL       PRIMARY KEY,
    name          VARCHAR(100) NOT NULL,
    email         VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role          VARCHAR(20)  NOT NULL,
    created_at    TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- -----------------------------------------------------------------------------
-- 6. Tabel mst_customer (Skema Awal sebelum Soft Delete)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS mst_customer (
    customer_id SERIAL       PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    nik         VARCHAR(20)  NOT NULL UNIQUE,
    phone       VARCHAR(20),
    email       VARCHAR(100),
    address     VARCHAR(255),
    created_at  TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- -----------------------------------------------------------------------------
-- 7. Seed Users
-- Password: admin123 (ADMIN), staff123 (STAFF) - BCrypt Hash
-- -----------------------------------------------------------------------------
INSERT INTO mst_user (name, email, password_hash, role) VALUES
    ('Administrator', 'admin@easycredit.test',
     '$2a$11$ACDxIbAkUhjG6e8vBa5PjOdXir3PmYFnj4REMxw63r7jdCiok7l2m', 'ADMIN'),
    ('Staff CS', 'staff@easycredit.test',
     '$2a$11$r7blHbOXR8wjFYaMor0vp.P.nRzMQg870/q1lGH05razxT.1Mahmm', 'STAFF')
ON CONFLICT (email) DO NOTHING;

-- -----------------------------------------------------------------------------
-- 8. Seed Customers
-- -----------------------------------------------------------------------------
INSERT INTO mst_customer (name, nik, phone, email, address) VALUES
    ('Budi Santoso', '3201010101010001', '081234567001', 'budi@mail.test', 'Jl. Merdeka No. 1, Jakarta'),
    ('Siti Aminah',  '3201010101010002', '081234567002', 'siti@mail.test', 'Jl. Sudirman No. 2, Bandung'),
    ('Andi Wijaya',  '3201010101010003', '081234567003', 'andi@mail.test', 'Jl. Diponegoro No. 3, Surabaya')
ON CONFLICT (nik) DO NOTHING;

-- -----------------------------------------------------------------------------
-- 9. Hak Akses Schema & Objek
-- -----------------------------------------------------------------------------
GRANT ALL PRIVILEGES ON ALL TABLES    IN SCHEMA public TO easycredit_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO easycredit_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO easycredit_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO easycredit_user;

-- =============================================================================
-- 10. SOFT DELETE MIGRATION (+ 1 column)
-- Penambahan kolom is_deleted setelah fitur Soft Delete diimplementasikan
-- =============================================================================
ALTER TABLE mst_customer 
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
