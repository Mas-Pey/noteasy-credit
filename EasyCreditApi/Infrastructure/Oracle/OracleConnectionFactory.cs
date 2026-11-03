using Oracle.ManagedDataAccess.Client;

namespace EasyCreditApi.Infrastructure.Oracle;

public sealed class OracleConnectionFactory
{
    private readonly string _connectionString;

    public OracleConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException("ConnectionStrings:Oracle belum diatur.");
    }

    // Mengembalikan koneksi dalam keadaan tertutup.
    // Dapper akan membuka & menutupnya otomatis per operasi.
    public OracleConnection Create() => new(_connectionString);
}
