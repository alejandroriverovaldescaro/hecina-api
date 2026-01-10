using HECINA.Api.Infrastructure;
using HECINA.Api.Models.Configurations;
using HECINA.Api.Repositories;
using HECINA.Api.Services;

namespace HECINA.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure database options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        
        // Configure Azure AD B2C options
        services.Configure<MicrosoftIdentityConfig>(configuration.GetSection(MicrosoftIdentityConfig.SectionName));
        
        // Configure Dataverse options
        services.Configure<DataVerseConfig>(configuration.GetSection(DataVerseConfig.SectionName));
        
        // Register infrastructure services
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        
        // Register HTTP client factory for Dataverse
        services.AddHttpClient();
        
        // Register repositories
        services.AddScoped<IMedicalExpensesRepository, MedicalExpensesRepository>();
        
        // Register application services
        services.AddScoped<IJwtHandlerService, JwtHandlerService>();
        services.AddScoped<IDataVerseRequest, DataVerseRequest>();
        
        return services;
    }
}
