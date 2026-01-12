using HECINA.Api.Models.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HECINA.Api.Authentication;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Check if AzureAdB2C configuration exists
        var azureB2CSection = configuration.GetSection(MicrosoftIdentityConfig.SectionName);
        var useAzureB2C = azureB2CSection.Exists() && 
                          !string.IsNullOrEmpty(azureB2CSection["Instance"]);

        if (useAzureB2C)
        {
            // Configure Azure AD B2C JWT authentication
            services.AddAuthentication()
                .AddJwtBearer("Bearer", options =>
                {
                    var azureB2CConfig = azureB2CSection.Get<MicrosoftIdentityConfig>();
                    
                    if (azureB2CConfig != null)
                    {
                        // Use JwtHandlerService to get validation parameters
                        // This will be resolved from DI at runtime
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                // Allow token from query string for SignalR, WebSockets, etc.
                                var accessToken = context.Request.Query["access_token"];
                                if (!string.IsNullOrEmpty(accessToken))
                                {
                                    context.Token = accessToken;
                                }
                                return Task.CompletedTask;
                            }
                        };

                        // Configure token validation parameters
                        // These will be overridden by JwtHandlerService if it's used directly
                        if (!azureB2CConfig.EnableStubMode)
                        {
                            options.Authority = azureB2CConfig.GetMetadataEndpoint();
                            options.Audience = !string.IsNullOrEmpty(azureB2CConfig.Audience) 
                                ? azureB2CConfig.Audience 
                                : azureB2CConfig.ClientId;

                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = azureB2CConfig.ValidateIssuer,
                                ValidIssuers = azureB2CConfig.GetValidIssuers(),
                                ValidateAudience = azureB2CConfig.ValidateAudience,
                                ValidAudience = options.Audience,
                                ValidateLifetime = azureB2CConfig.ValidateLifetime,
                                ValidateIssuerSigningKey = azureB2CConfig.ValidateIssuerSigningKey,
                                ClockSkew = TimeSpan.FromMinutes(5)
                            };
                        }
                        else
                        {
                            // Stub mode: minimal validation
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = false,
                                ValidateIssuerSigningKey = false,
                                SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
                            };
                        }
                    }
                });
        }
        else
        {
            // Fallback to legacy JWT configuration
            var jwtSection = configuration.GetSection("Authentication:Jwt");
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var secretKey = jwtSection["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException(
                    "JWT SecretKey is required in Authentication:Jwt configuration section.");
            }

            services.AddAuthentication()
                .AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ValidateLifetime = true
                    };
                });
        }

        return services;
    }
}
