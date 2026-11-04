# EasyCredit — Online Credit Application System
> **Bootcamp .NET Batch 19 Final Project**

Sistem pengajuan kredit online berbasis **ASP.NET Core** dengan arsitektur **Dual-Database (PostgreSQL & Oracle)** yang memisahkan otentikasi nasabah dan transaksi pinjaman secara independen.

---

## 1. Arsitektur Singkat

- **Frontend MVC (Port 5019)**: ASP.NET Core MVC (Razor), jQuery AJAX, Anti-CSRF (`[ValidateAntiForgeryToken]`), Responsive UI.
- **Backend API (Port 5052)**: ASP.NET Core Web API, JWT Authentication (`ADMIN` & `STAFF`), Global Exception Middleware (`ApiResponse<T>`).
- **PostgreSQL**: Otentikasi (`mst_user`) dan master nasabah (`mst_customer` dengan *Soft Delete* via Global Query Filter EF Core).
- **Oracle**: Transaksi kredit (`TRN_CREDIT_APPLICATION`) dan jadwal cicilan (`TRN_CREDIT_INSTALLMENT`) via Dapper & Stored Procedures.
- **Integritas Relasi**: Tidak ada *Cross-DB Foreign Key*. Integritas relasi `CUSTOMER_ID` digabungkan di memori C# Service layer.

---

## 2. Konfigurasi (`appsettings.json`)

Sesuaikan connection string di `EasyCreditApi/appsettings.json` (kredensial disamarkan):

```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=<ORACLE_USER>;Password=<ORACLE_PASSWORD>;Data Source=<ORACLE_HOST>:<PORT>/<SERVICE_NAME>",
    "Postgres": "Host=<POSTGRES_HOST>;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>"
  },
  "Jwt": {
    "Key": "<SECRET_KEY_MIN_32_CHARS>",
    "Issuer": "EasyCredit.Api",
    "Audience": "EasyCredit.Client",
    "ExpiryMinutes": 60
  }
}
```

---

## 3. Stored Procedures (Oracle Schema `PEY_NET`)

Jalankan seluruh Stored Procedure berikut di Oracle:

```sql
-- 1. Simulasi Angsuran Cicilan (Bunga Flat 1% / Bulan)
CREATE OR REPLACE PROCEDURE PEY_NET.sp_calculate_installment(
    p_loan_amount    IN  NUMBER,
    p_tenor_months   IN  NUMBER,
    p_cursor         OUT SYS_REFCURSOR,
    out_stat         OUT VARCHAR2,
    out_mess         OUT VARCHAR2
) IS
    v_interest_rate     NUMBER := 0.01;
    v_monthly_principal NUMBER;
    v_monthly_interest  NUMBER;
    v_monthly_total     NUMBER;
BEGIN
    out_stat := 'ERROR';
    out_mess := NULL;

    IF p_loan_amount < 1000000 OR p_loan_amount > 50000000 THEN
        out_stat := 'INVALID_AMOUNT';
        out_mess := 'Nominal pinjaman harus antara Rp 1.000.000 s/d Rp 50.000.000';
        RETURN;
    END IF;

    IF p_tenor_months NOT IN (6, 12, 24, 36) THEN
        out_stat := 'INVALID_TENOR';
        out_mess := 'Tenor harus 6, 12, 24, atau 36 bulan';
        RETURN;
    END IF;

    v_monthly_principal := p_loan_amount / p_tenor_months;
    v_monthly_interest  := p_loan_amount * v_interest_rate;
    v_monthly_total     := ROUND(v_monthly_principal + v_monthly_interest, 0);

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

-- 2. Ambil Semua Pengajuan Kredit
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

-- 3. Setujui Pengajuan Kredit (Role ADMIN)
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

-- 4. Tolak Pengajuan Kredit (Role ADMIN)
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

-- 5. Ambil Satu Customer (Oracle)
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

-- 6. Ambil Semua Customer (Oracle)
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

-- 7. Tambah Customer Baru (Oracle)
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
```

---

## 4. Cara Menjalankan Aplikasi

Jalankan perintah berikut di dua terminal terpisah:

```bash
# 1. Jalankan Backend Web API (Port 5052, Swagger: http://localhost:5052/swagger)
cd EasyCreditApi
dotnet run

# 2. Jalankan Frontend MVC (Port 5019: http://localhost:5019)
cd EasyCreditFrontend
dotnet run
```

### Kredensial Akun Login

| Role | Email | Password | Akses |
| :--- | :--- | :--- | :--- |
| **ADMIN** | `admin@easycredit.test` | `admin123` | Full Akses (Master Nasabah, Pengajuan, **Approve & Reject**) |
| **STAFF** | `staff@easycredit.test` | `staff123` | Master Nasabah, Pengajuan Kredit (Tanpa Approval) |
