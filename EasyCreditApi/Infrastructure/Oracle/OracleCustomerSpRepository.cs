using System.Data;
using Dapper;
using Dapper.Oracle;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure.Oracle;

namespace EasyCreditApi.Infrastructure;

public sealed class OracleCustomerSpRepository : ICustomerSpRepository
{
    private const string Schema = "PEY_NET";

    private readonly OracleConnectionFactory _factory;

    public OracleCustomerSpRepository(OracleConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_cursor", dbType: OracleMappingType.RefCursor, direction: ParameterDirection.Output);

        return await conn.QueryAsync<Customer>(
            $"{Schema}.sp_get_customers",
            p,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Customer?> GetByIdAsync(int customerId)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_id", customerId, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_cursor", dbType: OracleMappingType.RefCursor, direction: ParameterDirection.Output);

        return await conn.QueryFirstOrDefaultAsync<Customer>(
            $"{Schema}.sp_get_customer",
            p,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<(string Status, string Message, int CustomerId)> CreateAsync(Customer customer)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_name", customer.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 100);
        p.Add("p_nik", customer.Nik, OracleMappingType.Varchar2, ParameterDirection.Input, 20);
        p.Add("p_phone", DbValue(customer.Phone), OracleMappingType.Varchar2, ParameterDirection.Input, 20);
        p.Add("p_email", DbValue(customer.Email), OracleMappingType.Varchar2, ParameterDirection.Input, 100);
        p.Add("p_address", DbValue(customer.Address), OracleMappingType.Varchar2, ParameterDirection.Input, 100);
        p.Add("out_id", dbType: OracleMappingType.Int32, direction: ParameterDirection.Output);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        await conn.ExecuteAsync(
            $"{Schema}.sp_create_customer",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;
        var id = p.Get<int>("out_id");

        if (status == "SUCCESS")
            customer.CustomerId = id;

        return (status, message, id);
    }

    private static object DbValue(string? value) => value ?? (object)DBNull.Value;
}
