-- =============================================================================
-- Inisialisasi Database Oracle 11g Express Edition untuk EasyCredit
-- Sesuai dengan konfigurasi dan skema nyata di Windows (Schema: PEY_NET)
-- =============================================================================

-- 1. Membuat User & Schema pey_net (jika belum ada)
DECLARE
    v_user_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_user_count FROM dba_users WHERE username = 'PEY_NET';
    IF v_user_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE USER pey_net IDENTIFIED BY "peynet"';
    END IF;
END;
/

-- 2. Menentukan Default Tablespace & Kuota Penyimpanan
ALTER USER pey_net DEFAULT TABLESPACE USERS;
ALTER USER pey_net QUOTA UNLIMITED ON USERS;

-- 3. Memberikan Hak Akses (Grants)
GRANT CREATE SESSION TO pey_net;
GRANT CREATE TABLE TO pey_net;
GRANT CREATE SEQUENCE TO pey_net;
GRANT CREATE VIEW TO pey_net;
GRANT CREATE PROCEDURE TO pey_net;
GRANT CREATE TRIGGER TO pey_net;
GRANT CREATE SYNONYM TO pey_net;

-- =============================================================================
-- 4. Tabel CUSTOMERS (Pendukung Stored Procedures Customer Oracle)
-- =============================================================================
CREATE SEQUENCE PEY_NET.CUSTOMERS_SEQ
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE TABLE PEY_NET.CUSTOMERS (
    ID         NUMBER PRIMARY KEY,
    CUST_NAME  VARCHAR2(100) NOT NULL,
    NIK        VARCHAR2(20) UNIQUE NOT NULL,
    PHONE      VARCHAR2(20),
    EMAIL      VARCHAR2(100),
    ADDRESS    VARCHAR2(255),
    CREATED_AT DATE DEFAULT SYSDATE
);

CREATE OR REPLACE TRIGGER PEY_NET.CUSTOMERS_BIR
BEFORE INSERT ON PEY_NET.CUSTOMERS
FOR EACH ROW
BEGIN
    IF :NEW.ID IS NULL THEN
        SELECT PEY_NET.CUSTOMERS_SEQ.NEXTVAL INTO :NEW.ID FROM DUAL;
    END IF;
END;
/

-- =============================================================================
-- 5. Tabel Header Pengajuan Kredit (TRN_CREDIT_APPLICATION)
-- =============================================================================
CREATE SEQUENCE PEY_NET.TRN_CREDIT_APP_SEQ
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE TABLE PEY_NET.TRN_CREDIT_APPLICATION (
    APPLICATION_ID      NUMBER,
    CUSTOMER_ID         NUMBER NOT NULL,
    LOAN_AMOUNT         NUMBER NOT NULL,
    TENOR_MONTHS        NUMBER NOT NULL,
    INTEREST_RATE       NUMBER,
    CREDIT_SCORE        NUMBER,
    STATUS              VARCHAR2(20) DEFAULT 'PENDING' NOT NULL,
    APPLICATION_DATE    DATE NOT NULL,
    APPROVED_BY         VARCHAR2(100),
    APPROVED_DATE       DATE,
    CONSTRAINT PK_TRN_CREDIT_APP PRIMARY KEY (APPLICATION_ID)
);

CREATE OR REPLACE TRIGGER PEY_NET.TRN_CREDIT_APP_BIR
BEFORE INSERT ON PEY_NET.TRN_CREDIT_APPLICATION
FOR EACH ROW
BEGIN
    IF :NEW.APPLICATION_ID IS NULL THEN
        SELECT PEY_NET.TRN_CREDIT_APP_SEQ.NEXTVAL INTO :NEW.APPLICATION_ID FROM DUAL;
    END IF;
END;
/

