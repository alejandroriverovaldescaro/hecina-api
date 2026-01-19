using HECINA.Api.Models.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HECINA.Api.Services;

/// <summary>
/// Service for handling JWT token validation using Azure AD B2C
/// </summary>
public interface IJwtHandlerService
{
    /// <summary>
    /// Validates a JWT token from the Authorization header
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>ClaimsPrincipal if validation succeeds, null otherwise</returns>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>
    /// Extracts the user name identifier (sub claim) from a ClaimsPrincipal
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal containing the claims</param>
    /// <returns>The user name identifier, or null if not found</returns>
    string? GetUserNameIdentifier(ClaimsPrincipal principal);
}

public class JwtHandlerService : IJwtHandlerService
{
    private readonly MicrosoftIdentityConfig _config;
    private readonly ILogger<JwtHandlerService> _logger;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public JwtHandlerService(
        IOptions<MicrosoftIdentityConfig> config,
        ILogger<JwtHandlerService> logger)
    {
        _config = config.Value;
        _logger = logger;

        // Initialize the configuration manager for retrieving signing keys
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            _config.EndpointOpenIDConnectMetadataDocument,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    }

    /// <summary>
    /// Validates a JWT token using Azure AD B2C configuration
    /// </summary>
    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            // Retrieve the OpenID Connect configuration (includes signing keys)
            var openIdConfig = await _configurationManager.GetConfigurationAsync(CancellationToken.None);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                // Use the issuer from OpenID config for flexibility with different B2C policy configurations
                ValidIssuer = openIdConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = _config.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
            };

            var tokenHandler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            if (!result.IsValid)
            {
                _logger.LogWarning("Token validation failed: {Error}", result.Exception?.Message ?? "Unknown error");
                return null;
            }

            // Additional validation: Check that the token uses the expected algorithm
            var jwtToken = result.SecurityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
            if (jwtToken == null)
            {
                _logger.LogWarning("Token is not a valid JWT token");
                return null;
            }
            if (!jwtToken.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.Ordinal))
            {
                _logger.LogWarning("Token does not use RS256 algorithm");
                return null;
            }

            var principal = new ClaimsPrincipal(result.ClaimsIdentity);
            _logger.LogInformation("JWT token validated successfully for user: {UserId}", GetUserNameIdentifier(principal));
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError(ex, "Security token validation failed");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    /// <summary>
    /// Extracts the user name identifier (sub claim) from the ClaimsPrincipal
    /// </summary>
    public string? GetUserNameIdentifier(ClaimsPrincipal principal)
    {
        // Try to get the 'sub' claim (standard subject claim)
        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier) 
            ?? principal.FindFirst("sub")
            ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (subClaim != null)
        {
            return subClaim.Value;
        }

        _logger.LogWarning("User name identifier (sub claim) not found in token");
        return null;
    }
}
