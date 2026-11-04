using System.Data;
using Dapper;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure.Oracle;

namespace EasyCreditApi.Infrastructure;

public sealed class OracleCustomerRepository : ICustomerRepository
{
    // Schema perlu di-qualify karena user DB saat ini adalah SYSTEM.
    // Kalau nanti connect sebagai EASYCREDIT, nama ini tetap valid.
    private const string Table = "PEY_NET.CUSTOMERS";

    private const string SelectColumns = """
        ID         AS CustomerId,
        CUST_NAME  AS Name,
        NIK        AS Nik,
        PHONE      AS Phone,
        EMAIL      AS Email,
        ADDRESS    AS Address,
        CREATED_AT AS CreatedAt
        """;

    private readonly OracleConnectionFactory _factory;

    public OracleCustomerRepository(OracleConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Customer>> GetAllAsync(string? keyword)
    {
        await using var conn = _factory.Create();

        var sql = $"SELECT {SelectColumns} FROM {Table}";
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            sql += @" WHERE UPPER(CUST_NAME) LIKE :Pattern ESCAPE '\'";
            p.Add("Pattern", ToLikePattern(keyword), DbType.String);
        }

        sql += " ORDER BY ID";
        return await conn.QueryAsync<Customer>(sql, p);
    }

    public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(
        string? keyword, int page, int limit)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);

        await using var conn = _factory.Create();

        var where = "";
        if (!string.IsNullOrWhiteSpace(keyword))
            where = @" WHERE UPPER(CUST_NAME) LIKE :Pattern ESCAPE '\'";

        var countParams = new DynamicParameters();
        var pageParams = new DynamicParameters();

        if (where.Length > 0)
        {
            var pattern = ToLikePattern(keyword!);
            countParams.Add("Pattern", pattern, DbType.String);
            pageParams.Add("Pattern", pattern, DbType.String);
        }

        pageParams.Add("Offset", (page - 1) * limit, DbType.Int32);
        pageParams.Add("Limit", limit, DbType.Int32);

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM {Table}{where}", countParams);

        var items = await conn.QueryAsync<Customer>(
            $"""
             SELECT {SelectColumns}
             FROM {Table}{where}
             ORDER BY ID
             OFFSET :Offset ROWS FETCH NEXT :Limit ROWS ONLY
             """, pageParams);

        return (items, total);
    }

    public async Task<Customer?> GetByIdAsync(int customerId, bool includeDeleted = false)
    {
        await using var conn = _factory.Create();

        return await conn.QueryFirstOrDefaultAsync<Customer>(
            $"SELECT {SelectColumns} FROM {Table} WHERE ID = :CustomerId",
            new { CustomerId = customerId });
    }

    public async Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<int> customerIds)
    {
        var ids = customerIds.Distinct().ToArray();

        if (ids.Length == 0)
            return Array.Empty<Customer>();

        if (ids.Length > 1000)
            throw new ArgumentException("Maksimal 1000 ID per query; pecah per batch.");

        await using var conn = _factory.Create();

        return await conn.QueryAsync<Customer>(
            $"SELECT {SelectColumns} FROM {Table} WHERE ID IN :Ids ORDER BY ID",
            new { Ids = ids });
    }

    public async Task<bool> ExistsAsync(int customerId)
    {
        await using var conn = _factory.Create();

        var count = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM {Table} WHERE ID = :CustomerId",
            new { CustomerId = customerId });

        return count > 0;
    }

    public async Task<bool> NikExistsAsync(string nik, int? excludeCustomerId = null)
    {
        await using var conn = _factory.Create();

        var p = new DynamicParameters();
        p.Add("Nik", nik, DbType.String);
        p.Add("ExcludeId", excludeCustomerId, DbType.Int32);

        var count = await conn.ExecuteScalarAsync<int>(
            $"""
             SELECT COUNT(1)
             FROM {Table}
             WHERE NIK = :Nik
               AND (:ExcludeId IS NULL OR ID <> :ExcludeId)
             """, p);

        return count > 0;
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        await using var conn = _factory.Create();

        var p = new DynamicParameters();
        p.Add("Name", customer.Name, DbType.String);
        p.Add("Nik", customer.Nik, DbType.String);
        p.Add("Phone", customer.Phone, DbType.String);
        p.Add("Email", customer.Email, DbType.String);
        p.Add("Address", customer.Address, DbType.String);
        p.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("CreatedAt", dbType: DbType.DateTime, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            $"""
             INSERT INTO {Table} (CUST_NAME, NIK, PHONE, EMAIL, ADDRESS, CREATED_AT)
             VALUES (:Name, :Nik, :Phone, :Email, :Address, SYSDATE)
             RETURNING ID, CREATED_AT INTO :NewId, :CreatedAt
             """, p);

        customer.CustomerId = p.Get<int>("NewId");
        customer.CreatedAt = p.Get<DateTime>("CreatedAt");
        return customer.CustomerId;
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        await using var conn = _factory.Create();

        var affected = await conn.ExecuteAsync(
            $"""
             UPDATE {Table}
                SET CUST_NAME = :Name,
                    NIK       = :Nik,
                    PHONE     = :Phone,
                    EMAIL     = :Email,
                    ADDRESS   = :Address
              WHERE ID = :CustomerId
             """,
            new
            {
                customer.Name,
                customer.Nik,
                customer.Phone,
                customer.Email,
                customer.Address,
                customer.CustomerId
            });

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int customerId)
    {
        await using var conn = _factory.Create();

        var affected = await conn.ExecuteAsync(
            $"DELETE FROM {Table} WHERE ID = :CustomerId",
            new { CustomerId = customerId });

        return affected > 0;
    }

    private static string ToLikePattern(string keyword) =>
        "%" + keyword.Trim().ToUpperInvariant()
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_") + "%";
}