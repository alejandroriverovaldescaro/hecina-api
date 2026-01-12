namespace HECINA.Api.Models.Configurations;

/// <summary>
/// Configuration for Microsoft Identity (Azure AD B2C) JWT authentication.
/// Supports multi-issuer validation for Azure B2C scenarios.
/// </summary>
public class MicrosoftIdentityConfig
{
    public const string SectionName = "AzureAdB2C";

    /// <summary>
    /// Azure AD B2C tenant instance (e.g., "https://contoso.b2clogin.com")
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD B2C domain (e.g., "contoso.onmicrosoft.com")
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID (GUID)
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Client ID (Application ID)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Sign-up or sign-in policy/user flow name (e.g., "B2C_1_susi")
    /// </summary>
    public string SignUpSignInPolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable stub mode for development/testing (bypasses actual token validation)
    /// </summary>
    public bool EnableStubMode { get; set; } = false;

    /// <summary>
    /// List of valid issuers for multi-issuer support.
    /// If empty, will be auto-generated from Instance, Domain, and TenantId.
    /// </summary>
    public List<string> ValidIssuers { get; set; } = new List<string>();

    /// <summary>
    /// Audience (typically the ClientId)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Whether to validate the issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Gets the metadata endpoint URL for OIDC configuration
    /// </summary>
    public string GetMetadataEndpoint()
    {
        if (!string.IsNullOrEmpty(Instance) && !string.IsNullOrEmpty(Domain) && !string.IsNullOrEmpty(SignUpSignInPolicyId))
        {
            return $"{Instance.TrimEnd('/')}/{Domain}/{SignUpSignInPolicyId}/v2.0/.well-known/openid-configuration";
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets the list of valid issuers for token validation.
    /// If ValidIssuers is empty, generates default issuers based on configuration.
    /// </summary>
    public List<string> GetValidIssuers()
    {
        if (ValidIssuers != null && ValidIssuers.Any())
        {
            return ValidIssuers;
        }

        var issuers = new List<string>();
        if (!string.IsNullOrEmpty(Instance) && !string.IsNullOrEmpty(TenantId))
        {
            // Add both common issuer patterns for Azure B2C
            issuers.Add($"{Instance.TrimEnd('/')}/{TenantId}/v2.0/");
            issuers.Add($"{Instance.TrimEnd('/')}/tfp/{TenantId}/{SignUpSignInPolicyId}/v2.0/");
        }
        return issuers;
    }

    /// <summary>
    /// Validates that required configuration values are present
    /// </summary>
    public bool IsValid()
    {
        if (EnableStubMode)
        {
            return true; // In stub mode, configuration is optional
        }

        return !string.IsNullOrEmpty(Instance) &&
               !string.IsNullOrEmpty(Domain) &&
               !string.IsNullOrEmpty(TenantId) &&
               !string.IsNullOrEmpty(ClientId) &&
               !string.IsNullOrEmpty(SignUpSignInPolicyId);
    }
}
