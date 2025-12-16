using HECINA.Api.Infrastructure;
using HECINA.Api.Repositories;

namespace HECINA.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure database options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        
        // Register infrastructure services
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        
        // Register repositories
        services.AddScoped<IMedicalExpensesRepository, MedicalExpensesRepository>();
        
        return services;
    }
}
