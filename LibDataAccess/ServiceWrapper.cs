using LibDataAccess;
using Npgsql;
using System.Data;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddTransient<IDbConnection>((sp) => new NpgsqlConnection(connectionString));
        services.AddTransient<IDataAccess, DataAccess>();

        return services;
    }
}
