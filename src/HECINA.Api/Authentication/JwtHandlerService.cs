using HECINA.Api.Models.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HECINA.Api.Authentication;

/// <summary>
/// Service for handling JWT token validation for Azure AD B2C.
/// Supports multi-issuer validation, stub mode, and robust error handling.
/// </summary>
public interface IJwtHandlerService
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    TokenValidationParameters GetTokenValidationParameters();
}

public class JwtHandlerService : IJwtHandlerService
{
    private readonly MicrosoftIdentityConfig _config;
    private readonly ILogger<JwtHandlerService> _logger;
    private readonly IConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtHandlerService(
        IOptions<MicrosoftIdentityConfig> config,
        ILogger<JwtHandlerService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();

        // Initialize configuration manager for Azure B2C if not in stub mode
        if (!_config.EnableStubMode && !string.IsNullOrEmpty(_config.GetMetadataEndpoint()))
        {
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                _config.GetMetadataEndpoint(),
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
        }
    }

    /// <summary>
    /// Validates a JWT token and returns the ClaimsPrincipal if valid.
    /// </summary>
    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token validation failed: Token is null or empty");
            return null;
        }

        try
        {
            // Stub mode: Skip validation and return a mock principal
            if (_config.EnableStubMode)
            {
                _logger.LogInformation("Stub mode enabled: Bypassing token validation");
                return CreateStubPrincipal(token);
            }

            // Get validation parameters
            var validationParameters = await GetTokenValidationParametersAsync();

            // Validate the token
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            _logger.LogInformation("Token validated successfully for user: {User}", 
                principal.Identity?.Name ?? "Unknown");

            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid signature");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    /// <summary>
    /// Gets the token validation parameters for JWT validation.
    /// </summary>
    public TokenValidationParameters GetTokenValidationParameters()
    {
        return GetTokenValidationParametersAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the token validation parameters asynchronously.
    /// </summary>
    private async Task<TokenValidationParameters> GetTokenValidationParametersAsync()
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = _config.ValidateIssuer,
            ValidateAudience = _config.ValidateAudience,
            ValidateLifetime = _config.ValidateLifetime,
            ValidateIssuerSigningKey = _config.ValidateIssuerSigningKey,
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
        };

        // Set audience
        if (_config.ValidateAudience)
        {
            var audience = !string.IsNullOrEmpty(_config.Audience) ? _config.Audience : _config.ClientId;
            validationParameters.ValidAudience = audience;
        }

        // Set valid issuers (multi-issuer support)
        if (_config.ValidateIssuer)
        {
            var validIssuers = _config.GetValidIssuers();
            if (validIssuers.Any())
            {
                validationParameters.ValidIssuers = validIssuers;
            }
        }

        // Get signing keys from OIDC configuration if available
        if (_configurationManager != null)
        {
            try
            {
                var openIdConfig = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
                validationParameters.IssuerSigningKeys = openIdConfig.SigningKeys;

                // Also validate against issuer from metadata if not explicitly set
                if (_config.ValidateIssuer && !validationParameters.ValidIssuers.Any())
                {
                    validationParameters.ValidIssuer = openIdConfig.Issuer;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve OpenID Connect configuration from metadata endpoint");
                throw new InvalidOperationException(
                    "Unable to retrieve OpenID Connect configuration. Ensure Azure B2C configuration is correct.", ex);
            }
        }

        return validationParameters;
    }

    /// <summary>
    /// Creates a stub principal for development/testing when stub mode is enabled.
    /// </summary>
    private ClaimsPrincipal CreateStubPrincipal(string token)
    {
        try
        {
            // Try to read the token without validation to extract claims
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var claims = jwtToken.Claims.ToList();

            // Add some default claims if not present
            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                claims.Add(new Claim(ClaimTypes.Name, "StubUser"));
            }
            if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "stub-user-id"));
            }

            var identity = new ClaimsIdentity(claims, "Stub");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            // If we can't read the token, create a minimal stub principal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "StubUser"),
                new Claim(ClaimTypes.NameIdentifier, "stub-user-id")
            };
            var identity = new ClaimsIdentity(claims, "Stub");
            return new ClaimsPrincipal(identity);
        }
    }
}
