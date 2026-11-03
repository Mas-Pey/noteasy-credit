using System.Data;
using Dapper;
using Dapper.Oracle;
using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure.Oracle;

namespace EasyCreditApi.Infrastructure;

public sealed class OracleCreditApplicationSpRepository : ICreditApplicationSpRepository
{
    private const string Schema = "PEY_NET";

    private readonly OracleConnectionFactory _factory;

    public OracleCreditApplicationSpRepository(OracleConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<CreditApplication>> GetAllAsync()
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_cursor", dbType: OracleMappingType.RefCursor, direction: ParameterDirection.Output);

        return await conn.QueryAsync<CreditApplication>(
            $"{Schema}.sp_get_applications",
            p,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<(string Status, string Message)> ApproveAsync(int applicationId, string approver)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_app_id", applicationId, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_approver", approver, OracleMappingType.Varchar2, ParameterDirection.Input, 100);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        await conn.ExecuteAsync(
            $"{Schema}.sp_approve_application",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;

        return (status, message);
    }

    public async Task<(IEnumerable<ResInstallmentDto> Installments, string Status, string Message)> CalculateInstallmentAsync(
        decimal loanAmount,
        int tenorMonths)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_loan_amount", loanAmount, OracleMappingType.Decimal, ParameterDirection.Input);
        p.Add("p_tenor_months", tenorMonths, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_cursor", dbType: OracleMappingType.RefCursor, direction: ParameterDirection.Output);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        var installments = await conn.QueryAsync<ResInstallmentDto>(
            $"{Schema}.sp_calculate_installment",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;

        return (installments, status, message);
    }

    public async Task<int> CreateWithInstallmentsAsync(
        int customerId,
        decimal loanAmount,
        int tenorMonths,
        decimal interestRate,
        int creditScore,
        IEnumerable<ResInstallmentDto> installments)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        using var tx = conn.BeginTransaction();

        try
        {
            // 1. Insert Header ke TRN_CREDIT_APPLICATION
            const string insertHeaderSql = $@"
                INSERT INTO {Schema}.TRN_CREDIT_APPLICATION
                    (CUSTOMER_ID, LOAN_AMOUNT, TENOR_MONTHS, INTEREST_RATE, CREDIT_SCORE, STATUS, APPLICATION_DATE)
                VALUES
                    (:CustomerId, :LoanAmount, :TenorMonths, :InterestRate, :CreditScore, 'PENDING', SYSDATE)
                RETURNING APPLICATION_ID INTO :NewApplicationId";

            var headerParams = new DynamicParameters();
            headerParams.Add("CustomerId", customerId, DbType.Int32);
            headerParams.Add("LoanAmount", loanAmount, DbType.Decimal);
            headerParams.Add("TenorMonths", tenorMonths, DbType.Int32);
            headerParams.Add("InterestRate", interestRate, DbType.Decimal);
            headerParams.Add("CreditScore", creditScore, DbType.Int32);
            headerParams.Add("NewApplicationId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(insertHeaderSql, headerParams, tx);
            var applicationId = headerParams.Get<int>("NewApplicationId");

            // 2. Insert seluruh baris cicilan ke TRN_CREDIT_INSTALLMENT
            const string insertInstallmentSql = $@"
                INSERT INTO {Schema}.TRN_CREDIT_INSTALLMENT
                    (APPLICATION_ID, INSTALLMENT_NO, DUE_DATE, INSTALLMENT_AMOUNT, STATUS)
                VALUES
                    (:ApplicationId, :InstallmentNo, :DueDate, :InstallmentAmount, :Status)";

            foreach (var inst in installments)
            {
                var instParams = new DynamicParameters();
                instParams.Add("ApplicationId", applicationId, DbType.Int32);
                instParams.Add("InstallmentNo", inst.InstallmentNo, DbType.Int32);
                instParams.Add("DueDate", inst.DueDate, DbType.Date);
                instParams.Add("InstallmentAmount", inst.InstallmentAmount, DbType.Decimal);
                instParams.Add("Status", inst.Status ?? "UNPAID", DbType.String);

                await conn.ExecuteAsync(insertInstallmentSql, instParams, tx);
            }

            // 3. Commit transaksi jika semua baris cicilan berhasil tersimpan
            tx.Commit();
            return applicationId;
        }
        catch
        {
            // Rollback jika ada satu saja cicilan yang gagal, agar tidak ada data sampah di TRN
            tx.Rollback();
            throw;
        }
    }
}
