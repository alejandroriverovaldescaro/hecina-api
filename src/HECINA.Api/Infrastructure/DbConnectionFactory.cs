using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace HECINA.Api.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseOptions _options;

    public DbConnectionFactory(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_options.ConnectionString);
    }
}