-- =============================================================================
-- 6. Tabel Detail Jadwal Cicilan (TRN_CREDIT_INSTALLMENT)
-- =============================================================================
CREATE SEQUENCE PEY_NET.TRN_CREDIT_INSTALLMENT_SEQ
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE TABLE PEY_NET.TRN_CREDIT_INSTALLMENT (
    INSTALLMENT_ID      NUMBER,
    APPLICATION_ID      NUMBER NOT NULL,
    INSTALLMENT_NO      NUMBER NOT NULL,
    DUE_DATE            DATE NOT NULL,
    INSTALLMENT_AMOUNT  NUMBER NOT NULL,
    STATUS              VARCHAR2(20) DEFAULT 'UNPAID' NOT NULL,
    CONSTRAINT PK_TRN_CREDIT_INSTALLMENT PRIMARY KEY (INSTALLMENT_ID),
    CONSTRAINT FK_INSTALLMENT_APPLICATION FOREIGN KEY (APPLICATION_ID) 
        REFERENCES PEY_NET.TRN_CREDIT_APPLICATION(APPLICATION_ID) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER PEY_NET.TRN_CREDIT_INSTALLMENT_BIR
BEFORE INSERT ON PEY_NET.TRN_CREDIT_INSTALLMENT
FOR EACH ROW
BEGIN
    IF :NEW.INSTALLMENT_ID IS NULL THEN
        SELECT PEY_NET.TRN_CREDIT_INSTALLMENT_SEQ.NEXTVAL INTO :NEW.INSTALLMENT_ID FROM DUAL;
    END IF;
END;
/

-- =============================================================================
-- 7. Stored Procedures Oracle
-- =============================================================================

-- SP 1: Simulasi Cicilan Angsuran (Bunga Flat 1% / Bulan)
CREATE OR REPLACE PROCEDURE PEY_NET.sp_calculate_installment(
    p_loan_amount    IN  NUMBER,
    p_tenor_months   IN  NUMBER,
    p_cursor         OUT SYS_REFCURSOR,
    out_stat         OUT VARCHAR2,
    out_mess         OUT VARCHAR2
) IS
    v_interest_rate     NUMBER := 0.01; -- Bunga 1% (0.01)/bulan
    v_monthly_principal NUMBER;
    v_monthly_interest  NUMBER;
    v_monthly_total     NUMBER;
BEGIN
    out_stat := 'ERROR';
    out_mess := NULL;

    -- Validasi pinjaman (1.000.000 s/d 50.000.000)
    IF p_loan_amount < 1000000 OR p_loan_amount > 50000000 THEN
        out_stat := 'INVALID_AMOUNT';
        out_mess := 'Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000';
        RETURN;
    END IF;

    -- Validasi tenor (6/12/24/36)
    IF p_tenor_months NOT IN (6, 12, 24, 36) THEN
        out_stat := 'INVALID_TENOR';
        out_mess := 'Tenor harus 6, 12, 24, atau 36 bulan';
        RETURN;
    END IF;

    -- Hitung pokok dan bunga per bulan
    v_monthly_principal := p_loan_amount / p_tenor_months;
    v_monthly_interest  := p_loan_amount * v_interest_rate;
    v_monthly_total     := ROUND(v_monthly_principal + v_monthly_interest, 0);

    -- Buka cursor daftar jadwal cicilan virtual
    OPEN p_cursor FOR
        SELECT LEVEL                               AS InstallmentNo,
               ADD_MONTHS(TRUNC(SYSDATE), LEVEL)   AS DueDate,
               v_monthly_total                     AS InstallmentAmount,
               'UNPAID'                            AS Status
        FROM DUAL
        CONNECT BY LEVEL <= p_tenor_months;

    out_stat := 'SUCCESS';
    out_mess := 'Perhitungan simulasi cicilan berhasil';

EXCEPTION
    WHEN OTHERS THEN
        out_stat := 'ERROR';
        out_mess := SQLERRM;
END sp_calculate_installment;
/

-- SP 2: Ambil Daftar Seluruh Pengajuan Kredit
CREATE OR REPLACE PROCEDURE PEY_NET.sp_get_applications(
    p_cursor OUT SYS_REFCURSOR
) IS
BEGIN
    OPEN p_cursor FOR
        SELECT APPLICATION_ID   AS ApplicationId,
               CUSTOMER_ID      AS CustomerId,
               LOAN_AMOUNT      AS Amount,
               TENOR_MONTHS     AS TenorMonths,
               INTEREST_RATE    AS InterestRate,
               CREDIT_SCORE     AS CreditScore,
               STATUS           AS Status,
               APPLICATION_DATE AS ApplicationDate,
               APPROVED_BY      AS ApprovedBy,
               APPROVED_DATE    AS ApprovedAt
        FROM PEY_NET.TRN_CREDIT_APPLICATION
        ORDER BY APPLICATION_ID DESC;
END sp_get_applications;
/

-- SP 3: Setujui Pengajuan Kredit (Role ADMIN)
CREATE OR REPLACE PROCEDURE PEY_NET.sp_approve_application(
    p_app_id   IN  NUMBER,
    p_approver IN  VARCHAR2,
    out_stat   OUT VARCHAR2,
    out_mess   OUT VARCHAR2
) IS
    v_exists NUMBER;
BEGIN
    out_stat := 'ERROR';
    out_mess := NULL;

    SELECT COUNT(*) INTO v_exists
    FROM PEY_NET.TRN_CREDIT_APPLICATION
    WHERE APPLICATION_ID = p_app_id;

    IF v_exists = 0 THEN
        out_stat := 'NOT_FOUND';
        out_mess := 'Application not found';
        RETURN;
    END IF;

    UPDATE PEY_NET.TRN_CREDIT_APPLICATION
       SET STATUS        = 'APPROVED',
           APPROVED_BY   = p_approver,
           APPROVED_DATE = SYSDATE
     WHERE APPLICATION_ID = p_app_id
       AND STATUS IN ('SUBMITTED', 'PENDING');

    IF SQL%ROWCOUNT = 0 THEN
        out_stat := 'INVALID_STATUS';
        out_mess := 'Application is not in SUBMITTED/PENDING status';
        RETURN;
    END IF;

    COMMIT;
    out_stat := 'SUCCESS';
    out_mess := 'Application approved';
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        out_stat := 'ERROR';
        out_mess := SQLERRM;
END sp_approve_application;
/

-- SP 4: Tolak Pengajuan Kredit (Role ADMIN)
CREATE OR REPLACE PROCEDURE PEY_NET.sp_reject_application(
    p_app_id   IN  NUMBER,
    p_approver IN  VARCHAR2,
    out_stat   OUT VARCHAR2,
    out_mess   OUT VARCHAR2
) IS
    v_exists NUMBER;
BEGIN
    out_stat := 'ERROR';
    out_mess := NULL;

    SELECT COUNT(*) INTO v_exists
    FROM PEY_NET.TRN_CREDIT_APPLICATION
    WHERE APPLICATION_ID = p_app_id;

    IF v_exists = 0 THEN
        out_stat := 'NOT_FOUND';
        out_mess := 'Application not found';
        RETURN;
    END IF;

    UPDATE PEY_NET.TRN_CREDIT_APPLICATION
       SET STATUS        = 'REJECTED',
           APPROVED_BY   = p_approver,
           APPROVED_DATE = SYSDATE
     WHERE APPLICATION_ID = p_app_id
       AND STATUS IN ('SUBMITTED', 'PENDING');

    IF SQL%ROWCOUNT = 0 THEN
        out_stat := 'INVALID_STATUS';
        out_mess := 'Application is not in SUBMITTED/PENDING status';
        RETURN;
    END IF;

    COMMIT;
    out_stat := 'SUCCESS';
    out_mess := 'Application rejected successfully';
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        out_stat := 'ERROR';
        out_mess := SQLERRM;
END sp_reject_application;
/

-- SP 5: Ambil Single Customer
CREATE OR REPLACE PROCEDURE PEY_NET.sp_get_customer(
    p_id     IN  NUMBER,
    p_cursor OUT SYS_REFCURSOR
) IS
BEGIN
    OPEN p_cursor FOR
        SELECT ID         AS CustomerId,
               CUST_NAME  AS Name,
               NIK        AS Nik,
               PHONE      AS Phone,
               EMAIL      AS Email,
               ADDRESS    AS Address,
               CREATED_AT AS CreatedAt
        FROM PEY_NET.CUSTOMERS
        WHERE ID = p_id;
END sp_get_customer;
/

-- SP 6: Ambil Seluruh Customers
CREATE OR REPLACE PROCEDURE PEY_NET.sp_get_customers(
    p_cursor OUT SYS_REFCURSOR
) IS
BEGIN
    OPEN p_cursor FOR
        SELECT ID         AS CustomerId,
               CUST_NAME  AS Name,
               NIK        AS Nik,
               PHONE      AS Phone,
               EMAIL      AS Email,
               ADDRESS    AS Address,
               CREATED_AT AS CreatedAt
        FROM PEY_NET.CUSTOMERS 
        ORDER BY ID;
END sp_get_customers;
/

-- SP 7: Tambah Customer Baru
CREATE OR REPLACE PROCEDURE PEY_NET.sp_create_customer(
    p_name    IN  VARCHAR2,
    p_nik     IN  VARCHAR2,
    p_phone   IN  VARCHAR2,
    p_email   IN  VARCHAR2,
    p_address IN  VARCHAR2,
    out_id    OUT NUMBER,
    out_stat  OUT VARCHAR2,
    out_mess  OUT VARCHAR2
) IS
    v_count NUMBER;
BEGIN
    out_id   := 0;
    out_stat := 'ERROR';
    out_mess := NULL;

    SELECT COUNT(*) INTO v_count
    FROM PEY_NET.CUSTOMERS
    WHERE NIK = p_nik;

    IF v_count > 0 THEN
        out_stat := 'DUPLICATE';
        out_mess := 'NIK sudah terdaftar';
        RETURN;
    END IF;

    INSERT INTO PEY_NET.CUSTOMERS
        (CUST_NAME, NIK, PHONE, EMAIL, ADDRESS, CREATED_AT)
    VALUES
        (p_name, p_nik, p_phone, p_email, p_address, SYSDATE)
    RETURNING ID INTO out_id;

    COMMIT;
    out_stat := 'SUCCESS';
    out_mess := 'Customer created';
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        out_id   := 0;
        out_stat := 'ERROR';
        out_mess := SQLERRM;
END sp_create_customer;
/
