using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace EasyCreditApi.Infrastructure.Oracle;

public sealed class OracleTransactionDemo
{
    private const string Table = "EASYCREDIT.CUSTOMERS";

    private readonly OracleConnectionFactory _factory;

    public OracleTransactionDemo(OracleConnectionFactory factory) => _factory = factory;

    public async Task<int> InsertTwoAsync(bool failOnSecond)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        using var tx = conn.BeginTransaction();

        try
        {
            var firstId = await InsertAsync(conn, tx, "Tx Satu", "9999999999999991");

            if (failOnSecond)
                throw new InvalidOperationException("Simulasi gagal setelah insert pertama.");

            await InsertAsync(conn, tx, "Tx Dua", "9999999999999992");

            tx.Commit();
            return firstId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static async Task<int> InsertAsync(
        OracleConnection conn,
        IDbTransaction tx,
        string name,
        string nik)
    {
        var p = new DynamicParameters();
        p.Add("Name", name, DbType.String);
        p.Add("Nik", nik, DbType.String);
        p.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            $"""
             INSERT INTO {Table} (CUST_NAME, NIK, CREATED_AT)
             VALUES (:Name, :Nik, SYSDATE)
             RETURNING ID INTO :NewId
             """, p, tx);

        return p.Get<int>("NewId");
    }
}